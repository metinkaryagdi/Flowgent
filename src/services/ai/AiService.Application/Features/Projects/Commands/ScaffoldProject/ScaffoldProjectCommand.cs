using BitirmeProject.AiService.Application.DTOs;
using MediatR;

namespace BitirmeProject.AiService.Application.Features.Projects.Commands.ScaffoldProject;

public sealed record ScaffoldProjectCommand(
    Guid UserId,
    Guid OrganizationId,
    string Description
) : IRequest<ProjectScaffoldDraftDto>;
