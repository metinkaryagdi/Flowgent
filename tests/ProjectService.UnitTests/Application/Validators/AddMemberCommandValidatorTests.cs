using BitirmeProject.ProjectService.Domain.Enums;
using BitirmeProject.ProjectService.Application.Features.Projects.Commands.AddMember;
using FluentAssertions;

namespace ProjectService.UnitTests.Application.Validators;

public sealed class AddMemberCommandValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenProjectMissing()
    {
        var validator = new AddMemberCommandValidator();
        var command = new AddMemberCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null, ProjectMemberRole.Member);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenUserMissing()
    {
        var validator = new AddMemberCommandValidator();
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null, ProjectMemberRole.Member);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenAddedByMissing()
    {
        var validator = new AddMemberCommandValidator();
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, ProjectMemberRole.Member);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Passes_WhenValid()
    {
        var validator = new AddMemberCommandValidator();
        var command = new AddMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, ProjectMemberRole.Member);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
