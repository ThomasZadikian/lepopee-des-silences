using Leds.GameEngine.Application.Players.Ports;

namespace Leds.GameEngine.Infrastructure.Players;

public sealed class InMemoryPlayerRunSnapshotGateway : IPlayerRunSnapshotGateway
{
    public Task<PlayerRunSnapshot> GetRunSnapshotAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var snapshot = new PlayerRunSnapshot(
            PlayerId: playerId,
            DisplayName: "Joueur",
            Characters:
            [
                new PlayerRunSnapshotCharacter(
                    CharacterId: Guid.NewGuid(),
                    DefinitionKey: "character.player.self",
                    DisplayName: "Le Porteur",
                    MaxVitality: 100,
                    BaseMana: 0,
                    BaseCharge: 0,
                    SkillKeys: ["skill.basic.strike", "skill.basic.guard"])
            ]);

        return Task.FromResult(snapshot);
    }
}