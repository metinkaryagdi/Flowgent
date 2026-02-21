using MediatR;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;

public sealed record DeleteFileCommand(Guid FileId) : IRequest;
