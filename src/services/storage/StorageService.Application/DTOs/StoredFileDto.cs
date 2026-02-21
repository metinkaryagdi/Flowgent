namespace BitirmeProject.StorageService.Application.DTOs;

public sealed class StoredFileDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public Guid UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }
}
