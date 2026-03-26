using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Domain.Entities;
using MediatR;

namespace BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;

public sealed class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, SprintDto>
{
    private readonly ISprintRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSprintCommandHandler(
        ISprintRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SprintDto> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = new Sprint(
            request.ProjectId,
            request.Name,
            request.Goal,
            request.StartDate,
            request.EndDate,
            request.CreatedByUserId);
        await _repository.AddAsync(sprint, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SprintDto>(sprint);
    }
}
