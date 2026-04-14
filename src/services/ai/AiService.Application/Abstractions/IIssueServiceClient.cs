using BitirmeProject.AiService.Application.DTOs;

namespace BitirmeProject.AiService.Application.Abstractions;

public interface IIssueServiceClient
{
    Task<CreatedIssueDto> CreateIssueAsync(Guid projectId, Guid userId, Guid organizationId, string title, string? description, string priority, CancellationToken ct = default);
    Task<List<ProjectIssueDto>> GetIssuesByProjectAsync(Guid projectId, Guid organizationId, CancellationToken ct = default);
}
