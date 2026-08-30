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

    [Fact]
    public void Success_WithValueType_ShouldCreateSuccessfulResult()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WithValueType_ShouldCreateFailedResult()
    {
        var error = Error.Create("calculation.overflow", "Calculation overflowed.");
        var result = Result<int>.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_WithReferenceType_ShouldPreserveObjectIdentity()
    {
        var obj = new object();
        var result = Result<object>.Success(obj);

        result.Value.Should().BeSameAs(obj);
    }

    [Fact]
    public void ResultT_ShouldInheritFromResult()
    {
        var result = Result<string>.Success("value");

        result.Should().BeOfType<Result<string>>();
        result.Should().BeAssignableTo<Result>();
    }
}