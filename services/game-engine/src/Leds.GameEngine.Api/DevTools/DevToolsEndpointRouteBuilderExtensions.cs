using Leds.GameEngine.Application.DevTools;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Leds.GameEngine.Api.DevTools;

public static class DevToolsEndpointRouteBuilderExtensions
{
    private const string TokenHeaderName = "X-Leds-DevTools-Token";

    public static IEndpointRouteBuilder MapGameEngineDevTools(this IEndpointRouteBuilder endpoints)
    {
        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var options = DevToolsOptions.FromConfiguration(configuration);

        if (!environment.IsDevelopment() || !options.Enabled)
            return endpoints;

        var group = endpoints
            .MapGroup("/api/dev/v2")
            .WithTags("Development Tools")
            .AddEndpointFilter(async (context, next) =>
            {
                var configuredToken = options.Token;
                var providedToken = context.HttpContext.Request.Headers[TokenHeaderName].ToString();

                if (string.IsNullOrWhiteSpace(configuredToken) ||
                    string.IsNullOrWhiteSpace(providedToken) ||
                    !string.Equals(configuredToken, providedToken, StringComparison.Ordinal))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return await next(context);
            });

        group.MapGet("/status", (IWebHostEnvironment env) =>
            TypedResults.Ok(new DevToolsStatusResponse(true, env.EnvironmentName)));

        group.MapPost("/runs/{runId:guid}/advance-room", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AdvanceRoomAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/advance-rooms", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            DevToolsAdvanceRoomsRequest request,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AdvanceRoomsAsync(runId, request.Count, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/current-room/palace-state", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            DevToolsSetPalaceStateRequest request,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ForceCurrentRoomPalaceStateAsync(runId, request.State, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/current-room/climate", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            DevToolsSetClimateRequest request,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ForceCurrentRoomClimateAsync(runId, request.Climate, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/laws/activate", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            DevToolsActivateLawRequest request,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ActivatePalaceLawAsync(runId, request.LawKey, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/laws/clear", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ClearPalaceLawsAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/curses/activate", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            DevToolsActivateCurseRequest request,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ActivateCurseAsync(runId, request.CurseKey, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/curses/clear", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ClearCursesAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/combats/current/kill-enemies", async Task<Ok<DevToolsCombatDebugResult>> (
            Guid runId,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.KillAllCurrentCombatEnemiesAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/combats/current/enemies/{combatantId:guid}/kill", async Task<Ok<DevToolsCombatDebugResult>> (
            Guid runId,
            Guid combatantId,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.KillCurrentCombatEnemyAsync(runId, combatantId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/combats/current/combatants/{combatantId:guid}/set-vitals", async Task<Ok<DevToolsCombatDebugResult>> (
            Guid runId,
            Guid combatantId,
            DevToolsSetVitalsRequest request,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.SetCurrentCombatantVitalsAsync(
                runId, combatantId, request.Vitality, request.Guard, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapGet("/runs/{runId:guid}/psyche", async Task<Ok<DevToolsRunPsycheResult>> (
    Guid runId,
    IDevToolsPsycheService service,
    CancellationToken cancellationToken) =>
        {
            var result = await service.GetPsycheAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/party/add-ally", async Task<Ok<DevToolsRunDebugResult>> (
    Guid runId,
    IDevToolsRunDebugService service,
    CancellationToken cancellationToken) =>
        {
            var result = await service.AddDebugAllyAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        group.MapPost("/runs/{runId:guid}/party/remove-ally", async Task<Ok<DevToolsRunDebugResult>> (
            Guid runId,
            IDevToolsRunDebugService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RemoveDebugAllyAsync(runId, cancellationToken);
            return TypedResults.Ok(result);
        });

        return endpoints;
    }
}

public sealed record DevToolsStatusResponse(bool Enabled, string Environment);

public sealed record DevToolsAdvanceRoomsRequest(int Count);

public sealed record DevToolsSetPalaceStateRequest(string State);

public sealed record DevToolsSetClimateRequest(string Climate);

public sealed record DevToolsActivateLawRequest(string LawKey);

public sealed record DevToolsActivateCurseRequest(string CurseKey);

public sealed record DevToolsSetVitalsRequest(int Vitality, int Guard);
