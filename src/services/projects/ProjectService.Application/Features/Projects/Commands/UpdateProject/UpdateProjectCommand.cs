using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string Key,
    Guid UpdatedByUserId,
    Guid? CorrelationId) : IRequest<ProjectDto>;
