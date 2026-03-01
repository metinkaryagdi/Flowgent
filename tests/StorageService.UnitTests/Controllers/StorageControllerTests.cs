using System.IO;
using BitirmeProject.StorageService.Api.Controllers;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;
using BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;
using BitirmeProject.StorageService.Domain.Entities;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shared.Abstractions.Exceptions;

namespace StorageService.UnitTests.Controllers;

public sealed class StorageControllerTests
{
    [Fact]
    public async Task Upload_Throws_WhenFileEmpty()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        var controller = new StorageController(mediator, repository, storage);
        var file = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "file.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var act = async () => await controller.Upload(file, Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Upload_SavesFile_AndReturnsOk()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("path");

        var dto = new StoredFileDto { Id = Guid.NewGuid() };
        mediator.Send(Arg.Any<UploadFileCommand>(), Arg.Any<CancellationToken>()).Returns(dto);

        var controller = new StorageController(mediator, repository, storage);
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "file.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var result = await controller.Upload(file, Guid.NewGuid(), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        mediator.Send(Arg.Any<BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById.GetFileByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((StoredFileDto?)null);

        var controller = new StorageController(mediator, repository, storage);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Download_ReturnsNotFound_WhenFileMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var controller = new StorageController(mediator, repository, storage);

        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Download_ReturnsFile_WhenFound()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        var stored = new StoredFile("file.txt", "text/plain", 3, "path", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(stored);
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

        var controller = new StorageController(mediator, repository, storage);

        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
    }

    [Fact]
    public async Task Delete_SendsCommand_AndReturnsNoContent()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        var controller = new StorageController(mediator, repository, storage);

        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1).Send(Arg.Any<DeleteFileCommand>(), Arg.Any<CancellationToken>());
    }
}
