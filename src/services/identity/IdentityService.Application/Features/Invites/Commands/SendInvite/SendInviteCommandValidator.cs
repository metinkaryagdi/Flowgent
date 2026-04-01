using BitirmeProject.IdentityService.Domain.Enums;
using FluentValidation;

namespace BitirmeProject.IdentityService.Application.Features.Invites.Commands.SendInvite;

public sealed class SendInviteCommandValidator : AbstractValidator<SendInviteCommand>
{
    public SendInviteCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Role)
            .NotEqual(OrganizationRole.Owner).WithMessage("Cannot invite a user as Owner.")
            .IsInEnum().WithMessage("Invalid role.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is required.");

        RuleFor(x => x.InvitedByUserId)
            .NotEmpty().WithMessage("InvitedByUserId is required.");
    }
}
