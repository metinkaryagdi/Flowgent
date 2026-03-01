using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;
using FluentAssertions;

namespace NotificationService.UnitTests.Application.Validators;

public sealed class MarkNotificationReadCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenNotificationMissing()
    {
        var validator = new MarkNotificationReadCommandValidator();
        var command = new MarkNotificationReadCommand(Guid.Empty, Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenUserMissing()
    {
        var validator = new MarkNotificationReadCommandValidator();
        var command = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new MarkNotificationReadCommandValidator();
        var command = new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
