using FluentValidation;

namespace BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;

public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AddedByUserId).NotEmpty();
    }
}
