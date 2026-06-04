using FluentAssertions;
using RPG_ESI07.Application.Results;

namespace RPG_ESI07.Tests.Application.Results;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Failure_CreatesFailedResult()
    {
        var result = Result.Failure("Error");
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Error");
    }

    [Fact]
    public void SuccessGeneric_CreatesSuccessfulResultWithValue()
    {
        var result = Result.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitOperator_ConvertsValueToSuccessResult()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitOperator_ConvertsStringToFailedResult()
    {
        Result<int> result = "Error message";

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Error message");
    }
}