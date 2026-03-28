using FluentValidation;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;

public sealed class AttachFileCommandValidator : AbstractValidator<AttachFileCommand>
{
    public AttachFileCommandValidator()
    {
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.UploadedByUserId).NotEmpty();
    }
}
