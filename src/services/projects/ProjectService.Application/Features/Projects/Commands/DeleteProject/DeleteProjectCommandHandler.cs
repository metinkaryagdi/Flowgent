using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.Common.Mappings;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IProjectSummaryRepository _summaryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DeleteProjectCommandHandler(IProjectRepository repository, IProjectSummaryRepository summaryRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _summaryRepository = summaryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ProjectDto> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _repository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project", request.ProjectId);

        project.Archive();
        await _repository.UpdateAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = await _summaryRepository.GetByProjectIdAsync(project.Id, cancellationToken);
        return ProjectDtoFactory.Create(project, summary);
    }
}
