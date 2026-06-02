using FluentAssertions;
using Leds.SharedBuildingBlocks.Errors;
using Leds.SharedBuildingBlocks.Results;
using Xunit;

namespace Leds.SharedBuildingBlocks.UnitTests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = Error.Create("validation.required", "Value is required.");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_ShouldThrow_WhenErrorIsNone()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("A failed result must contain an error.");
    }
}