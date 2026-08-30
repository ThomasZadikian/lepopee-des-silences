namespace Leds.Catalog.Domain.Npcs;

// The authored surface: tone, what the PNJ is attentive to, and what it OFFERS
// (tea, water, attention…). Offerings are the symbols the consequences reuse.
public sealed record NpcPersona(
    string Tone,
    EmotionalRegister Register,
    IReadOnlyList<string> Needs,
    IReadOnlyList<string> Offerings);