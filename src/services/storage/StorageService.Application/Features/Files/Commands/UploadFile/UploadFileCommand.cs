using BitirmeProject.StorageService.Application.DTOs;
using MediatR;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;

public sealed record UploadFileCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    string StoragePath,
    Guid UploadedByUserId) : IRequest<StoredFileDto>;
