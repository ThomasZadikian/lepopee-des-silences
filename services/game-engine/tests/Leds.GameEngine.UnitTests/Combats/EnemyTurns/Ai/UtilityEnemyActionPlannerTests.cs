using FluentAssertions;
using Leds.GameEngine.Application.Combats.EnemyTurns.Ai;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.UnitTests.Combats.EnemyTurns.Ai;

public sealed class UtilityEnemyActionPlannerTests
{
    private readonly UtilityEnemyActionPlanner _planner = new();

    [Fact]
    public void Plan_ShouldReturnNull_WhenActorHasNoSkills()
    {
        var actor = Enemy("basic", []);
        var combat = Context([Ally("hero")], [actor]);

        _planner.Plan(combat, actor).Should().BeNull();
    }

    [Fact]
    public void Plan_ShouldIgnoreManaAndChargeSkillsThatCannotBeAfforded()
    {
        var mana = Skill("mana", "Damage", "SingleEnemy", power: 10, manaCost: 6);
        var charge = Skill("charge", "Damage", "SingleEnemy", power: 10, chargeCost: 2);
        var actor = Enemy("basic", [mana, charge], mana: 5, charge: 1);
        var combat = Context([Ally("hero")], [actor]);

        _planner.Plan(combat, actor).Should().BeNull();
    }

    [Fact]
    public void Plan_ShouldTargetSelf_ForSelfSkill()
    {
        var skill = Skill("self-buff", "Buff", "Self", power: 1);
        var actor = Enemy("memory", [skill]);

        var plan = _planner.Plan(Context([Ally("hero")], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.Skill.Should().BeSameAs(skill);
        plan.TargetIds.Should().Equal(actor.Id.Value);
    }

    [Fact]
    public void Plan_ShouldTargetEveryLivingPlayer_ForAllEnemiesSkill()
    {
        var skill = Skill("aoe", "Damage", "AllEnemies", power: 15);
        var actor = Enemy("rupture", [skill]);
        var first = Ally("first");
        var second = Ally("second", currentVitality: 40);

        var plan = _planner.Plan(Context([first, second], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().BeEquivalentTo([first.Id.Value, second.Id.Value]);
        plan.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Plan_ShouldTargetEveryLivingTeamMember_ForAllAlliesSkill()
    {
        var skill = Skill("team-guard", "Guard", "AllAllies", power: 5);
        var actor = Enemy("fragile", [skill], currentVitality: 45);
        var teammate = Enemy("basic", [], currentVitality: 30);

        var plan = _planner.Plan(Context([Ally("hero")], [actor, teammate]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().BeEquivalentTo([actor.Id.Value, teammate.Id.Value]);
    }

    [Fact]
    public void Plan_ShouldPickMostWoundedAlly_ForSingleAllyHeal()
    {
        var skill = Skill("heal", "Heal", "SingleAlly", power: 20);
        var actor = Enemy("trauma", [skill], currentVitality: 80);
        var wounded = Enemy("basic", [], currentVitality: 20);
        var healthy = Enemy("basic", [], currentVitality: 90);

        var plan = _planner.Plan(Context([Ally("hero")], [actor, healthy, wounded]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().Equal(wounded.Id.Value);
        plan.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Plan_ShouldFallbackToActor_ForSingleAlly_WhenTeamCollectionIsEmpty()
    {
        var skill = Skill("heal", "Heal", "SingleAlly", power: 20);
        var actor = Enemy("basic", [skill], currentVitality: 50);

        var plan = _planner.Plan(Context([Ally("hero")], []), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().Equal(actor.Id.Value);
    }

    [Fact]
    public void Plan_ShouldReturnNull_ForSingleEnemy_WhenNoLivingPlayerExists()
    {
        var actor = Enemy("basic", [Skill("strike", "Damage", "SingleEnemy", 10)]);

        _planner.Plan(Context([], [actor]), actor).Should().BeNull();
    }

    [Fact]
    public void Plan_ShouldTreatUnknownTargetingAsSingleEnemy()
    {
        var skill = Skill("odd", "Damage", "Unexpected", power: 10);
        var actor = Enemy("shadow", [skill]);
        var target = Ally("hero");

        var plan = _planner.Plan(Context([target], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().Equal(target.Id.Value);
    }

    [Fact]
    public void Plan_ShouldPreferLethalSingleTarget()
    {
        var skill = Skill("execute", "Damage", "SingleEnemy", power: 30);
        var actor = Enemy("elite", [skill]);
        var healthy = Ally("healthy", currentVitality: 100);
        var lethal = Ally("lethal", currentVitality: 20);

        var plan = _planner.Plan(Context([healthy, lethal], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().Equal(lethal.Id.Value);
    }

    [Fact]
    public void Plan_ShouldIncludeThreatAndLastAttacker_WhenChoosingSingleTarget()
    {
        var skill = Skill("strike", "Damage", "SingleEnemy", power: 5);
        var actor = Enemy("boss", [skill]);
        var quiet = Ally("quiet");
        var threatening = Ally("threatening");
        threatening.AccrueThreat(100);
        actor.RecordLastAttacker(threatening.Id.Value);

        var plan = _planner.Plan(Context([quiet, threatening], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().Equal(threatening.Id.Value);
    }

    [Fact]
    public void Plan_ShouldChooseHighestScoringSkill()
    {
        var weak = Skill("weak", "Damage", "SingleEnemy", power: 1);
        var strong = Skill("strong", "Damage", "SingleEnemy", power: 100);
        var actor = Enemy("rupture", [weak, strong]);

        var plan = _planner.Plan(Context([Ally("hero")], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.Skill.Should().BeSameAs(strong);
    }

    [Theory]
    [InlineData("Heal", "Other")]
    [InlineData("Other", "Heal")]
    [InlineData("Guard", "Other")]
    [InlineData("Other", "Defense")]
    [InlineData("Buff", "Other")]
    [InlineData("Debuff", "Other")]
    [InlineData("Status", "Other")]
    [InlineData("Damage", "Attack")]
    public void Plan_ShouldScoreEverySkillCategory(string effectType, string skillType)
    {
        var targetType = effectType == "Heal" || skillType == "Heal" ? "SingleAlly" : "SingleEnemy";
        var skill = Skill("category", effectType, targetType, power: 10, skillType: skillType);
        var actor = Enemy("basic", [skill], currentVitality: 50);
        var teammate = Enemy("basic", [], currentVitality: 30);

        var plan = _planner.Plan(Context([Ally("hero", currentVitality: 70)], [actor, teammate]), actor);

        plan.Should().NotBeNull();
        plan!.Score.Should().NotBe(double.NegativeInfinity);
    }

    [Theory]
    [InlineData("rupture")]
    [InlineData("shadow")]
    [InlineData("memory")]
    [InlineData("fragile")]
    [InlineData("trauma")]
    [InlineData("boss")]
    [InlineData("elite")]
    [InlineData("unknown")]
    [InlineData("  RUPTURE  ")]
    public void Plan_ShouldSupportEveryArchetypeWeightProfile(string archetype)
    {
        var actor = Enemy(archetype, [Skill("strike", "Damage", "SingleEnemy", 10)]);

        var plan = _planner.Plan(Context([Ally("hero")], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Plan_ShouldScoreAoeControlAndOffenseWithMultipleTargets()
    {
        var control = Skill("control", "Status", "AllEnemies", power: 1);
        var aoe = Skill("aoe", "Damage", "AllEnemies", power: 20);
        var actor = Enemy("memory", [control, aoe]);
        var players = new[] { Ally("one"), Ally("two", 70), Ally("three", 40) };

        var plan = _planner.Plan(Context(players, [actor]), actor);

        plan.Should().NotBeNull();
        plan!.TargetIds.Should().HaveCount(3);
    }

    [Fact]
    public void Plan_ShouldScoreDefenseUsingActorDangerAndPlayerCount()
    {
        var defense = Skill("guard", "Guard", "Self", power: 1);
        var actor = Enemy("fragile", [defense], currentVitality: 10);

        var plan = _planner.Plan(Context([Ally("one"), Ally("two")], [actor]), actor);

        plan.Should().NotBeNull();
        plan!.Score.Should().BeGreaterThan(0);
    }

    private static CombatantSkill Skill(
        string key,
        string effectType,
        string targetingType,
        int power,
        int manaCost = 0,
        int chargeCost = 0,
        string skillType = "Attack") =>
        CombatantSkill.Create(
            key,
            key,
            skillType,
            targetingType,
            effectType,
            manaCost,
            chargeCost,
            power,
            emotionalRegister: "Silence");

    private static Combatant Enemy(
        string archetype,
        IReadOnlyCollection<CombatantSkill> skills,
        int currentVitality = 100,
        int mana = 10,
        decimal charge = 5) =>
        Combatant.Create(
            CombatantId.New(),
            $"enemy.{Guid.NewGuid():N}",
            "Enemy",
            CombatantSide.Enemy,
            archetype,
            maxVitality: 100,
            currentVitality: currentVitality,
            guard: 0,
            baseGuard: 0,
            mana: mana,
            charge: charge,
            skills: skills,
            maxMana: Math.Max(mana, 10));

    private static Combatant Ally(string key, int currentVitality = 100) =>
        Combatant.Create(
            CombatantId.New(),
            $"ally.{key}.{Guid.NewGuid():N}",
            key,
            CombatantSide.Player,
            "hero",
            maxVitality: 100,
            currentVitality: currentVitality,
            guard: 0,
            baseGuard: 0,
            mana: 10,
            charge: 0,
            skills: [],
            maxMana: 10);

    private static FakeCombatContext Context(
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies) => new(allies, enemies);

    private sealed class FakeCombatContext(
        IReadOnlyCollection<Combatant> allies,
        IReadOnlyCollection<Combatant> enemies) : ICombatContext
    {
        public CombatId Id { get; } = CombatId.New();
        public CombatStatus Status => CombatStatus.Active;
        public int CurrentTick => 17;
        public int TurnNumber => 3;
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
