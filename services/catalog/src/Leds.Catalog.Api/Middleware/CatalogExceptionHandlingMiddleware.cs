using FluentValidation;

namespace Leds.Catalog.Api.Middleware;

public sealed class CatalogExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public CatalogExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                title = "Validation failed.",
                status = StatusCodes.Status400BadRequest,
                errors = exception.Errors
                    .Select(error => error.ErrorMessage)
                    .ToArray()
            });
        }
    }
}