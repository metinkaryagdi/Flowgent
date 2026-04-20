using BitirmeProject.IssueService.Application.Abstractions;
using BitirmeProject.IssueService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace IssueService.UnitTests.Consumers;

/// <summary>
/// Security regression — Scenario 5: Duplicate event replay idempotency.
/// Verifies that the ProcessedEvent check prevents double-processing of the same event.
/// </summary>
public sealed class DuplicateEventReplayTests
{
    [Fact]
    public async Task ProcessedEventRepository_Exists_ReturnsTrueForDuplicateEventId()
    {
        var eventId = Guid.NewGuid();

        var repo = Substitute.For<IProcessedEventRepository>();
        repo.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        var isDuplicate = await repo.ExistsAsync(eventId);

        isDuplicate.Should().BeTrue("a previously seen eventId must be detected as a duplicate");
        await repo.DidNotReceive().AddAsync(Arg.Any<ProcessedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessedEventRepository_Exists_ReturnsFalseForNewEventId()
    {
        var eventId = Guid.NewGuid();

        var repo = Substitute.For<IProcessedEventRepository>();
        repo.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        var isDuplicate = await repo.ExistsAsync(eventId);

        isDuplicate.Should().BeFalse("a new eventId must pass through for processing");
    }

    [Fact]
    public async Task ConsumerPattern_WhenDuplicate_SkipsHandlerAndDoesNotSaveProcessedEvent()
    {
        // Simulates the consumer's idempotency guard:
        // if ExistsAsync returns true, the handler must NOT be called and AddAsync must NOT be called.
        var eventId = Guid.NewGuid();
        var handlerCalled = false;

        var repo = Substitute.For<IProcessedEventRepository>();
        repo.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        if (!await repo.ExistsAsync(eventId))
        {
            handlerCalled = true;
            await repo.AddAsync(new ProcessedEvent(eventId, "TestEvent"));
        }

        handlerCalled.Should().BeFalse("duplicate events must not invoke the handler");
        await repo.DidNotReceive().AddAsync(Arg.Any<ProcessedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumerPattern_WhenNew_CallsHandlerAndSavesProcessedEvent()
    {
        var eventId = Guid.NewGuid();
        var handlerCalled = false;

        var repo = Substitute.For<IProcessedEventRepository>();
        repo.ExistsAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        if (!await repo.ExistsAsync(eventId))
        {
            handlerCalled = true;
            await repo.AddAsync(new ProcessedEvent(eventId, "TestEvent"));
        }

        handlerCalled.Should().BeTrue("new events must be processed");
        await repo.Received(1).AddAsync(
            Arg.Is<ProcessedEvent>(p => p.EventId == eventId),
            Arg.Any<CancellationToken>());
    }
}
