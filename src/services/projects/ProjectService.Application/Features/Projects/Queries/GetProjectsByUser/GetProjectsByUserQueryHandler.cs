using System.Text.Json;
using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Queries.GetProjectsByUser;

public sealed class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    private static readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    public GetProjectsByUserQueryHandler(IProjectRepository repository, IProjectSummaryRepository summaryRepository, IMapper mapper, IDistributedCache cache)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = request.OrganizationId.HasValue
            ? $"projects:org:{request.OrganizationId}"
            : $"projects:user:{request.UserId}";

        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (cached is not null)
                return JsonSerializer.Deserialize<List<ProjectDto>>(cached)!;
        }
        catch { /* Redis unavailable — fall through to DB */ }

        var projects = request.OrganizationId.HasValue
            ? await _repository.GetByOrganizationIdAsync(request.OrganizationId.Value, cancellationToken)
            : await _repository.GetByMemberUserIdAsync(request.UserId, cancellationToken);
        var summaries = await _summaryRepository.GetByProjectIdsAsync(projects.Select(x => x.Id).ToArray(), cancellationToken);
        var summaryLookup = summaries.ToDictionary(x => x.ProjectId);

        var dtos = projects
            .Select(project => ProjectDtoFactory.Create(
                project,
                summaryLookup.TryGetValue(project.Id, out var summary) ? summary : null))
            .ToList();

        try
        {
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), _cacheOptions, cancellationToken);
        }
        catch { /* ignore cache write failure */ }

        return dtos;
    }
}
