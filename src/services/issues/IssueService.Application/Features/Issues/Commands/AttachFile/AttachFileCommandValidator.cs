using FluentValidation;

namespace BitirmeProject.IssueService.Application.Features.Issues.Commands.AttachFile;

public sealed class AttachFileCommandValidator : AbstractValidator<AttachFileCommand>
{
    public AttachFileCommandValidator()
    {
        RuleFor(x => x.IssueId).NotEmpty();
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.UploadedByUserId).NotEmpty();
    }
}
