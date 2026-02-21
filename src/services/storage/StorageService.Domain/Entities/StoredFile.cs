namespace BitirmeProject.StorageService.Domain.Entities;

public sealed class StoredFile
{
    public Guid Id { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

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
        UploadedAt = DateTime.UtcNow;
    }
}
