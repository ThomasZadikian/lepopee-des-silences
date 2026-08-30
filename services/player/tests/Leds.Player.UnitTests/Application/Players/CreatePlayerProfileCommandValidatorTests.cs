using FluentAssertions;
using FluentValidation.TestHelper;
using Leds.Player.Application.Players.CreatePlayerProfile;

namespace Leds.Player.UnitTests.Application.Players;

public sealed class CreatePlayerProfileCommandValidatorTests
{
    private readonly CreatePlayerProfileCommandValidator _validator = new();

    [Fact]
    public void ShouldHaveError_WhenDisplayNameIsEmpty()
    {
        var command = new CreatePlayerProfileCommand("");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Player display name is required.");
    }

    [Fact]
    public void ShouldHaveError_WhenDisplayNameIsNull()
    {
        var command = new CreatePlayerProfileCommand(null!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Player display name is required.");
    }

    [Fact]
    public void ShouldHaveError_WhenDisplayNameIsWhitespace()
    {
        var command = new CreatePlayerProfileCommand("   ");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Player display name is required.");
    }

    [Fact]
    public void ShouldHaveError_WhenDisplayNameExceedsMaxLength()
    {
        var command = new CreatePlayerProfileCommand(new string('a', 129));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Player display name must not exceed 128 characters.");
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameIsValid()
    {
        var command = new CreatePlayerProfileCommand("Valid Player Name");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameIsExactly128Characters()
    {
        var command = new CreatePlayerProfileCommand(new string('a', 128));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameIsSingleCharacter()
    {
        var command = new CreatePlayerProfileCommand("A");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameContainsSpaces()
    {
        var command = new CreatePlayerProfileCommand("John Doe");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameContainsSpecialCharacters()
    {
        var command = new CreatePlayerProfileCommand("Player-Name_123");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDisplayNameContainsUnicodeCharacters()
    {
        var command = new CreatePlayerProfileCommand("Jøhn Døe");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void ShouldHaveError_WhenDisplayNameIsVariousWhitespaceInputs(string displayName)
    {
        var command = new CreatePlayerProfileCommand(displayName);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData(129)]
    [InlineData(150)]
    [InlineData(200)]
    [InlineData(1000)]
    public void ShouldHaveError_WhenDisplayNameExceedsVariousLengths(int length)
    {
        var command = new CreatePlayerProfileCommand(new string('a', length));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Player display name must not exceed 128 characters.");
    }

    [Fact]
    public void ShouldHaveErrorCount_WhenDisplayNameIsNullOrEmpty()
    {
        var command = new CreatePlayerProfileCommand("");

        var result = _validator.TestValidate(command);

        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void ShouldHaveErrorCount_WhenDisplayNameIsTooLong()
    {
        var command = new CreatePlayerProfileCommand(new string('a', 129));

        var result = _validator.TestValidate(command);

        result.Errors.Should().HaveCount(1);
    }
}
