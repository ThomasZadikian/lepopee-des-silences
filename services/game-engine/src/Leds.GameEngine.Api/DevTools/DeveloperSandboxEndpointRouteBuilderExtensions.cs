using System.Security.Claims;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.Runs;
using Leds.SharedBuildingBlocks.Time;
using MediatR;

namespace Leds.GameEngine.Api.DevTools;

public static class DeveloperSandboxEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapDeveloperSandbox(this IEndpointRouteBuilder endpoints)
    {
        var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var options = DevToolsOptions.FromConfiguration(configuration);

        if (!environment.IsDevelopment() || !options.Enabled)
            return endpoints;

        var group = endpoints
            .MapGroup("/api/dev/v2/sandboxes")
            .WithTags("Developer Sandbox")
            .RequireAuthorization(policy => policy.RequireRole("Developer", "Administrator"));

        group.MapPost("/reset", async Task<IResult> (
            DeveloperSandboxRequest request,
            ClaimsPrincipal user,
            IRunRepository repository,
            IClock clock,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var subject = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(subject, out var playerId))
                return Results.Unauthorized();

            var existing = await repository.GetOpenByPlayerIdAsync(
                playerId,
                RunMode.DeveloperSandbox,
                cancellationToken);
            if (existing is not null)
            {
                existing.Abandon(clock.UtcNow);
                await repository.UpdateAsync(existing, cancellationToken);
            }

            var response = await sender.Send(
                new StartRunCommand(
                    PlayerId: playerId,
                    DifficultyLevel: null,
                    CharacterId: request.CharacterId,
                    Mode: RunMode.DeveloperSandbox),
                cancellationToken);

            return Results.Ok(response);
        });

        return endpoints;
    }
}

public sealed record DeveloperSandboxRequest(Guid CharacterId);
