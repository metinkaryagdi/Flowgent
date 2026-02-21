using BitirmeProject.StorageService.Application.Abstractions;
using MediatR;
using Shared.Abstractions.Exceptions;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;

public sealed class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand>
{
    private readonly IStorageRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;

    public DeleteFileCommandHandler(
        IStorageRepository repository,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(request.FileId, cancellationToken);
        if (file is null)
            throw new NotFoundException("StoredFile", request.FileId);

        await _fileStorage.DeleteAsync(file.StoragePath, cancellationToken);
        await _repository.RemoveAsync(file, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
