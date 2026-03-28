namespace BitirmeProject.SprintService.Application.DTOs;

/// <summary>
/// Minimal issue data fetched from IssueService when the local SprintIssue
/// projection has not yet been created (race condition guard in AddIssueCommandHandler).
/// </summary>
public sealed record IssueMetadataDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Status,
    string Priority,
    Guid CreatedByUserId);
