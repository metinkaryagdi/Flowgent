using AutoMapper;
using BitirmeProject.SprintService.Application.Abstractions;
using BitirmeProject.SprintService.Application.DTOs;
using BitirmeProject.SprintService.Application.Features.Sprints.Commands.CreateSprint;
using BitirmeProject.SprintService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace SprintService.UnitTests.Application.Handlers;

public sealed class CreateSprintCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesSprint_AndSaves()
    {
        var repository = Substitute.For<ISprintRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mapper = Substitute.For<IMapper>();

        Sprint? captured = null;
        repository.AddAsync(Arg.Do<Sprint>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var expectedDto = new SprintDto { Id = Guid.NewGuid() };
        mapper.Map<SprintDto>(Arg.Any<Sprint>()).Returns(expectedDto);
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(14);

        var handler = new CreateSprintCommandHandler(repository, unitOfWork, mapper);
        var command = new CreateSprintCommand(Guid.NewGuid(), "Sprint", "Goal", Guid.NewGuid(), null, startDate, endDate);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedDto);
        captured.Should().NotBeNull();
        captured!.ProjectId.Should().Be(command.ProjectId);
        captured.Name.Should().Be(command.Name);
        captured.StartDate.Should().Be(startDate);
        captured.EndDate.Should().Be(endDate);

        await repository.Received(1).AddAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        mapper.Received(1).Map<SprintDto>(Arg.Any<Sprint>());
    }
}
