using FluentAssertions;
using Leds.Catalog.Domain.Npcs;

namespace Leds.Catalog.UnitTests.Domain.Npcs;

public sealed class NpcPersonaTests
{
    [Fact]
    public void Create_ShouldCreatePersona_WithAllProperties()
    {
        var persona = new NpcPersona(
            Tone: "Melancholic and reserved",
            Register: EmotionalRegister.Melancolie,
            Needs: new[] { "silence", "understanding" },
            Offerings: new[] { "tea", "warmth" });

        persona.Tone.Should().Be("Melancholic and reserved");
        persona.Register.Should().Be(EmotionalRegister.Melancolie);
        persona.Needs.Should().HaveCount(2);
        persona.Needs.Should().Contain("silence");
        persona.Offerings.Should().HaveCount(2);
        persona.Offerings.Should().Contain("tea");
    }

    [Fact]
    public void Create_ShouldCreatePersona_WithEmptyLists()
    {
        var persona = new NpcPersona(
            Tone: "Neutral",
            Register: EmotionalRegister.Neutral,
            Needs: Array.Empty<string>(),
            Offerings: Array.Empty<string>());

        persona.Needs.Should().BeEmpty();
        persona.Offerings.Should().BeEmpty();
    }

    [Fact]
    public void RecordEquality_ShouldBeEqual_WhenAllPropertiesMatch()
    {
        var p1 = new NpcPersona("Tone", EmotionalRegister.Neutral, Array.Empty<string>(), Array.Empty<string>());
        var p2 = new NpcPersona("Tone", EmotionalRegister.Neutral, Array.Empty<string>(), Array.Empty<string>());

        p1.Should().Be(p2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenToneDiffers()
    {
        var p1 = new NpcPersona("Tone1", EmotionalRegister.Neutral, Array.Empty<string>(), Array.Empty<string>());
        var p2 = new NpcPersona("Tone2", EmotionalRegister.Neutral, Array.Empty<string>(), Array.Empty<string>());

        p1.Should().NotBe(p2);
    }

    [Fact]
    public void RecordEquality_ShouldNotBeEqual_WhenRegisterDiffers()
    {
        var p1 = new NpcPersona("Tone", EmotionalRegister.Neutral, Array.Empty<string>(), Array.Empty<string>());
        var p2 = new NpcPersona("Tone", EmotionalRegister.Rupture, Array.Empty<string>(), Array.Empty<string>());

        p1.Should().NotBe(p2);
    }
}
