using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;

public sealed record RemoveMemberCommand(
    Guid ProjectId,
    Guid UserId,
    Guid RemovedByUserId) : IRequest<ProjectDto>;
