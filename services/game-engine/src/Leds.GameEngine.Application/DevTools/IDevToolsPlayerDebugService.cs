using Leds.GameEngine.Application.Players;

namespace Leds.GameEngine.Application.DevTools;

public interface IDevToolsPlayerDebugService
{
    Task<DevToolsPlayerDebugResult> UnlockSkillAsync(Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken = default);

}

public sealed record DevToolsPlayerDebugResult(string Message, PlayerProfileView Profile);
