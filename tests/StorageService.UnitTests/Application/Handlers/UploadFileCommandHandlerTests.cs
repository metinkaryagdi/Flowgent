using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;
using BitirmeProject.StorageService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace StorageService.UnitTests.Application.Handlers;

public sealed class UploadFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesStoredFile_AndSaves()
    {
        var repository = Substitute.For<IStorageRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        StoredFile? captured = null;
        repository.AddAsync(Arg.Do<StoredFile>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var expected = new StoredFileDto { Id = Guid.NewGuid() };
        mapper.Map<StoredFileDto>(Arg.Any<StoredFile>()).Returns(expected);

        var handler = new UploadFileCommandHandler(repository, unitOfWork, mapper);
        var command = new UploadFileCommand("file.txt", "text/plain", 10, "path", Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        captured.Should().NotBeNull();
        captured!.FileName.Should().Be("file.txt");

        await repository.Received(1).AddAsync(Arg.Any<StoredFile>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<StoredFileDto>(Arg.Any<StoredFile>());
    }
}
