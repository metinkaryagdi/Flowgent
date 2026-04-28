namespace BitirmeProject.AiService.Application.Tools;

public sealed record ToolContext(
    Guid UserId,
    Guid OrganizationId,
    Guid ProjectId,
    Guid? SessionId = null);
