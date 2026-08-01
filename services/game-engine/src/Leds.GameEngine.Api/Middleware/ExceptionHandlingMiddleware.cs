using FluentValidation;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Leds.GameEngine.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                exception.Errors.Select(error => error.ErrorMessage).ToArray());
        }
        catch (ConflictException exception)
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                new[] { exception.Message });
        }
        catch (DomainException exception)
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Domain rule violated.",
                new[] { exception.Message });
        }
        catch (NotFoundException exception)
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status404NotFound,
                "Resource not found.",
                new[] { exception.Message });
        }
        catch (Exception exception)
        {
            var correlationId = context.Items["CorrelationId"] as string;
            _logger.LogError(exception, "Unhandled exception occurred. CorrelationId={CorrelationId}", correlationId);

            // Development-only: surface the real exception in the response body so it's
            // visible directly in a failing test's assertion message, since ILogger output
            // isn't reliably captured by every test runner/host combination. Production
            // (any environment other than Development) keeps the generic message.
            var errors = _environment.IsDevelopment()
                ? new[] { exception.ToString() }
                : new[] { "An unexpected error occurred." };

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Unexpected server error.",
                errors);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        IReadOnlyCollection<string> errors)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["errors"] = errors;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}