using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintsByProject;

public sealed class GetSprintsByProjectQueryHandler : IRequestHandler<GetSprintsByProjectQuery, IReadOnlyList<SprintDto>>
{
    private readonly ISprintRepository _repository;
    private readonly IMapper _mapper;

    public GetSprintsByProjectQueryHandler(ISprintRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SprintDto>> Handle(GetSprintsByProjectQuery request, CancellationToken cancellationToken)
    {
        var sprints = await _repository.GetByProjectIdAsync(request.ProjectId, request.CallerOrgId, cancellationToken);
        return _mapper.Map<IReadOnlyList<SprintDto>>(sprints);
    }
}
