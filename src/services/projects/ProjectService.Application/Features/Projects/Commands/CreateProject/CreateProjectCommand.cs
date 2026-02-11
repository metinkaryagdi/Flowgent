using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string Key,
    Guid OwnerUserId,
    Guid? CorrelationId) : IRequest<ProjectDto>;