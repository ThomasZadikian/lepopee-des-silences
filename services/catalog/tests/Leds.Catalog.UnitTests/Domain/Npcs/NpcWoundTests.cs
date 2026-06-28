using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcWoundTests
{
    [Fact]
    public void Create_ShouldCreateWound_WithAllProperties()
    {
        var transgressions = new[]
        {
            new NpcTransgression("wound-key", "trigger-flag", -5)
        };

        var wound = new NpcWound(
            Key: "wound-betrayal",
            WoundRegister: EmotionalRegister.Rupture,
            Reversibility: NpcWoundReversibility.Irreversible,
            TenseThreshold: 10,
            RuptureThreshold: 20,
            Transgressions: transgressions,
            RupturedNarrativeKey: "narrative-rupture-betrayal");

        wound.Key.Should().Be("wound-betrayal");
        wound.WoundRegister.Should().Be(EmotionalRegister.Rupture);
        wound.Reversibility.Should().Be(NpcWoundReversibility.Irreversible);
        wound.TenseThreshold.Should().Be(10);
        wound.RuptureThreshold.Should().Be(20);
        wound.Transgressions.Should().HaveCount(1);
        wound.RupturedNarrativeKey.Should().Be("narrative-rupture-betrayal");
    }

    [Fact]
    public void Create_ShouldCreateWound_WithNullRupturedNarrativeKey()
    {
        var wound = new NpcWound(
            Key: "wound-minimal",
            WoundRegister: EmotionalRegister.Silence,
            Reversibility: NpcWoundReversibility.SoothableByScore,
            TenseThreshold: 5,
            RuptureThreshold: 15,
            Transgressions: Array.Empty<NpcTransgression>());

        wound.RupturedNarrativeKey.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldCreateWound_WithEmptyTransgressions()
    {
        var wound = new NpcWound(
            Key: "wound-no-transgressions",
            WoundRegister: EmotionalRegister.Melancolie,
            Reversibility: NpcWoundReversibility.SoothableByAct,
            TenseThreshold: 8,
            RuptureThreshold: 16,
            Transgressions: Array.Empty<NpcTransgression>());

        wound.Transgressions.Should().BeEmpty();
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenAllPropertiesMatch()
    {
        var wound1 = new NpcWound(
            "wound-key",
            EmotionalRegister.Rupture,
            NpcWoundReversibility.Irreversible,
            10,
            20,
            Array.Empty<NpcTransgression>());

        var wound2 = new NpcWound(
            "wound-key",
            EmotionalRegister.Rupture,
            NpcWoundReversibility.Irreversible,
            10,
            20,
            Array.Empty<NpcTransgression>());

        wound1.Should().Be(wound2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenKeyDiffers()
    {
        var wound1 = new NpcWound(
            "wound-key-1",
            EmotionalRegister.Rupture,
            NpcWoundReversibility.Irreversible,
            10,
            20,
            Array.Empty<NpcTransgression>());

        var wound2 = new NpcWound(
            "wound-key-2",
            EmotionalRegister.Rupture,
            NpcWoundReversibility.Irreversible,
            10,
            20,
            Array.Empty<NpcTransgression>());

        wound1.Should().NotBe(wound2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenThresholdsDiffer()
    {
        var wound1 = new NpcWound(
            "wound-key",
            EmotionalRegister.Rupture,
            NpcWoundReversibility.Irreversible,
            10,
            20,
            Array.Empty<NpcTransgression>());

        var wound2 = new NpcWound(
            "wound-key",
            EmotionalRegister.Rupture,
            NpcWoundReversibility.Irreversible,
            15,
            20,
            Array.Empty<NpcTransgression>());

        wound1.Should().NotBe(wound2);
    }
}
