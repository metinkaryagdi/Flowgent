using BitirmeProject.ProjectService.Application.DTOs;
using BitirmeProject.ProjectService.Domain.Entities;

namespace BitirmeProject.ProjectService.Application.Common.Mappings;

internal static class ProjectDtoFactory
{
    public static ProjectDto Create(Project project, ProjectSummary? summary)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Key = project.Key,
            OwnerUserId = project.OwnerUserId,
            OrganizationId = project.OrganizationId,
            IsArchived = project.IsArchived,
            IssueCount = summary?.IssueCount ?? 0,
            OpenIssueCount = summary?.OpenIssueCount ?? 0,
            InProgressIssueCount = summary?.InProgressIssueCount ?? 0,
            DoneIssueCount = summary?.DoneIssueCount ?? 0,
            CreatedAt = project.CreatedAt
        };
    }
}
