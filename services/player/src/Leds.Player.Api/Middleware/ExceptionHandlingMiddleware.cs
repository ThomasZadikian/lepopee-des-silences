using Leds.Player.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Leds.Player.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteProblemDetails(context, 400, "Validation failed.",
                new Dictionary<string, object>
                {
                    ["errors"] = ex.Errors.Select(e => e.ErrorMessage).ToArray()
                });
        }
        catch (Leds.Player.Domain.Common.DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated");
            await WriteProblemDetails(context, 400, ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteProblemDetails(context, 404, ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Business rule conflict");
            await WriteProblemDetails(context, 409, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected server error");
            await WriteProblemDetails(context, 500, "Unexpected server error.");
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context,
        int status,
        string title,
        IDictionary<string, object>? extensions = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.Request.Path
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        await context.Response.WriteAsJsonAsync(problem);
    }
}
