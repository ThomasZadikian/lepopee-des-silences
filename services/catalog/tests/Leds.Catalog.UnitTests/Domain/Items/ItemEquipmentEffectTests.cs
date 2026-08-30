using FluentAssertions;
using Leds.Catalog.Domain.Items;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Items;

public sealed class ItemEquipmentEffectTests
{
    [Fact]
    public void Create_ShouldSupportStatBonus()
    {
        var effect = new ItemEquipmentEffect(ItemEquipmentEffectKind.StatBonus, StatKind: "AttackPower", Amount: 10);

        effect.Kind.Should().Be(ItemEquipmentEffectKind.StatBonus);
        effect.StatKind.Should().Be("AttackPower");
        effect.Amount.Should().Be(10);
        effect.SkillKey.Should().BeNull();
        effect.AffinityRegister.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSupportGrantSkill()
    {
        var effect = new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantSkill, SkillKey: "skill.basic.strike");

        effect.SkillKey.Should().Be("skill.basic.strike");
        effect.StatKind.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSupportGrantAffinity()
    {
        var effect = new ItemEquipmentEffect(ItemEquipmentEffectKind.GrantAffinity, AffinityRegister: EmotionalRegister.Silence);

        effect.AffinityRegister.Should().Be(EmotionalRegister.Silence);
    }
}
