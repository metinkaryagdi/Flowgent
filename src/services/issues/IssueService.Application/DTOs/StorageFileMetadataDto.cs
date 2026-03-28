namespace BitirmeProject.IssueService.Application.DTOs;

public sealed class StorageFileMetadataDto
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public Guid UploadedByUserId { get; init; }
    // 0 = Temporary, 1 = Finalized  (matches StorageService.Domain.Enums.StoredFileStatus)
    public int Status { get; init; }
}
