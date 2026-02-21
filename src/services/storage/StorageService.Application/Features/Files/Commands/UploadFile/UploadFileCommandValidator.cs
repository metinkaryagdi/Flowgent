using FluentValidation;

namespace BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;

public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
        RuleFor(x => x.StoragePath).NotEmpty().MaximumLength(500);
        RuleFor(x => x.UploadedByUserId).NotEmpty();
    }
}
