using AutoMapper;
using BitirmeProject.ProjectService.Application.Abstractions;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DeleteProjectCommandHandler(IProjectRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
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

        return _mapper.Map<ProjectDto>(project);
    }
}
