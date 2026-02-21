using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest<ProjectDto>;
