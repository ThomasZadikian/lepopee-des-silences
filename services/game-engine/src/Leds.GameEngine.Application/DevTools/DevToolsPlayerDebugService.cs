using Leds.GameEngine.Application.Players.Ports;

namespace Leds.GameEngine.Application.DevTools;

public sealed class DevToolsPlayerDebugService : IDevToolsPlayerDebugService
{
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public DevToolsPlayerDebugService(IPlayerProfileGateway playerProfileGateway)
    {
        _playerProfileGateway = playerProfileGateway;
    }

    public async Task<DevToolsPlayerDebugResult> UnlockSkillAsync(
        Guid playerId, Guid characterId, string skillKey, CancellationToken cancellationToken = default)
    {
        var profile = await _playerProfileGateway.UnlockSkillAsync(playerId, characterId, skillKey, cancellationToken);
        return new DevToolsPlayerDebugResult($"Skill '{skillKey}' unlocked.", profile);
    }

}
