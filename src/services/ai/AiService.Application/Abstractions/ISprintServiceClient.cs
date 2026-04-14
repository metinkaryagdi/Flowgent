using BitirmeProject.AiService.Application.DTOs;

namespace BitirmeProject.AiService.Application.Abstractions;

public interface ISprintServiceClient
{
    Task<CreatedSprintDto> CreateSprintAsync(Guid projectId, Guid userId, Guid organizationId, string name, string goal, CancellationToken ct = default);
    Task AddIssueToSprintAsync(Guid sprintId, Guid issueId, Guid userId, CancellationToken ct = default);
    Task<ActiveSprintDto?> GetActiveSprintAsync(Guid projectId, Guid organizationId, CancellationToken ct = default);
    Task<SprintDetailDto?> GetSprintByIdAsync(Guid sprintId, Guid organizationId, CancellationToken ct = default);
}
