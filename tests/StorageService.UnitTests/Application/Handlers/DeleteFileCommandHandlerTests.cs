using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;
using BitirmeProject.StorageService.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Shared.Abstractions.Exceptions;

namespace StorageService.UnitTests.Application.Handlers;

public sealed class DeleteFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_Throws_WhenFileMissing()
    {
        var repository = Substitute.For<IStorageRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var fileStorage = Substitute.For<IFileStorage>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var handler = new DeleteFileCommandHandler(repository, unitOfWork, fileStorage);
        var command = new DeleteFileCommand(Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repository.DidNotReceive().RemoveAsync(Arg.Any<StoredFile>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeletesFile_AndRemovesRecord()
    {
        var repository = Substitute.For<IStorageRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var fileStorage = Substitute.For<IFileStorage>();

        var stored = new StoredFile("file.txt", "text/plain", 10, "path", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(stored);

        var handler = new DeleteFileCommandHandler(repository, unitOfWork, fileStorage);
        var command = new DeleteFileCommand(stored.Id);

        await handler.Handle(command, CancellationToken.None);

        await fileStorage.Received(1).DeleteAsync(stored.StoragePath, Arg.Any<CancellationToken>());
        await repository.Received(1).RemoveAsync(stored, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
