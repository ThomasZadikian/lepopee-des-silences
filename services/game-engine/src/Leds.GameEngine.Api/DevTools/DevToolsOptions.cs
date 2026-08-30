using Microsoft.Extensions.Configuration;

namespace Leds.GameEngine.Api.DevTools;

public sealed class DevToolsOptions
{
    public const string SectionName = "DevTools";

    public bool Enabled { get; init; }

    public string Token { get; init; } = string.Empty;

    public static DevToolsOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<DevToolsOptions>() ?? new DevToolsOptions();
        var envEnabled = Environment.GetEnvironmentVariable("GAME_ENGINE_DEVTOOLS_ENABLED");
        var envToken = Environment.GetEnvironmentVariable("GAME_ENGINE_DEVTOOLS_TOKEN");

        return new DevToolsOptions
        {
            Enabled = bool.TryParse(envEnabled, out var enabled)
                ? enabled
                : options.Enabled,
            Token = string.IsNullOrEmpty(envToken)
                ? options.Token
                : envToken
        };
    }
}
