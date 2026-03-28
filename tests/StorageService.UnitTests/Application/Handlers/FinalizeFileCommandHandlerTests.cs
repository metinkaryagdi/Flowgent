using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Commands.FinalizeFile;
using BitirmeProject.StorageService.Domain.Entities;
using BitirmeProject.StorageService.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace StorageService.UnitTests.Application.Handlers;

public sealed class FinalizeFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_FinalizesStoredFile_AndPersistsChanges()
    {
        var repository = Substitute.For<IStorageRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var fileStorage = Substitute.For<IFileStorage>();
        var mapper = Substitute.For<IMapper>();

        var file = new StoredFile("file.txt", "text/plain", 10, "temp/file.txt", Guid.NewGuid());
        repository.GetByIdAsync(file.Id, Arg.Any<CancellationToken>()).Returns(file);
        fileStorage.PromoteAsync(file.StoragePath, Arg.Any<CancellationToken>()).Returns("files/file.txt");

        var expected = new StoredFileDto { Id = file.Id, Status = StoredFileStatus.Finalized };
        mapper.Map<StoredFileDto>(file).Returns(expected);

        var handler = new FinalizeFileCommandHandler(repository, unitOfWork, fileStorage, mapper);
        var result = await handler.Handle(new FinalizeFileCommand(file.Id), CancellationToken.None);

        result.Should().Be(expected);
        file.Status.Should().Be(StoredFileStatus.Finalized);
        file.StoragePath.Should().Be("files/file.txt");
        file.FinalizedAt.Should().NotBeNull();

        await repository.Received(1).UpdateAsync(file, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
