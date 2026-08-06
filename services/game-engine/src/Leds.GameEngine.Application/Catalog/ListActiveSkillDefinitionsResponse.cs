namespace Leds.GameEngine.Application.Catalog;

public sealed record SkillDefinitionView(
    string Key,
    string DisplayName,
    string Description,
    string SkillType,
    string TargetingType,
    string EffectType,
    int ManaCost,
    int ChargeCost,
    int BasePower,
    string Category,
    bool BasePowerIsPercentOfMaxVitality,
    IReadOnlyCollection<SkillEffectView> Effects,
    IReadOnlyCollection<string> AcquisitionHints,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    int Cooldown = 0,
    bool IsUltimate = false,
    string EmotionalRegister = null!,
    // Compatibility is resolved server-side from Catalog character definitions; the
    // client never receives a second archetype model to interpret.
    IReadOnlyCollection<string>? CompatibleCharacterDefinitionKeys = null,
    // "Player"/"Enemy"/"Any" — always non-"Enemy" here (already filtered out server-side).
    // Surfaced anyway so the Grimoire can defensively re-filter without depending solely on
    // this endpoint's own filter staying correct.
    string Audience = "Player");

public sealed record SkillEffectView(
    string Kind,
    string? StatusKey,
    int Magnitude,
    int DurationTicks,
    int TickInterval,
    string? Stat,
    bool MagnitudeIsPercentOfMax,
    bool MagnitudeIsPercentOfBaseStat,
    bool AppliesToActor,
    bool IsPermanent);

public sealed record ListActiveSkillDefinitionsResponse(IReadOnlyCollection<SkillDefinitionView> Skills);
