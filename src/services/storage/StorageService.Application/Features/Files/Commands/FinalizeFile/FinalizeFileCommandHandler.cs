using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.FinalizeFile;

public sealed class FinalizeFileCommandHandler : IRequestHandler<FinalizeFileCommand, StoredFileDto>
{
    private readonly IStorageRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IMapper _mapper;

    public FinalizeFileCommandHandler(
        IStorageRepository repository,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _mapper = mapper;
    }

    public async Task<StoredFileDto> Handle(FinalizeFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null)
            throw new NotFoundException("StoredFile", request.FileId);

        var permanentPath = await _fileStorage.PromoteAsync(file.StoragePath, cancellationToken);
        file.FinalizeUpload(permanentPath);

        await _repository.UpdateAsync(file, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<StoredFileDto>(file);
    }
}
