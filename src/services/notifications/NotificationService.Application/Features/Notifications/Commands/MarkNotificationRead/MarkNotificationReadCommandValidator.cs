using FluentValidation;

namespace BitirmeProject.NotificationService.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
