using FluentValidation.TestHelper;
using RPG_ESI07.Application.Queries.Leaderboard;
using RPG_ESI07.Application.Validators;

namespace RPG_ESI07.Tests.Infrastructure.Queries;

public class SearchLeaderboardQueryValidatorTests
{
    private readonly SearchLeaderboardQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidTerm_PassesValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery("Kael"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyTerm_FailsValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery(""));
        result.ShouldHaveValidationErrorFor(x => x.Term);
    }

    [Fact]
    public void Validate_TooShortTerm_FailsValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery("K"));
        result.ShouldHaveValidationErrorFor(x => x.Term)
              .WithErrorMessage("La recherche doit contenir au moins 2 caractères.");
    }

    [Fact]
    public void Validate_TooLongTerm_FailsValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery(new string('a', 51)));
        result.ShouldHaveValidationErrorFor(x => x.Term)
              .WithErrorMessage("La recherche ne peut pas dépasser 50 caractères.");
    }

    [Fact]
    public void Validate_InvalidCharacters_FailsValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery("<script>"));
        result.ShouldHaveValidationErrorFor(x => x.Term)
              .WithErrorMessage("Caractères non autorisés.");
    }

    [Fact]
    public void Validate_TermWithSpaces_PassesValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery("Kael le Brave"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TermWithHyphen_PassesValidation()
    {
        var result = _validator.TestValidate(new SearchLeaderboardQuery("Kael-le-Brave"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}