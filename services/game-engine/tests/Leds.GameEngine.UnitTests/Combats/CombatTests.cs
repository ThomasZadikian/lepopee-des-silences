using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Combats;

public sealed class CombatTests
{
    private static Combat CreateSut(
        int allyCount = 1, int enemyCount = 1,
        bool hitCounterDoubleDamageEnabled = false,
        bool firstHitCriticalEnabled = false,
        bool lowHpDamageAmplificationEnabled = false,
        int dotDurationExtensionTicks = 0,
        bool duelDamageAsymmetryEnabled = false,
        int dotMagnitudeBonus = 0,
        bool healingBlocked = false)
    {
        var allies = Enumerable.Range(0, allyCount).Select(i =>
            Combatant.CreateAlly($"player.{i}", $"Hero{i}", "Fighter", 100)).ToArray();

        var enemies = Enumerable.Range(0, enemyCount).Select(i =>
            Combatant.CreateEnemy($"enemy.{i}", $"Enemy{i}", "Guard", 80)).ToArray();

        return Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies,
            hitCounterDoubleDamageEnabled,
            firstHitCriticalEnabled,
            lowHpDamageAmplificationEnabled,
            dotDurationExtensionTicks,
            duelDamageAsymmetryEnabled,
            dotMagnitudeBonus,
            healingBlocked);
    }

    [Fact]
    public void Create_ShouldSucceed_WithOneAllyAndOneEnemy()
    {
        var combat = CreateSut();

        combat.Allies.Should().HaveCount(1);
        combat.Enemies.Should().HaveCount(1);
    }

    [Fact]
    public void Create_ShouldSucceed_WithMultipleAlliesAndEnemies()
    {
        var combat = CreateSut(allyCount: 3, enemyCount: 2);

        combat.Allies.Should().HaveCount(3);
        combat.Enemies.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoAlly()
    {
        var enemies = new[] { Combatant.CreateEnemy("enemy.0", "Enemy", "Guard", 80) };

        var act = () => Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            Array.Empty<Combatant>(),
            enemies);

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one ally.");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoEnemy()
    {
        var allies = new[] { Combatant.CreateAlly("player.self", "Hero", "Fighter", 100) };

        var act = () => Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            Array.Empty<Combatant>());

        act.Should().Throw<DomainException>().WithMessage("Combat requires at least one enemy.");
    }

    [Fact]
    public void Create_ShouldStartAsActive()
    {
        var combat = CreateSut();

        combat.Status.Should().Be(CombatStatus.Active);
    }

    [Fact]
    public void Create_ShouldStartAtTurnOne()
    {
        var combat = CreateSut();

        combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public void Create_ShouldSetFirstAllyAsActiveCombatant()
    {
        var allies = new[]
        {
            Combatant.CreateAlly("player.1", "Alice", "Fighter", 100),
            Combatant.CreateAlly("player.2", "Bob", "Mage", 80)
        };
        var enemies = new[] { Combatant.CreateEnemy("enemy.0", "Enemy", "Guard", 80) };

        var combat = Combat.Create(
            CombatId.New(),
            RunId.New(),
            RoomId.New(),
            NodeId.New(),
            allies,
            enemies);

        combat.ActiveCombatantId.Should().NotBeNull();
        combat.Allies.Select(a => a.Id).Should().Contain(combat.ActiveCombatantId.Value);
    }

    [Fact]
    public void Create_ShouldRejectCombatantWithInvalidVitality()
    {
        var allies = new[] { Combatant.CreateAlly("player.self", "Hero", "Fighter", 100) };

        var act = () =>
        {
            var enemies = new[]
            {
                Combatant.Create(
                    CombatantId.New(),
                    "enemy.0",
                    "Enemy",
                    CombatantSide.Enemy,
                    "Guard",
                    maxVitality: -10,
                    currentVitality: -10,
                    guard: 0,
                    baseGuard: 0,
                    mana: 0,
                    charge: 0)
            };

            Combat.Create(
                CombatId.New(),
                RunId.New(),
                RoomId.New(),
                NodeId.New(),
                allies,
                enemies);
        };

        act.Should().Throw<DomainException>().WithMessage("Combatant max vitality must be greater than zero.");
    }

    [Fact]
    public void MarkCompleted_ShouldSetStatusCompleted()
    {
        var combat = CreateSut();

        combat.MarkCompleted();

        combat.Status.Should().Be(CombatStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_ShouldThrow_WhenNotActive()
    {
        var combat = CreateSut();
        combat.MarkCompleted();

        var act = () => combat.MarkCompleted();

        act.Should().Throw<DomainException>().WithMessage("Only an active combat can be completed.");
    }

    [Fact]
    public void MarkFailed_ShouldSetStatusFailed()
    {
        var combat = CreateSut();

        combat.MarkFailed();

        combat.Status.Should().Be(CombatStatus.Failed);
    }

    [Fact]
    public void MarkFailed_ShouldThrow_WhenNotActive()
    {
        var combat = CreateSut();
        combat.MarkCompleted();

        var act = () => combat.MarkFailed();

        act.Should().Throw<DomainException>().WithMessage("Only an active combat can be failed.");
    }

    [Fact]
    public void EnsureActorCanAct_ShouldSucceed_WhenActorIsActiveCombatant()
    {
        var combat = CreateSut();
        var actor = combat.Allies.Single();

        var act = () => combat.EnsureActorCanAct(actor.Id.Value);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureActorCanAct_ShouldThrow_WhenActorIsNotActiveCombatant()
    {
        var combat = CreateSut();
        var enemy = combat.Enemies.Single();

        var act = () => combat.EnsureActorCanAct(enemy.Id.Value);

        act.Should().Throw<DomainException>().WithMessage("It is not this combatant's turn.");
    }

    [Fact]
    public void EnsureActorCanAct_ShouldThrow_WhenCombatIsNotActive()
    {
        var combat = CreateSut();
        var actor = combat.Allies.Single();
        combat.MarkCompleted();

        var act = () => combat.EnsureActorCanAct(actor.Id.Value);

        act.Should().Throw<DomainException>().WithMessage("Combat is not active.");
    }

    [Fact]
    public void AdvanceTurn_ShouldMoveToNextActiveCombatant()
    {
        var combat = CreateSut();
        var enemy = combat.Enemies.Single();

        combat.AdvanceTurn();

        combat.ActiveCombatantId.Should().Be(enemy.Id);
        combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public void AdvanceTurn_ShouldSkipDefeatedCombatants()
    {
        var allies = new[] { Combatant.CreateAlly("player.0", "Hero", "Fighter", 100) };
        var enemies = new[]
        {
            Combatant.CreateEnemy("enemy.0", "Enemy0", "Guard", 80),
            Combatant.CreateEnemy("enemy.1", "Enemy1", "Guard", 80)
        };
        var combat = Combat.Create(CombatId.New(), RunId.New(), RoomId.New(), NodeId.New(), allies, enemies);
        enemies[0].MarkDefeated();

        combat.AdvanceTurn();

        combat.ActiveCombatantId.Should().Be(enemies[1].Id);
    }

    [Fact]
    public void AdvanceTurn_ShouldIncrementTurnNumber_WhenReturningToFirstCombatant()
    {
        var combat = CreateSut();

        combat.AdvanceTurn();
        combat.AdvanceTurn();

        combat.ActiveCombatantId.Should().Be(combat.Allies.Single().Id);
        combat.TurnNumber.Should().Be(2);
    }

    [Fact]
    public void AdvanceTurn_ShouldCompleteCombat_WhenAllEnemiesAreDefeated()
    {
        var combat = CreateSut();
        combat.Enemies.Single().MarkDefeated();

        combat.AdvanceTurn();

        combat.Status.Should().Be(CombatStatus.Completed);
        combat.ActiveCombatantId.Should().BeNull();
    }

    [Fact]
    public void AdvanceTurn_ShouldFailCombat_WhenAllAlliesAreDefeated()
    {
        var combat = CreateSut();
        combat.Allies.Single().MarkDefeated();

        combat.AdvanceTurn();

        combat.Status.Should().Be(CombatStatus.Failed);
        combat.ActiveCombatantId.Should().BeNull();
    }

    [Fact]
    public void AdvanceTurn_ShouldNotAdvance_WhenCombatIsCompleted()
    {
        var combat = CreateSut();
        combat.MarkCompleted();

        combat.AdvanceTurn();

        combat.Status.Should().Be(CombatStatus.Completed);
        combat.ActiveCombatantId.Should().BeNull();
        combat.TurnNumber.Should().Be(1);
    }

    [Fact]
    public void AdvanceTurn_ShouldBeDeterministic()
    {
        var combat = CreateSut(allyCount: 2, enemyCount: 2);
        var expectedOrder = combat.Allies.Concat(combat.Enemies)
            .OrderByDescending(c => c.BaseStatSnapshot.Speed)
            .ThenByDescending(c => c.BaseStatSnapshot.Initiative)
            .ThenBy(c => c.Side)
            .ThenBy(c => c.Id.Value)
            .Select(c => c.Id)
            .ToArray();

        combat.ActiveCombatantId.Should().Be(expectedOrder[0]);
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(expectedOrder[1]);
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(expectedOrder[2]);
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(expectedOrder[3]);
        combat.AdvanceTurn();
        combat.ActiveCombatantId.Should().Be(expectedOrder[0]);
        combat.TurnNumber.Should().Be(2);
    }

    // ---------------------------------------------------------------------------
    // "Loi du Treizième Coup" (RegisterLandedHit)
    // ---------------------------------------------------------------------------

    [Fact]
    public void RegisterLandedHit_ShouldReturnFalse_ForTheFirstTwelveHits_WhenEnabled()
    {
        var combat = CreateSut(hitCounterDoubleDamageEnabled: true);

        for (var i = 0; i < Combat.HitCounterTrigger - 1; i++)
        {
            combat.RegisterLandedHit().Should().BeFalse();
        }

        combat.HitCounter.Should().Be(Combat.HitCounterTrigger - 1);
    }

    [Fact]
    public void RegisterLandedHit_ShouldReturnTrue_OnTheThirteenthHit_WhenEnabled()
    {
        var combat = CreateSut(hitCounterDoubleDamageEnabled: true);

        for (var i = 0; i < Combat.HitCounterTrigger - 1; i++)
        {
            combat.RegisterLandedHit();
        }

        combat.RegisterLandedHit().Should().BeTrue();
        combat.HitCounter.Should().Be(Combat.HitCounterTrigger);
    }

    [Fact]
    public void RegisterLandedHit_ShouldNeverReturnTrue_WhenNotEnabled()
    {
        var combat = CreateSut(hitCounterDoubleDamageEnabled: false);

        for (var i = 0; i < Combat.HitCounterTrigger * 2; i++)
        {
            combat.RegisterLandedHit().Should().BeFalse();
        }
    }

    [Fact]
    public void RegisterLandedHit_ShouldRepeatEveryThirteenHits_WhenEnabled()
    {
        var combat = CreateSut(hitCounterDoubleDamageEnabled: true);
        var results = new List<bool>();

        for (var i = 0; i < Combat.HitCounterTrigger * 2; i++)
        {
            results.Add(combat.RegisterLandedHit());
        }

        results[Combat.HitCounterTrigger - 1].Should().BeTrue();
        results[Combat.HitCounterTrigger * 2 - 1].Should().BeTrue();
        results.Count(r => r).Should().Be(2);
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Première Impression" (TryConsumeFirstHitCritical)
    // ---------------------------------------------------------------------------

    [Fact]
    public void TryConsumeFirstHitCritical_ShouldReturnTrue_OnceOnly_WhenEnabled()
    {
        var combat = CreateSut(firstHitCriticalEnabled: true);

        combat.TryConsumeFirstHitCritical().Should().BeTrue();
        combat.TryConsumeFirstHitCritical().Should().BeFalse();
        combat.HasFirstHitLanded.Should().BeTrue();
    }

    [Fact]
    public void TryConsumeFirstHitCritical_ShouldAlwaysReturnFalse_WhenNotEnabled()
    {
        var combat = CreateSut(firstHitCriticalEnabled: false);

        combat.TryConsumeFirstHitCritical().Should().BeFalse();
        combat.HasFirstHitLanded.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Curée" (LowHpDamageAmplificationEnabled) — the flag itself; the
    // actual +15%-below-25%-HP amplification is exercised in
    // CombatSkillEffectResolverTests (it needs live vitality, resolved there).
    // ---------------------------------------------------------------------------

    [Fact]
    public void LowHpDamageAmplificationEnabled_ShouldReflectTheValueBakedInAtCreation()
    {
        CreateSut(lowHpDamageAmplificationEnabled: true).LowHpDamageAmplificationEnabled.Should().BeTrue();
        CreateSut(lowHpDamageAmplificationEnabled: false).LowHpDamageAmplificationEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi de l'Écriture" (DotDurationExtensionTicks) and "Loi du Duel"
    // (DuelDamageAsymmetryEnabled) — flags baked in at creation; the actual
    // mechanics are exercised in CombatSkillEffectResolverTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void DotDurationExtensionTicks_ShouldReflectTheValueBakedInAtCreation()
    {
        CreateSut(dotDurationExtensionTicks: 5000).DotDurationExtensionTicks.Should().Be(5000);
        CreateSut().DotDurationExtensionTicks.Should().Be(0);
    }

    [Fact]
    public void DuelDamageAsymmetryEnabled_ShouldReflectTheValueBakedInAtCreation()
    {
        CreateSut(duelDamageAsymmetryEnabled: true).DuelDamageAsymmetryEnabled.Should().BeTrue();
        CreateSut(duelDamageAsymmetryEnabled: false).DuelDamageAsymmetryEnabled.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // "Loi de la Marée Haute" (DotMagnitudeBonus) — the flag itself; the actual
    // +1-per-tick DoT bonus is exercised in CombatSkillEffectResolverTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void DotMagnitudeBonus_ShouldReflectTheValueBakedInAtCreation()
    {
        CreateSut(dotMagnitudeBonus: 1).DotMagnitudeBonus.Should().Be(1);
        CreateSut().DotMagnitudeBonus.Should().Be(0);
    }

    // ---------------------------------------------------------------------------
    // "Loi des Visites Terminées" (HealingBlocked) — the flag itself; the actual
    // healing-skill no-op is exercised in CombatSkillEffectResolverTests.
    // ---------------------------------------------------------------------------

    [Fact]
    public void HealingBlocked_ShouldReflectTheValueBakedInAtCreation()
    {
        CreateSut(healingBlocked: true).HealingBlocked.Should().BeTrue();
        CreateSut().HealingBlocked.Should().BeFalse();
    }
}