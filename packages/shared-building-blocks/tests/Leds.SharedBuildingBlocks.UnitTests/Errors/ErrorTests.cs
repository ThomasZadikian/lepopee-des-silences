using FluentAssertions;
using Leds.SharedBuildingBlocks.Errors;
using Xunit;

namespace Leds.SharedBuildingBlocks.UnitTests.Errors;

public sealed class ErrorTests
{
    [Fact]
    public void None_ShouldRepresentAbsenceOfError()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
        Error.None.IsNone.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimCodeAndMessage()
    {
        var error = Error.Create("  validation.required  ", "  Value is required.  ");

        error.Code.Should().Be("validation.required");
        error.Message.Should().Be("Value is required.");
        error.IsNone.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrow_WhenCodeIsBlank(string code)
    {
        var act = () => Error.Create(code, "Message");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Error code is required.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrow_WhenMessageIsBlank(string message)
    {
        var act = () => Error.Create("error.code", message);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Error message is required.*");
    }
}