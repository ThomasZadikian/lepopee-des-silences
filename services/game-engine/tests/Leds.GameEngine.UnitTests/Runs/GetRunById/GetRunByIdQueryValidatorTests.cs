using FluentAssertions;
using Leds.GameEngine.Application.Runs.GetRunById;

namespace Leds.GameEngine.UnitTests.Runs.GetRunById;

public sealed class GetRunByIdQueryValidatorTests
{
    private readonly GetRunByIdQueryValidator _validator = new();

    private static readonly GetRunByIdQuery ValidQuery = new(Guid.NewGuid());

    [Fact]
    public void ShouldHaveError_WhenRunIdIsEmpty()
    {
        var query = ValidQuery with { RunId = Guid.Empty };

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Run id"));
    }

    [Fact]
    public void ShouldNotHaveError_WhenQueryIsValid()
    {
        var result = _validator.Validate(ValidQuery);

        result.IsValid.Should().BeTrue();
    }
}
