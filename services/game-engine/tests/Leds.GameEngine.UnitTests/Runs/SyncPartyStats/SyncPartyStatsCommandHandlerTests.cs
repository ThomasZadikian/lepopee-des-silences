using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.SyncPartyStats;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.SyncPartyStats;

/// <summary>
/// Stat-point "Valider les choix" mid-run resync — re-reads the freshest stat
/// allocation from player-service and re-applies it to both the protagonist
/// (<see cref="Run.PlayerState"/> + <see cref="Run.Attack"/>/etc) and every
/// companion's <see cref="RunCharacterSnapshot"/>.
/// </summary>
public sealed class SyncPartyStatsCommandHandlerTests
{
    private static PlayerRunSnapshot CreateFreshSnapshot(Guid playerId, Guid protagonistId, Guid companionId) =>
        new(playerId, "Test Player",
        [
            new PlayerRunSnapshotCharacter(
                protagonistId,
                "character.player.self",
                "Le Porteur",
                Stats: new PlayerRunSnapshotCharacterStats(
                    MaxVitality: 150, AttackPower: 20, Defense: 9, StartingGuard: 0,
                    Speed: 14, Initiative: 10, Recovery: 5, Focus: 3, Mana: 25, Charge: 4),
                Skills: []),
            new PlayerRunSnapshotCharacter(
                companionId,
                "character.mane",
                "Mané",
                Stats: new PlayerRunSnapshotCharacterStats(
                    MaxVitality: 95, AttackPower: 16, Defense: 5, StartingGuard: 2,
                    Speed: 9, Initiative: 8, Recovery: 4, Focus: 1, Mana: 5, Charge: 1),
                Skills: [])
        ]);

    private static Run CreateRunWithProtagonistAndCompanion(out Guid protagonistId, out Guid companionId)
    {
        var run = TestGameEngineFactory.CreateRun();
        protagonistId = Guid.NewGuid();
        companionId = Guid.NewGuid();

        var statBlock = RunCharacterStatSnapshot.Create(
            maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
            speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0);

        var skills = new[]
        {
            RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.basic.strike", displayName: "Frappe",
                skillType: "Damage", targetingMode: "SingleEnemy", effectType: "Damage", basePower: 10)
        };

        var protagonist = RunCharacterSnapshot.Create(
            characterId: protagonistId, definitionKey: "character.player.self", displayName: "Le Porteur",
            statBlock: statBlock, skills: skills);
        var companion = RunCharacterSnapshot.Create(
            characterId: companionId, definitionKey: "character.mane", displayName: "Mané",
            statBlock: statBlock, skills: skills);

        var snapshot = RunPlayerSnapshot.Create(
            playerId: run.PlayerId, displayName: "Joueur",
            characters: [protagonist, companion], createdAtUtc: DateTimeOffset.UtcNow);

        run.AttachPlayerSnapshot(snapshot);
        return run;
    }

    [Fact]
    public async Task Handle_ShouldReplaceProtagonistAndCompanionStats_AndPersistTheRun()
    {
        var run = CreateRunWithProtagonistAndCompanion(out var protagonistId, out var companionId);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(run.PlayerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateFreshSnapshot(run.PlayerId, protagonistId, companionId));

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var skillMerger = new PlayerSkillMerger(catalogGateway.Object);
        var statMerger = new PlayerStatMerger();

        var handler = new SyncPartyStatsCommandHandler(repo.Object, playerGateway.Object, skillMerger, statMerger);

        await handler.Handle(new SyncPartyStatsCommand(run.Id.Value), CancellationToken.None);

        run.Attack.Should().Be(20);
        run.Defense.Should().Be(9);
        run.Speed.Should().Be(14);
        run.Focus.Should().Be(3);
        run.MaxHp.Should().Be(150);
        run.PlayerState.MaxVitality.Should().Be(150);
        run.PlayerState.MaxMana.Should().Be(25);
        run.PlayerState.Charge.Should().Be(4);

        run.PlayerSnapshot!.Characters.First(c => c.CharacterId == protagonistId).StatBlock.AttackPower
            .Should().Be(20);
        run.PlayerSnapshot!.Characters.First(c => c.CharacterId == companionId).StatBlock.AttackPower
            .Should().Be(16);
        run.PlayerSnapshot!.Characters.First(c => c.CharacterId == companionId).StatBlock.StartingGuard
            .Should().Be(2);

        repo.Verify(r => r.UpdateAsync(run, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RunId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Run?)null);

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        var skillMerger = new PlayerSkillMerger(new Mock<ICatalogContentGateway>().Object);
        var statMerger = new PlayerStatMerger();
        var handler = new SyncPartyStatsCommandHandler(repo.Object, playerGateway.Object, skillMerger, statMerger);

        var act = () => handler.Handle(new SyncPartyStatsCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenCombatIsActive()
    {
        var run = CreateRunWithProtagonistAndCompanion(out _, out _);
        var combat = CombatInstance.Create(new[]
        {
            CombatantSnapshot.Create("p", "Player", CombatantSide.Player, 40, 12, 6, 10),
            CombatantSnapshot.Create("e", "Enemy", CombatantSide.Enemy, 30, 8, 4, 6),
        });
        run.ChooseNode(run.CurrentRoom.AvailableNodes.First().Id);
        run.SetActiveCombat(combat.Id);

        var repo = new Mock<IRunRepository>();
        repo.Setup(r => r.GetByIdAsync(run.Id, It.IsAny<CancellationToken>())).ReturnsAsync(run);

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        var skillMerger = new PlayerSkillMerger(new Mock<ICatalogContentGateway>().Object);
        var statMerger = new PlayerStatMerger();
        var handler = new SyncPartyStatsCommandHandler(repo.Object, playerGateway.Object, skillMerger, statMerger);

        var act = () => handler.Handle(new SyncPartyStatsCommand(run.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot sync party stats while a combat is active.");
    }
}
