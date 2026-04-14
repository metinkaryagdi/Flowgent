using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Queries.GetSprintById;

public sealed class GetSprintByIdQueryHandler : IRequestHandler<GetSprintByIdQuery, SprintDto?>
{
    private readonly ISprintRepository _repository;
    private readonly IMapper _mapper;

    public GetSprintByIdQueryHandler(ISprintRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<SprintDto?> Handle(GetSprintByIdQuery request, CancellationToken cancellationToken)
    {
        var sprint = await _repository.GetByIdAsync(request.SprintId, cancellationToken);
        return sprint is null ? null : _mapper.Map<SprintDto>(sprint);
    }
}
