using FluentValidation.TestHelper;
using RPG_ESI07.Application.Commands.Auth;
using RPG_ESI07.Application.Validators;

namespace RPG_ESI07.Tests.Application.Validators;

public class ValidatorsTests
{
    private readonly RegisterValidator _registerValidator = new();
    private readonly LoginValidator _loginValidator = new();

    [Fact]
    public void RegisterValidator_ShouldHaveError_WhenUsernameIsTooShort()
    {
        var command = new RegisterCommand("ab", "test@test.com", "Password123!");
        var result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void RegisterValidator_ShouldHaveError_WhenPasswordLacksRequirements()
    {
        var command = new RegisterCommand("valid_user", "test@test.com", "weakpass");
        var result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("One uppercase required");
    }

    [Fact]
    public void RegisterValidator_ShouldNotHaveError_WhenModelIsValid()
    {
        var command = new RegisterCommand("valid_user", "test@test.com", "StrongPass1!");
        var result = _registerValidator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginValidator_ShouldHaveError_WhenFieldsAreEmpty()
    {
        var command = new LoginCommand("", "");
        var result = _loginValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("root")]
    [InlineData("admin")]
    [InlineData("administrator")]
    [InlineData("superuser")]
    [InlineData("system")]
    [InlineData("ROOT")]
    [InlineData("Admin")]
    public void RegisterValidator_ShouldHaveError_WhenUsernameIsReserved(string username)
    {
        var command = new RegisterCommand(username, "test@test.com", "StrongPass1!");
        var result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Ce nom d'utilisateur est réservé.");
    }

    [Theory]
    [InlineData("devuser")]
    [InlineData("adminuser")]
    [InlineData("testplayer")]
    public void RegisterValidator_ShouldHaveError_WhenUsernameIsSeederAccount(string username)
    {
        var command = new RegisterCommand(username, "test@test.com", "StrongPass1!");
        var result = _registerValidator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Username)
              .WithErrorMessage("Ce nom d'utilisateur est réservé.");
    }
}