using BitirmeProject.StorageService.Application.DTOs;
using MediatR;

namespace BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById;

public sealed record GetFileByIdQuery(Guid FileId) : IRequest<StoredFileDto?>;
