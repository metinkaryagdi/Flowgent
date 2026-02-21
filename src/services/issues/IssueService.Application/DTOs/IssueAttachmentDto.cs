namespace BitirmeProject.IssueService.Application.DTOs;

public sealed class IssueAttachmentDto
{
    public Guid Id { get; init; }
    public Guid IssueId { get; init; }
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public Guid UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }
}
