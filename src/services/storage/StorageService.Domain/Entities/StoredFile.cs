using BitirmeProject.StorageService.Domain.Enums;

namespace BitirmeProject.StorageService.Domain.Entities;

/// <summary>
/// Represents a stored file (blob + minimal metadata).
///
/// Authorization semantics:
///   UploadedByUserId is the only identity field StorageService tracks per file.
///   Access control at the StorageService layer is uploader/Admin only.
///   Parent-entity context (Project, Issue) is intentionally absent here;
///   that authorization layer lives in the consuming service (IssueService).
/// </summary>
public sealed class StoredFile
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public StoredFileStatus Status { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? FinalizedAt { get; private set; }

    private StoredFile() { }

    public StoredFile(
        string fileName,
        string contentType,
        long sizeBytes,
        string storagePath,
        Guid uploadedByUserId)
    {
        Id = Guid.NewGuid();
        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        StoragePath = storagePath.Trim();
        UploadedByUserId = uploadedByUserId;
        Status = StoredFileStatus.Temporary;
        UploadedAt = DateTime.UtcNow;
        ExpiresAt = UploadedAt.AddHours(24);
    }

    public bool IsExpired(DateTime utcNow)
    {
        return Status == StoredFileStatus.Temporary
            && ExpiresAt.HasValue
            && ExpiresAt.Value <= utcNow;
    }

    public void FinalizeUpload(string storagePath)
    {
        if (Status == StoredFileStatus.Finalized)
            return;

        StoragePath = storagePath.Trim();
        Status = StoredFileStatus.Finalized;
        FinalizedAt = DateTime.UtcNow;
        ExpiresAt = null;
    }
}
