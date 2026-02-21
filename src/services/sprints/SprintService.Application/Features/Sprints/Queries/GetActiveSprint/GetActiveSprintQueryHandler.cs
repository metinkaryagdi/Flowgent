using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetActiveSprint;

public sealed class GetActiveSprintQueryHandler : IRequestHandler<GetActiveSprintQuery, SprintDto?>
{
    private readonly ISprintRepository _repository;
    private readonly IMapper _mapper;

    public GetActiveSprintQueryHandler(ISprintRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<SprintDto?> Handle(GetActiveSprintQuery request, CancellationToken cancellationToken)
    {
        var sprint = await _repository.GetActiveByProjectIdAsync(request.ProjectId, cancellationToken);
        return sprint is null ? null : _mapper.Map<SprintDto>(sprint);
    }
}
