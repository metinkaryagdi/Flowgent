using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Domain.Entities;
using MediatR;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;

public sealed class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, StoredFileDto>
{
    private readonly IStorageRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UploadFileCommandHandler(
        IStorageRepository repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StoredFileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var file = new StoredFile(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            request.StoragePath,
            request.UploadedByUserId);

        await _repository.AddAsync(file, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<StoredFileDto>(file);
    }
}
