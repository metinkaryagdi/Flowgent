using System.IO;
using System.Security.Claims;
using BitirmeProject.StorageService.Api.Controllers;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Commands.DeleteFile;
using BitirmeProject.StorageService.Application.Features.Files.Commands.FinalizeFile;
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
        var controller = CreateController(mediator, repository, storage, Guid.NewGuid());

        var file = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "file.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var act = async () => await controller.Upload(file, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Upload_SavesTemporaryFile_AndReturnsOk()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();
        var uploaderId = Guid.NewGuid();

        storage.SaveTemporaryAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("temp/file.txt");

        var dto = new StoredFileDto { Id = Guid.NewGuid() };
        mediator.Send(Arg.Any<UploadFileCommand>(), Arg.Any<CancellationToken>()).Returns(dto);

        var controller = CreateController(mediator, repository, storage, uploaderId);
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "file.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var result = await controller.Upload(file, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);

        await mediator.Received(1).Send(
            Arg.Is<UploadFileCommand>(c => c.UploadedByUserId == uploaderId && c.StoragePath == "temp/file.txt"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        mediator.Send(Arg.Any<BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById.GetFileByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns((StoredFileDto?)null);

        var controller = CreateController(mediator, repository, storage, Guid.NewGuid());
        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Download_ReturnsForbid_WhenRequesterDoesNotOwnFile()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();

        var stored = new StoredFile("file.txt", "text/plain", 3, "temp/file.txt", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(stored);

        var controller = CreateController(mediator, repository, storage, Guid.NewGuid());
        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Download_ReturnsFile_WhenOwnerMatches()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();
        var uploaderId = Guid.NewGuid();

        var stored = new StoredFile("file.txt", "text/plain", 3, "files/file.txt", uploaderId);
        stored.FinalizeUpload("files/file.txt");
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(stored);
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

        var controller = CreateController(mediator, repository, storage, uploaderId);
        var result = await controller.Download(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<FileStreamResult>();
    }

    [Fact]
    public async Task Finalize_ReturnsOk_WhenOwnerMatches()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();
        var uploaderId = Guid.NewGuid();

        var stored = new StoredFile("file.txt", "text/plain", 3, "temp/file.txt", uploaderId);
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(stored);

        var dto = new StoredFileDto { Id = stored.Id };
        mediator.Send(Arg.Any<FinalizeFileCommand>(), Arg.Any<CancellationToken>()).Returns(dto);

        var controller = CreateController(mediator, repository, storage, uploaderId);
        var result = await controller.Finalize(stored.Id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Delete_SendsCommand_AndReturnsNoContent_WhenOwnerMatches()
    {
        var mediator = Substitute.For<IMediator>();
        var repository = Substitute.For<IStorageRepository>();
        var storage = Substitute.For<IFileStorage>();
        var uploaderId = Guid.NewGuid();

        var stored = new StoredFile("file.txt", "text/plain", 3, "files/file.txt", uploaderId);
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(stored);

        var controller = CreateController(mediator, repository, storage, uploaderId);
        var result = await controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await mediator.Received(1).Send(Arg.Any<DeleteFileCommand>(), Arg.Any<CancellationToken>());
    }

    private static StorageController CreateController(
        IMediator mediator,
        IStorageRepository repository,
        IFileStorage storage,
        Guid userId,
        bool isAdmin = false)
    {
        var controller = new StorageController(mediator, repository, storage);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }
}
