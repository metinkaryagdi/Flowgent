using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.RemoveMember;

public sealed class RemoveMemberCommandValidator : AbstractValidator<RemoveMemberCommand>
{
    public RemoveMemberCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
