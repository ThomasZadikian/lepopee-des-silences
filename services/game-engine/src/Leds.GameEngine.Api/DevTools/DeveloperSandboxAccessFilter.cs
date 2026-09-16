using System.Security.Claims;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Api.DevTools;

/// <summary>
/// Prevents debug mutations from escaping the authenticated developer's sandbox.
/// Routes without a run or player identifier (for example /status) are left untouched.
/// </summary>
public sealed class DeveloperSandboxAccessFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var subject = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subject, out var authenticatedPlayerId))
            return Results.Unauthorized();

        if (TryReadGuidRouteValue(httpContext, "playerId", out var requestedPlayerId) &&
            requestedPlayerId != authenticatedPlayerId)
        {
            return Results.Forbid();
        }

        if (!TryReadGuidRouteValue(httpContext, "runId", out var runId))
            return await next(context);

        var repository = httpContext.RequestServices.GetRequiredService<IRunRepository>();
        var run = await repository.GetByIdAsync(new RunId(runId), httpContext.RequestAborted);

        if (run is null)
            return Results.NotFound();

        if (run.PlayerId != authenticatedPlayerId || run.Mode != RunMode.DeveloperSandbox)
            return Results.Forbid();

        return await next(context);
    }

    private static bool TryReadGuidRouteValue(HttpContext context, string key, out Guid value)
    {
        value = Guid.Empty;
        return context.Request.RouteValues.TryGetValue(key, out var raw) &&
               Guid.TryParse(raw?.ToString(), out value);
    }
}
