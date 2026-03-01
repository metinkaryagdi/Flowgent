using BitirmeProject.NotificationService.Application.Features.Notifications.Commands.CreateNotification;
using FluentAssertions;

namespace NotificationService.UnitTests.Application.Validators;

public sealed class CreateNotificationCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenUserMissing()
    {
        var validator = new CreateNotificationCommandValidator();
        var command = new CreateNotificationCommand(Guid.Empty, "Title", "Body", "InApp", "Issue", Guid.NewGuid(), null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenTitleMissing()
    {
        var validator = new CreateNotificationCommandValidator();
        var command = new CreateNotificationCommand(Guid.NewGuid(), "", "Body", "InApp", "Issue", Guid.NewGuid(), null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenMessageMissing()
    {
        var validator = new CreateNotificationCommandValidator();
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "", "InApp", "Issue", Guid.NewGuid(), null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenChannelMissing()
    {
        var validator = new CreateNotificationCommandValidator();
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Body", "", "Issue", Guid.NewGuid(), null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new CreateNotificationCommandValidator();
        var command = new CreateNotificationCommand(Guid.NewGuid(), "Title", "Body", "InApp", "Issue", Guid.NewGuid(), null, null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
