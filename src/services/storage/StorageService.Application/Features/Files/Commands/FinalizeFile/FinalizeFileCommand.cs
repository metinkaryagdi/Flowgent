using BitirmeProject.StorageService.Application.DTOs;
using MediatR;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.FinalizeFile;

public sealed record FinalizeFileCommand(Guid FileId) : IRequest<StoredFileDto>;
