using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using MediatR;

namespace BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById;

public sealed class GetFileByIdQueryHandler : IRequestHandler<GetFileByIdQuery, StoredFileDto?>
{
    private readonly IStorageRepository _repository;
    private readonly IMapper _mapper;

    public GetFileByIdQueryHandler(IStorageRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<StoredFileDto?> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(request.FileId, cancellationToken);
        return file is null ? null : _mapper.Map<StoredFileDto>(file);
    }
}
