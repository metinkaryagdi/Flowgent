using System.Text.Json;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProjectPaged;

public sealed class GetIssuesByProjectPagedQueryHandler : IRequestHandler<GetIssuesByProjectPagedQuery, PagedResult<IssueBoardItemDto>>
{
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    // Mirrors ProjectService's cache-aside convention (System.Text.Json + 2 minute TTL).
    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    // The generation token outlives the individual page snapshots so that every page/filter
    // variant of a board shares one generation between writes; the command handlers bust it
    // instantly via RemoveAsync on any issue mutation.
    private static readonly DistributedCacheEntryOptions _generationOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    };

    public GetIssuesByProjectPagedQueryHandler(IIssueBoardRepository boardRepository, IMapper mapper, IDistributedCache cache)
    {
        _boardRepository = boardRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<PagedResult<IssueBoardItemDto>> Handle(GetIssuesByProjectPagedQuery request, CancellationToken cancellationToken)
    {
        // The five issue command handlers (create/update/status/assign/delete) invalidate the board
        // cache by removing this exact key. We treat it as a *generation token*: each cached
        // page/filter variant embeds the current token, so a single RemoveAsync on this key
        // atomically invalidates ALL variants at once. This is required because the query is
        // parameterised (page, pageSize, sprintId, backlogOnly) — a single flat key cannot hold
        // every variant, and the codebase exposes only IDistributedCache (RemoveAsync cannot
        // pattern-match, and there is no StackExchange.Redis SCAN wired up anywhere).
        var versionKey = $"board:project:{request.ProjectId}:{request.CallerOrgId}";

        var generation = await GetOrCreateGenerationAsync(versionKey, cancellationToken);
        var cacheKey = generation is null
            ? null
            : $"{versionKey}:g{generation}:p{request.Page}:s{request.PageSize}:sp{request.SprintId?.ToString() ?? "all"}:b{request.BacklogOnly}";

        if (cacheKey is not null)
        {
            try
            {
                var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
                if (cached is not null)
                    return JsonSerializer.Deserialize<PagedResult<IssueBoardItemDto>>(cached)!;
            }
            catch { /* Redis unavailable — fall through to DB */ }
        }

        var (items, totalCount) = await _boardRepository.GetByProjectIdPagedAsync(
            request.ProjectId,
            request.Page,
            request.PageSize,
            request.SprintId,
            request.BacklogOnly,
            request.CallerOrgId,
            cancellationToken);

        var result = new PagedResult<IssueBoardItemDto>
        {
            Items = items.Select(item => _mapper.Map<IssueBoardItemDto>(item)).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };

        if (cacheKey is not null)
        {
            try
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), _cacheOptions, cancellationToken);
            }
            catch { /* ignore cache write failure */ }
        }

        return result;
    }

    // Returns the current generation token for the board, creating one if absent. Returns null
    // when Redis is unavailable, in which case the caller skips caching and reads straight from DB.
    private async Task<string?> GetOrCreateGenerationAsync(string versionKey, CancellationToken cancellationToken)
    {
        try
        {
            var generation = await _cache.GetStringAsync(versionKey, cancellationToken);
            if (generation is null)
            {
                generation = Guid.NewGuid().ToString("N");
                await _cache.SetStringAsync(versionKey, generation, _generationOptions, cancellationToken);
            }

            return generation;
        }
        catch
        {
            return null;
        }
    }
}
