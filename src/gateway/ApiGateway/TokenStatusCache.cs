using Microsoft.Extensions.Caching.Distributed;

namespace ApiGateway;

/// <summary>
/// Caches the "is this token still valid" answer that the gateway asks IdentityService on
/// every authenticated request.
///
/// This used to be an <c>IMemoryCache</c>, which is per-process: with two gateway replicas
/// each one kept its own window, so a revoked token stayed usable for up to the window
/// length on every replica that had not asked yet, and the effective revocation delay grew
/// with the replica count. Redis gives every replica one shared answer.
///
/// <para>
/// <b>Failure policy: the cache fails open, the source of truth fails closed.</b> If Redis
/// is unreachable, this class reports a miss and swallows the write, so the caller falls
/// through to asking IdentityService directly -- slower, but the answer is fresher than
/// the cache would have been, so skipping the cache is never the less safe option. Failing
/// closed here would mean a Redis blip logs out every user of the product, which is a far
/// worse outcome than a few hundred extra HTTP calls. If IdentityService itself cannot be
/// reached the caller still rejects the request; that is the check that actually decides
/// authorisation.
/// </para>
/// </summary>
public sealed class TokenStatusCache
{
    /// <summary>
    /// How long a positive answer is trusted. This is the revocation delay a signed-out or
    /// deactivated user can still act within, so it is deliberately short.
    /// </summary>
    private static readonly TimeSpan ValidTtl = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Negative answers expire faster: a user who was just reactivated should not be kept
    /// locked out, and the cost of re-asking is one cheap call.
    /// </summary>
    private static readonly TimeSpan InvalidTtl = TimeSpan.FromSeconds(2);

    private static readonly byte[] True = [1];

    private readonly IDistributedCache _cache;
    private readonly ILogger<TokenStatusCache> _logger;

    public TokenStatusCache(IDistributedCache cache, ILogger<TokenStatusCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Returns the cached verdict, or null when unknown (miss or cache down).</summary>
    public async Task<bool?> TryGetAsync(Guid userId, Guid securityStamp, CancellationToken cancellationToken)
    {
        try
        {
            var cached = await _cache.GetAsync(Key(userId, securityStamp), cancellationToken);
            return cached is null ? null : cached.Length > 0 && cached[0] == 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Debug, not Warning: when Redis is down this fires on every single request and
            // would bury the connection error the Redis client itself logs once.
            _logger.LogDebug(ex, "Token status cache read failed; falling back to IdentityService.");
            return null;
        }
    }

    public async Task SetAsync(Guid userId, Guid securityStamp, bool isValid, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetAsync(
                Key(userId, securityStamp),
                isValid ? True : [0],
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = isValid ? ValidTtl : InvalidTtl,
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Token status cache write failed; the answer is simply not cached.");
        }
    }

    /// <summary>
    /// The stamp is part of the key on purpose: rotating it (sign-out, password change)
    /// makes every previously cached entry unreachable rather than needing invalidation.
    /// </summary>
    private static string Key(Guid userId, Guid securityStamp) => $"token-status:{userId}:{securityStamp}";
}
