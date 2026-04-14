using System.Text.Json;
using AutoMapper;
using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace BitirmeProject.IssueService.Application.Features.Issues.Queries.GetIssuesByProject;

public sealed class GetIssuesByProjectQueryHandler : IRequestHandler<GetIssuesByProjectQuery, IReadOnlyList<IssueBoardItemDto>>
{
    private readonly IIssueBoardRepository _boardRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public GetIssuesByProjectQueryHandler(IIssueBoardRepository boardRepository, IMapper mapper, IDistributedCache cache)
    {
        _boardRepository = boardRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<IssueBoardItemDto>> Handle(GetIssuesByProjectQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"board:project:{request.ProjectId}:{request.CallerOrgId}";

        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (cached is not null)
                return JsonSerializer.Deserialize<List<IssueBoardItemDto>>(cached)!;
        }
        catch { /* Redis unavailable — fall through to DB */ }

        var items = await _boardRepository.GetByProjectIdAsync(request.ProjectId, request.CallerOrgId, cancellationToken);
        var dtos = items.Select(item => _mapper.Map<IssueBoardItemDto>(item)).ToList();

        try
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), _cacheOptions, cancellationToken);
        }
        catch { /* ignore cache write failure */ }

        return dtos;
    }
}
