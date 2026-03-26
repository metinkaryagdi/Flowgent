using BitirmeProject.ProjectService.Domain.Enums;
using BitirmeProject.ProjectService.Application.DTOs;
using MediatR;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;

public sealed record AddMemberCommand(
    Guid ProjectId,
    Guid UserId,
    Guid AddedByUserId,
    Guid? CorrelationId,
    ProjectMemberRole Role) : IRequest<ProjectDto>;
