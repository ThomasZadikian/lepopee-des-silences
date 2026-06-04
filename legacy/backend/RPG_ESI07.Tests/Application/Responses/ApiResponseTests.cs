using FluentAssertions;
using RPG_ESI07.Application.Responses;

namespace RPG_ESI07.Tests.Application.Responses;

public class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_CreatesValidInstance_WithDataAndDefaultMessage()
    {
        var data = "TestData";

        var response = ApiResponse<string>.SuccessResponse(data);

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Success");
        response.Data.Should().Be(data);
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SuccessResponse_CreatesValidInstance_WithCustomMessage()
    {
        var data = 42;
        var customMessage = "Operation completed";

        var response = ApiResponse<int>.SuccessResponse(data, customMessage);

        response.Success.Should().BeTrue();
        response.Message.Should().Be(customMessage);
        response.Data.Should().Be(data);
        response.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ErrorResponse_CreatesValidInstance_WithErrorList()
    {
        var errorMessage = "Validation Failed";
        var errorDetail = "Name is required";

        var response = ApiResponse<object>.ErrorResponse(errorMessage, errorDetail);

        response.Success.Should().BeFalse();
        response.Message.Should().Be(errorMessage);
        response.Data.Should().BeNull();
        response.Errors.Should().ContainSingle().Which.Should().Be(errorDetail);
    }
}