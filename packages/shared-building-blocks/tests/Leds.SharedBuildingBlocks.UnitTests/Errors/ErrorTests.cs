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

    [Fact]
    public void Create_ShouldThrow_WhenCodeIsNull()
    {
        var act = () => Error.Create(null!, "Message");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Error code is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenMessageIsNull()
    {
        var act = () => Error.Create("error.code", null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Error message is required.*");
    }

    [Fact]
    public void ErrorsWithSameCodeAndMessage_ShouldBeEqual()
    {
        var error1 = Error.Create("validation.required", "Value is required.");
        var error2 = Error.Create("validation.required", "Value is required.");

        error1.Should().Be(error2);
        error1.Should().NotBeSameAs(error2);
    }

    [Fact]
    public void ErrorsWithDifferentCode_ShouldNotBeEqual()
    {
        var error1 = Error.Create("validation.required", "Value is required.");
        var error2 = Error.Create("validation.missing", "Value is required.");

        error1.Should().NotBe(error2);
    }

    [Fact]
    public void ErrorsWithDifferentMessage_ShouldNotBeEqual()
    {
        var error1 = Error.Create("validation.required", "Value is required.");
        var error2 = Error.Create("validation.required", "Field is required.");

        error1.Should().NotBe(error2);
    }

    [Fact]
    public void None_ShouldEqualItself()
    {
        Error.None.Should().Be(Error.None);
    }

    [Fact]
    public void None_ShouldNotEqualCreatedError()
    {
        var error = Error.Create("some.code", "Some message.");

        Error.None.Should().NotBe(error);
    }
}