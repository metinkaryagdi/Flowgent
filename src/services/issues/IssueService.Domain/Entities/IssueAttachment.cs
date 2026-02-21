namespace BitirmeProject.IssueService.Domain.Entities;

public sealed class IssueAttachment
{
    public Guid Id { get; private set; }
    public Guid IssueId { get; private set; }
    public Guid FileId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private IssueAttachment() { }

    public IssueAttachment(
        Guid issueId,
        Guid fileId,
        string fileName,
        string contentType,
        long sizeBytes,
        Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        IssueId = issueId;
        FileId = fileId;
        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = DateTime.UtcNow;
    }
}
