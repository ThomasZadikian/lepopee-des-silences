using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.SyncPartySkills;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.SyncPartySkills;

/// <summary>
/// Grimoire "Valider les choix" mid-run resync — re-reads the freshest equipped-skill
/// selection from player-service and re-applies it to both the protagonist
/// (<see cref="Run.PlayerState"/>) and every companion's <see cref="RunCharacterSnapshot"/>.
/// </summary>
public sealed class SyncPartySkillsCommandHandlerTests
{
    private static PlayerRunSnapshot CreateFreshSnapshot(Guid playerId, Guid protagonistId, Guid companionId) =>
        new(playerId, "Test Player",
        [
            new PlayerRunSnapshotCharacter(
                protagonistId,
                "character.player.self",
                "Le Porteur",
                Stats: new PlayerRunSnapshotCharacterStats(
                    MaxVitality: 100, AttackPower: 12, Defense: 6, StartingGuard: 0,
                    Speed: 10, Initiative: 10,Focus: 0, Mana: 0, Charge: 0),
                Skills:
                [
                    new PlayerRunSnapshotCharacterSkill(
                        SkillDefinitionKey: "skill.new.protagonist",
                        DisplayName: "Nouveau sort du joueur",
                        SkillType: "Damage",
                        TargetingMode: "SingleEnemy",
                        EffectType: "Damage",
                        ManaCost: 0,
                        ChargeCost: 0,
                        BasePower: 12)
                ]),
            new PlayerRunSnapshotCharacter(
                companionId,
                "character.mane",
                "Mané",
                Stats: new PlayerRunSnapshotCharacterStats(
                    MaxVitality: 80, AttackPower: 10, Defense: 4, StartingGuard: 0,
                    Speed: 8, Initiative: 8,Focus: 0, Mana: 0, Charge: 0),
                Skills:
                [
                    new PlayerRunSnapshotCharacterSkill(
                        SkillDefinitionKey: "skill.new.companion",
                        DisplayName: "Nouveau sort du compagnon",
                        SkillType: "Damage",
                        TargetingMode: "SingleEnemy",
                        EffectType: "Damage",
                        ManaCost: 0,
                        ChargeCost: 0,
                        BasePower: 9)
                ])
        ]);

    private static Run CreateRunWithProtagonistAndCompanion(out Guid protagonistId, out Guid companionId)
    {
        var run = TestGameEngineFactory.CreateRun();
        protagonistId = Guid.NewGuid();
        companionId = Guid.NewGuid();

        var statBlock = RunCharacterStatSnapshot.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0);

        var protagonistSkills = new[]
        {
            RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.basic.strike", displayName: "Frappe",
                skillType: "Damage", targetingMode: "SingleEnemy", effectType: "Damage", basePower: 10,
                emotionalRegister: "Neutral")
        };
        var companionSkills = new[]
        {
            RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.basic.strike", displayName: "Frappe",
                skillType: "Damage", targetingMode: "SingleEnemy", effectType: "Damage", basePower: 10,
                emotionalRegister: "Neutral")
        };

        var protagonist = RunCharacterSnapshot.Create(
            characterId: protagonistId, definitionKey: "character.player.self", displayName: "Le Porteur",
            statBlock: statBlock, skills: protagonistSkills, emotionalRegisterCode: "Neutral");
        var companion = RunCharacterSnapshot.Create(
            characterId: companionId, definitionKey: "character.mane", displayName: "Mané",
            statBlock: statBlock, skills: companionSkills, emotionalRegisterCode: "Memoire");

        var snapshot = RunPlayerSnapshot.Create(
            playerId: run.PlayerId, displayName: "Joueur",
            characters: [protagonist, companion], createdAtUtc: DateTimeOffset.UtcNow);

        run.AttachPlayerSnapshot(snapshot);
        return run;
    }

    [Fact]
    public async Task Handle_ShouldReplaceProtagonistAndCompanionSkills_AndPersistTheRun()
    {
        var run = CreateRunWithProtagonistAndCompanion(out var protagonistId, out var companionId);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(run.PlayerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFreshSnapshot(run.PlayerId, protagonistId, companionId));

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetSkillDefinitionByKeyAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => new CatalogSkillDefinition(
                key,
                key == "skill.new.protagonist" ? "Nouveau sort du joueur" : "Nouveau sort du compagnon",
                "Definition Catalog",
                "Damage",
                "SingleEnemy",
                "Damage",
                0,
                0,
                key == "skill.new.protagonist" ? 12 : 9,
                [],
                "neutral"));
        var merger = new PlayerSkillMerger(catalogGateway.Object);

        var handler = new SyncPartySkillsCommandHandler(repo.Object, playerGateway.Object, merger);

        await handler.Handle(new SyncPartySkillsCommand(run.Id.Value), CancellationToken.None);

        run.PlayerState.Skills.Should().ContainSingle(s => s.Key == "skill.new.protagonist");
        run.PlayerSnapshot!.Characters.First(c => c.CharacterId == protagonistId).Skills
            .Should().ContainSingle(s => s.SkillDefinitionKey == "skill.new.protagonist");
        run.PlayerSnapshot!.Characters.First(c => c.CharacterId == companionId).Skills
            .Should().ContainSingle(s => s.SkillDefinitionKey == "skill.new.companion");
        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        var merger = new PlayerSkillMerger(new Mock<ICatalogContentGateway>().Object);
        var handler = new SyncPartySkillsCommandHandler(repo.Object, playerGateway.Object, merger);

        var act = () => handler.Handle(new SyncPartySkillsCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenCombatIsActive()
    {
        var run = CreateRunWithProtagonistAndCompanion(out _, out _);
        TestGameEngineFactory.EnterNode(run, run.CurrentRoom.AvailableNodes.First());
        var ally = Combatant.CreateAlly("p", "Player", "Fighter", 40);
        var enemy = Combatant.CreateEnemy("e", "Enemy", "Guard", 30);
        var combat = TestTacticalCombatHelper.Create(run.Id, RoomId.New(), NodeId.New(), [ally], [enemy]);
        run.StartTacticalCombat(combat);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        var merger = new PlayerSkillMerger(new Mock<ICatalogContentGateway>().Object);
        var handler = new SyncPartySkillsCommandHandler(repo.Object, playerGateway.Object, merger);

        var act = () => handler.Handle(new SyncPartySkillsCommand(run.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot sync party skills while a combat is active.");
    }
}
