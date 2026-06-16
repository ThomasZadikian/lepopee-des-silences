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
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Recovery: 5,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills:
                    [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])
            ]);

        return Task.FromResult(snapshot);
    }
}
