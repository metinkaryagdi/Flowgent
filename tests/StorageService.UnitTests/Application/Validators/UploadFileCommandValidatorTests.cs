using BitirmeProject.StorageService.Application.Features.Files.Commands.UploadFile;
using FluentAssertions;

namespace StorageService.UnitTests.Application.Validators;

public sealed class UploadFileCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenFileNameMissing()
    {
        var validator = new UploadFileCommandValidator();
        var command = new UploadFileCommand("", "text/plain", 1, "path", Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenContentTypeMissing()
    {
        var validator = new UploadFileCommandValidator();
        var command = new UploadFileCommand("file", "", 1, "path", Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenSizeInvalid()
    {
        var validator = new UploadFileCommandValidator();
        var command = new UploadFileCommand("file", "text/plain", 0, "path", Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new UploadFileCommandValidator();
        var command = new UploadFileCommand("file", "text/plain", 1, "path", Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
