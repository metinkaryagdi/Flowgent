using BitirmeProject.IssueService.Application.DTOs;
using BitirmeProject.IssueService.Domain.Entities;

namespace BitirmeProject.IssueService.Application.Common.Mappings;

internal static class IssueDtoFactory
{
    public static IssueDto Create(Issue issue, Guid? sprintId)
    {
        return new IssueDto
        {
            Id = issue.Id,
            ProjectId = issue.ProjectId,
            Title = issue.Title,
            Description = issue.Description,
            Status = issue.Status,
            Priority = issue.Priority,
            CreatedByUserId = issue.CreatedByUserId,
            AssigneeUserId = issue.AssigneeUserId,
            SprintId = sprintId,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt,
            Version = issue.Version
        };
    }
}
