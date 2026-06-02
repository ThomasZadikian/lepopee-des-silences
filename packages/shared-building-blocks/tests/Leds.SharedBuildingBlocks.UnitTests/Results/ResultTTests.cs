using FluentAssertions;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Xunit;

namespace Leds.SharedBuildingBlocks.UnitTests.Results;

public sealed class ResultTTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResultWithValue()
    {
        var result = Result<string>.Success("value");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("value");
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Success_ShouldThrow_WhenValueIsNull()
    {
        var act = () => Result<string>.Success(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = Error.Create("catalog.not_found", "Catalog content was not found.");

        var result = Result<string>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_ShouldThrow_WhenResultIsFailure()
    {
        var error = Error.Create("catalog.not_found", "Catalog content was not found.");
        var result = Result<string>.Failure(error);

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access the value of a failed result.");
    }
}