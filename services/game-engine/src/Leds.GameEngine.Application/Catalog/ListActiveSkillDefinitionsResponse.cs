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
    string EmotionalRegister = "Neutral",
    // The skill's OWN "élément" (registre émotionnel intrinsèque), resolved the exact same
    // way as CombatantSkillRuntimeDto.EmotionalType — see EmotionalTypeProfileProvider.
    // Null for basic attacks and any skill without a declared type; distinct from
    // EmotionalRegister above, which is a near-always-"Neutral" catalog seed field.
    string? EmotionalType = null,
    // Player archetypes allowed to equip this skill — empty means unrestricted. Enforced
    // server-side by SkillArchetypeGate; surfaced here so the Grimoire can grey out
    // incompatible skills instead of only failing on submit. This response only ever
    // contains Audience "Player"/"Any" skills — see ListActiveSkillDefinitionsQueryHandler.
    IReadOnlyCollection<string>? AllowedArchetypes = null,
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
