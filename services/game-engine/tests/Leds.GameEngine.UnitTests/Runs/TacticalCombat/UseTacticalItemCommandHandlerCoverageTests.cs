using System.Reflection;
using FluentAssertions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Runs.TacticalCombat;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Runs.TacticalCombat;

public sealed class UseTacticalItemCommandHandlerCoverageTests
{
    private static readonly MethodInfo ParseShapeMethod =
        typeof(UseTacticalItemCommandHandler).GetMethod(
            "ParseShape",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ParseShape was not found.");

    private static readonly MethodInfo ApplyEffectMethod =
        typeof(UseTacticalItemCommandHandler).GetMethod(
            "ApplyEffect",
            BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("ApplyEffect was not found.");

    [Theory]
    [InlineData("Single", TacticalAreaShape.Single)]
    [InlineData("single", TacticalAreaShape.Single)]
    [InlineData("Diamond", TacticalAreaShape.Diamond)]
    [InlineData("Cross", TacticalAreaShape.Cross)]
    [InlineData("Map", TacticalAreaShape.Map)]
    [InlineData("not-a-shape", TacticalAreaShape.Diamond)]
    [InlineData("", TacticalAreaShape.Diamond)]
    public void ParseShape_ShouldResolveKnownValuesAndFallback(
        string raw,
        TacticalAreaShape expected)
    {
        InvokeParseShape(raw).Should().Be(expected);
    }

    [Fact]
    public void ApplyEffect_Heal_ShouldHealDamagedLivingTarget()
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        target.ApplyVitalityDamage(40);
        var item = CreateItem(RunItemEffectType.Heal, 15);

        InvokeApplyEffect(actor, item, [target]);

        target.CurrentVitality.Should().Be(75);
    }

    [Fact]
    public void ApplyEffect_Heal_ShouldLeaveFullVitalityUnchanged()
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        var item = CreateItem(RunItemEffectType.Heal, 15);

        InvokeApplyEffect(actor, item, [target]);

        target.CurrentVitality.Should().Be(target.MaxVitality);
    }

    [Theory]
    [InlineData(RunItemEffectType.HealPercent)]
    [InlineData(RunItemEffectType.ConditionalHealOrPoison)]
    public void ApplyEffect_PercentHealingEffects_ShouldHealByPercentage(RunItemEffectType effectType)
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        target.ApplyVitalityDamage(50);
        var item = CreateItem(effectType, 20);

        InvokeApplyEffect(actor, item, [target]);

        target.CurrentVitality.Should().Be(70);
    }

    [Fact]
    public void ApplyEffect_ManaRestore_ShouldIncreaseMana()
    {
        var actor = CreateAlly();
        var target = CreateAlly(mana: 5, maxMana: 100);
        var item = CreateItem(RunItemEffectType.ManaRestore, 12);

        InvokeApplyEffect(actor, item, [target]);

        target.Mana.Should().Be(17);
    }

    [Fact]
    public void ApplyEffect_ChargeRestore_ShouldIncreaseCharge()
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        var item = CreateItem(RunItemEffectType.ChargeRestore, 2);

        InvokeApplyEffect(actor, item, [target]);

        target.Charge.Should().Be(2);
    }

    [Fact]
    public void ApplyEffect_Guard_ShouldIncreaseGuard()
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        var item = CreateItem(RunItemEffectType.Guard, 18);

        InvokeApplyEffect(actor, item, [target]);

        target.Guard.Should().Be(18);
    }

    [Fact]
    public void ApplyEffect_HealAndManaRestorePercent_ShouldAffectBothResources()
    {
        var actor = CreateAlly();
        var target = CreateAlly(mana: 10, maxMana: 100);
        target.ApplyVitalityDamage(60);
        var item = CreateItem(RunItemEffectType.HealAndManaRestorePercent, 25);

        InvokeApplyEffect(actor, item, [target]);

        target.CurrentVitality.Should().Be(65);
        target.Mana.Should().Be(35);
    }

    [Fact]
    public void ApplyEffect_ShouldIgnoreDefeatedTargets()
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        target.MarkDefeated();
        var item = CreateItem(RunItemEffectType.Heal, 50);

        InvokeApplyEffect(actor, item, [target]);

        target.IsDefeated.Should().BeTrue();
        target.CurrentVitality.Should().Be(0);
    }

    [Fact]
    public void ApplyEffect_ShouldRejectUnsupportedCombatEffect()
    {
        var actor = CreateAlly();
        var target = CreateAlly();
        var item = CreateItem(RunItemEffectType.None, 10);

        var action = () => InvokeApplyEffect(actor, item, [target]);

        action.Should().Throw<ConflictException>()
            .WithMessage("*n'est pas une action consommable résolue par le combat tactique*");
    }

    private static TacticalAreaShape InvokeParseShape(string raw) =>
        (TacticalAreaShape)ParseShapeMethod.Invoke(null, [raw])!;

    private static void InvokeApplyEffect(
        Combatant actor,
        RunItem item,
        IReadOnlyCollection<Combatant> targets)
    {
        try
        {
            // The covered effect families do not read the combat clock. Passing null lets
            // these pure switch branches be tested without manufacturing a tactical aggregate.
            ApplyEffectMethod.Invoke(null, [null, actor, item, targets]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static Combatant CreateAlly(int mana = 0, int? maxMana = null) =>
        Combatant.Create(
            CombatantId.New(),
            "player.self",
            "Hero",
            CombatantSide.Player,
            "Fighter",
            maxVitality: 100,
            currentVitality: 100,
            guard: 0,
            baseGuard: 0,
            mana: mana,
            charge: 0,
            maxMana: maxMana);

    private static RunItem CreateItem(RunItemEffectType effectType, int amount) =>
        RunItem.Create(
            definitionKey: $"item.test.{effectType}",
            displayName: "Objet de test",
            description: "Test",
            type: RunItemType.Consumable,
            rarity: default,
            quantity: 1,
            effectType: effectType,
            effectAmount: amount);
}
