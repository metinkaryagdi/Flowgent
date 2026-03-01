using AutoMapper;
using BitirmeProject.StorageService.Application.Abstractions;
using BitirmeProject.StorageService.Application.DTOs;
using BitirmeProject.StorageService.Application.Features.Files.Queries.GetFileById;
using BitirmeProject.StorageService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace StorageService.UnitTests.Application.Handlers;

public sealed class GetFileByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsNull_WhenMissing()
    {
        var repository = Substitute.For<IStorageRepository>();
        var mapper = Substitute.For<IMapper>();

        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((StoredFile?)null);

        var handler = new GetFileByIdQueryHandler(repository, mapper);
        var query = new GetFileByIdQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenFound()
    {
        var repository = Substitute.For<IStorageRepository>();
        var mapper = Substitute.For<IMapper>();

        var file = new StoredFile("file.txt", "text/plain", 10, "path", Guid.NewGuid());
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(file);

        var dto = new StoredFileDto { Id = file.Id };
        mapper.Map<StoredFileDto>(file).Returns(dto);

        var handler = new GetFileByIdQueryHandler(repository, mapper);
        var query = new GetFileByIdQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().Be(dto);
    }
}
