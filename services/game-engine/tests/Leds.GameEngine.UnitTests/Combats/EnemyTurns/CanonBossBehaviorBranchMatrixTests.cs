using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;
using Leds.GameEngine.Application.Combats.EnemyTurns.Bossing.Canon;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.StatusEffects;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns;

public sealed class CanonBossBehaviorBranchMatrixTests
{
    [Fact]
    public void EveryCanonBossBehavior_ShouldRemainTotalAcrossRepresentativeCombatStates()
    {
        var behaviorTypes = typeof(CanonBossBehaviorBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract
                && typeof(CanonBossBehaviorBase).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToArray();

        behaviorTypes.Should().NotBeEmpty();

        foreach (var behaviorType in behaviorTypes)
        {
            var behavior = (CanonBossBehaviorBase)Activator.CreateInstance(behaviorType)!;

            foreach (var turn in Enumerable.Range(1, 6))
            {
                foreach (var bossVitality in new[] { 100, 49, 29 })
                {
                    var first = Ally("first", currentVitality: 100, speed: 8, defense: 12, magicDefense: 4);
                    var second = Ally("second", currentVitality: 42, speed: 18, defense: 3, magicDefense: 15);
                    var boss = Enemy(
                        behavior.BossKey,
                        currentVitality: bossVitality,
                        mana: turn % 2 == 0 ? 2 : 20,
                        CommonSkills());
                    var teammate = Enemy("enemy.teammate", currentVitality: 28, mana: 1, []);

                    if (turn % 2 == 0)
                    {
                        first.ApplyStatusEffect(CombatStatusEffect.Create(
                            "test.dot",
                            "DoT",
                            StatusEffectKind.DamageOverTime,
                            currentTick: 0,
                            durationTicks: 20,
                            magnitude: 2));
                        second.ApplyStatusEffect(CombatStatusEffect.Create(
                            "test.slow",
                            "Slow",
                            StatusEffectKind.StatModifier,
                            currentTick: 0,
                            durationTicks: 20,
                            magnitude: -10,
                            stat: CombatStat.Speed));
                        boss.ApplyStatusEffect(CombatStatusEffect.Create(
                            "test.buff",
                            "Buff",
                            StatusEffectKind.StatModifier,
                            currentTick: 0,
                            durationTicks: 20,
                            magnitude: 10,
                            stat: CombatStat.Defense));
                    }

                    var context = new FakeCombatContext(
                        turn,
                        [first, second],
                        [boss, teammate]);

                    var act = () => behavior.DecideAction(new BossDecisionContext(context, boss));
                    act.Should().NotThrow(
                        because: $"{behaviorType.Name} must gracefully degrade for partial kits at turn {turn} and {bossVitality}% HP");
                }
            }
        }
    }

    [Fact]
    public void EveryCanonBossBehavior_ShouldHandleNoLivingPlayerWithoutThrowing()
    {
        foreach (var behaviorType in typeof(CanonBossBehaviorBase).Assembly.GetTypes()
                     .Where(type => !type.IsAbstract
                         && typeof(CanonBossBehaviorBase).IsAssignableFrom(type)))
        {
            var behavior = (CanonBossBehaviorBase)Activator.CreateInstance(behaviorType)!;
            var dead = Ally("dead", currentVitality: 0);
            var boss = Enemy(behavior.BossKey, currentVitality: 100, mana: 0, []);
            var context = new FakeCombatContext(3, [dead], [boss]);

            var act = () => behavior.DecideAction(new BossDecisionContext(context, boss));
            act.Should().NotThrow();
        }
    }

    private static IReadOnlyCollection<CombatantSkill> CommonSkills() =>
    [
        Skill("canon.skill.flamme-froide", "Damage", "SingleEnemy", 10),
        Skill("canon.skill.priere-aspiration", "Drain", "SingleEnemy", 8),
        Skill("canon.skill.brume", "Status", "AllEnemies", 0),
        Skill("canon.skill.transmutation", "Buff", "Self", 0),
        Skill("canon.skill.flamme-seraphine", "Damage", "SingleEnemy", 20)
    ];

    private static CombatantSkill Skill(string key, string effectType, string targetingType, int power) =>
        CombatantSkill.Create(
            key,
            key,
            effectType,
            targetingType,
            effectType,
            0,
            0,
            power,
            emotionalRegister: "Neutral");

    private static Combatant Enemy(
        string sourceKey,
        int currentVitality,
        int mana,
        IReadOnlyCollection<CombatantSkill> skills) =>
        Combatant.Create(
            CombatantId.New(),
            sourceKey,
            sourceKey,
            CombatantSide.Enemy,
            "Boss",
            maxVitality: 100,
            currentVitality: currentVitality,
            guard: 0,
            baseGuard: 0,
            mana: mana,
            charge: 5,
            skills: skills,
            defense: 8,
            speed: 10,
            maxMana: 20,
            magicDefense: 8);

    private static Combatant Ally(
        string key,
        int currentVitality,
        int speed = 10,
        int defense = 10,
        int magicDefense = 10) =>
        Combatant.Create(
            CombatantId.New(),
            $"ally.{key}",
            key,
            CombatantSide.Player,
            "Hero",
            maxVitality: 100,
            currentVitality: currentVitality,
            guard: 0,
            baseGuard: 0,
            mana: 10,
            charge: 0,
            skills: [],
            defense: defense,
            speed: speed,
            maxMana: 10,
            magicDefense: magicDefense);

    private sealed class FakeCombatContext(
        int turnNumber,
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies) : ICombatContext
    {
        public CombatId Id { get; } = CombatId.New();
        public CombatStatus Status => CombatStatus.Active;
        public int CurrentTick => turnNumber * 100;
        public int TurnNumber { get; } = turnNumber;
        public IReadOnlyCollection<Combatant> Allies { get; } = allies;
        public IReadOnlyCollection<Combatant> Enemies { get; } = enemies;
        public EmotionalAffinityMatrixSnapshot EmotionalAffinityMatrix => null!;
        public bool LowHpDamageAmplificationEnabled => false;
        public bool HealingBlocked => false;
        public bool DuelDamageAsymmetryEnabled => false;
        public int DotMagnitudeBonus => 0;
        public int DotDurationExtensionTicks => 0;
        public bool RegisterLandedHit() => false;
        public bool TryConsumeFirstHitCritical() => false;
        public void RegisterCombatantDefeated() { }
        public (int HealAmount, bool Triggered) ApplyThirdCupRollIfActive(Combatant target, int healAmount) =>
            (healAmount, false);
    }
}
