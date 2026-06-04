using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using RPG_ESI07.API.Middleware;
using RPG_ESI07.Application.Responses;
using System.Text.Json;

namespace RPG_ESI07.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
    }

    private async Task<(HttpContext Context, ApiResponse<object>? ResponseData)> ExecuteMiddlewareAsync(Exception? exceptionToThrow)
    {
        RequestDelegate next = (HttpContext ctx) =>
        {
            if (exceptionToThrow != null)
                throw exceptionToThrow;

            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);
        var context = new DefaultHttpContext();

        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        ApiResponse<object>? responseData = null;
        if (!string.IsNullOrEmpty(responseBody))
        {
            responseData = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, _jsonOptions);
        }

        return (context, responseData);
    }

    [Fact]
    public async Task InvokeAsync_NoException_ProceedsToNextMiddleware()
    {
        var (context, responseData) = await ExecuteMiddlewareAsync(null);

        context.Response.StatusCode.Should().Be(200);
        responseData.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500InternalServerError()
    {
        var exceptionMessage = "Database connection failed";
        var exception = new Exception(exceptionMessage);

        var (context, responseData) = await ExecuteMiddlewareAsync(exception);

        context.Response.StatusCode.Should().Be(500);
        context.Response.ContentType.Should().Be("application/json; charset=utf-8");

        responseData.Should().NotBeNull();
        responseData!.Success.Should().BeFalse();
        responseData.Message.Should().Be("An internal server error occurred");
        responseData.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullException_Returns400BadRequest()
    {
        var exception = new ArgumentNullException("ParamName", "Parameter cannot be null");

        var (context, responseData) = await ExecuteMiddlewareAsync(exception);

        context.Response.StatusCode.Should().Be(400);

        responseData.Should().NotBeNull();
        responseData!.Message.Should().Be("Invalid input");
        responseData.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404NotFound()
    {
        var exceptionMessage = "Entity with ID 5 not found";
        var exception = new KeyNotFoundException(exceptionMessage);

        var (context, responseData) = await ExecuteMiddlewareAsync(exception);

        context.Response.StatusCode.Should().Be(404);

        responseData.Should().NotBeNull();
        responseData!.Message.Should().Be("Resource not found");
        responseData.Errors.Should().BeEmpty();
    }
}