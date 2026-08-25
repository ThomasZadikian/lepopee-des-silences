using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.Dtos;

/// <param name="CurrentRoomIndex">
/// Zero-based position of the current room in the infinite run sequence.
/// Threshold room = 0. Incremented by MoveToNextRoom (future).
/// </param>
/// <param name="CurrentRoomNumber">
/// One-based room number for player display ("Salle 1", "Salle 2", …).
/// Always equals <c>CurrentRoomIndex + 1</c>. Display-only — do not use for game logic.
/// </param>
/// <param name="CanResume">
/// <c>true</c> when the run is in <see cref="RunStatus.Suspended"/> state and can be resumed.
/// </param>
/// <param name="SavedAt">
/// The timestamp at which the player saved and exited the run. <c>null</c> if never suspended.
/// </param>
/// <param name="AbandonedAt">
/// The timestamp at which the run was resolved with <see cref="RunOutcome.Abandon"/>.
/// Equals <c>EndedAt</c> on the domain object. <c>null</c> for non-abandoned runs.
/// </param>
public sealed record RunDto(
    Guid Id,
    Guid PlayerId,
    string Seed,
    string GeneratorVersion,
    string MarkovMatrixVersion,
    string Status,
    int CurrentDepth,
    Guid? ActiveCombatId,
    Guid? PendingRewardOfferId,
    RoomDto CurrentRoom,
    IReadOnlyCollection<RoomDto> Rooms,
    IReadOnlyCollection<ActivePalaceLawDto> ActivePalaceLaws,
    IReadOnlyCollection<RunItemDto> InventoryItems,
    int RunItemCapacity,
    int CurrentRoomIndex,
    int CurrentRoomNumber,
    bool CanResume,
    DateTimeOffset? SavedAt,
    DateTimeOffset? AbandonedAt,
    PlayerRuntimeStateDto? PlayerState,
    IReadOnlyCollection<RunModifierDto>? ActiveModifiers = null,
    RunPartySnapshotDto? Party = null,
    IReadOnlyCollection<PalacePublicIndicatorDto>? PalaceIndicators = null,
    bool JournalEnabled = false,
    IReadOnlyCollection<RunJournalEntryDto>? JournalEntries = null,
    bool LawDenialEnabled = false,
    bool CanUseLawDenial = false,
    bool CaliceInfiniEnabled = false,
    bool CanUseCaliceInfini = false,
    string? Outcome = null,
    long Revision = 0,
    string TechnicalRecoveryState = "None")
{
    public static RunDto FromDomain(
        Run run,
        IReadOnlyCollection<PalaceIndicator>? palaceIndicators = null,
        IReadOnlyCollection<PalacePublicIndicatorDto>? projectedPalaceIndicators = null)
    {
        var activeModifiers = run.RunModifiers
            .Where(m => !m.IsConsumed)
            .Select(RunModifierDto.FromDomain)
            .ToArray();

        var publicIndicators = projectedPalaceIndicators?.ToArray()
            ?? (palaceIndicators ?? [])
                .Where(indicator => !indicator.IsExpired)
                .Select(PalacePublicIndicatorDto.FromDomain)
                .ToArray();
        var currentRoomDto = RoomDto.FromDomain(run.CurrentRoom, run.RunModifiers, run.GroundItems);

        return new RunDto(
            run.Id.Value,
            run.PlayerId,
            run.Seed,
            run.GeneratorVersion,
            run.MarkovMatrixVersion,
            run.Status.ToString(),
            run.CurrentDepth,
            run.ActiveCombatId?.Value,
            run.PendingRewardOfferId?.Value,
            currentRoomDto,
            new[] { currentRoomDto },
            run.ActivePalaceLaws.Where(law => !law.IsConsumed).Select(ActivePalaceLawDto.FromDomain).ToArray(),
            run.RunItems.Select(RunItemDto.FromDomain).ToArray(),
            run.RunItemCapacity,
            run.CurrentRoomIndex,
            run.CurrentRoomIndex + 1,
            CanResume: run.Status == RunStatus.Suspended,
            SavedAt: run.SavedAt,
            AbandonedAt: run.Outcome == RunOutcome.Abandon ? run.EndedAt : null,
            PlayerState: PlayerRuntimeStateDto.FromDomain(run.PlayerState),
            ActiveModifiers: activeModifiers.Length > 0 ? activeModifiers : null,
            Party: RunPartySnapshotDto.FromDomain(run.PlayerSnapshot, run.PlayerState),
            PalaceIndicators: publicIndicators,
            JournalEnabled: run.JournalEnabled,
            JournalEntries: run.JournalEntries.Select(RunJournalEntryDto.FromDomain).ToArray(),
            LawDenialEnabled: run.LawDenialEnabled,
            CanUseLawDenial: run.CanUseLawDenial,
            CaliceInfiniEnabled: run.CaliceInfiniEnabled,
            CanUseCaliceInfini: run.CanUseCaliceInfini,
            Outcome: run.Outcome?.ToString(),
            Revision: run.Revision,
            TechnicalRecoveryState: run.TechnicalRecoveryState.ToString());
    }
}

public sealed record RunJournalEntryDto(
    int RoomIndex,
    int RoomNumber,
    string? RoomDisplayName,
    string Text)
{
    public static RunJournalEntryDto FromDomain(RunJournalEntry entry) => new(
        entry.RoomIndex,
        entry.RoomIndex + 1,
        entry.RoomDisplayName,
        entry.Text);
}

public sealed record PalacePublicIndicatorDto(
    string Key,
    string Label,
    string? Description,
    string? Category,
    string? Level,
    string? Tone,
    string? Source)
{
    public static PalacePublicIndicatorDto FromDomain(PalaceIndicator indicator)
    {
        ArgumentNullException.ThrowIfNull(indicator);

        var publicIntensity = NormalizePublicIntensity(indicator.Intensity);

        return new PalacePublicIndicatorDto(
            indicator.IndicatorKey,
            indicator.DisplayLabel,
            indicator.NarrativeText,
            Category: null,
            Level: publicIntensity.Level,
            Tone: publicIntensity.Tone,
            Source: "run");
    }

    private static (string? Level, string? Tone) NormalizePublicIntensity(string intensity)
    {
        var normalized = intensity.Trim().ToLowerInvariant();

        return normalized switch
        {
            "low" or "medium" or "high" or "critical" => (normalized, null),
            "calm" or "stable" => (null, "calm"),
            "unstable" or "tense" => (null, "unstable"),
            "danger" or "dangerous" => (null, "danger"),
            "mystery" or "unknown" => (null, "mystery"),
            _ => (null, null)
        };
    }
}

public sealed record RunModifierDto(
    Guid Id,
    string Type,
    double Value,
    string Duration,
    string SourceType,
    string SourceKey)
{
    public static RunModifierDto FromDomain(RunModifier modifier) => new(
        modifier.Id.Value,
        modifier.Type.ToString(),
        modifier.Value,
        modifier.Duration.ToString(),
        modifier.SourceType,
        modifier.SourceKey);
}

public sealed record RunItemDto(
    Guid Id,
    string DefinitionKey,
    string DisplayName,
    string Description,
    string Type,
    string Rarity,
    int Quantity,
    string EffectType,
    int EffectAmount,
    bool IsUsable = false,
    bool IsContainer = false,
    int? ContainerCapacity = null,
    bool IsLiquid = false,
    string? ContainedLiquidDefinitionKey = null,
    int TacticalRange = 1,
    string TacticalAreaShape = "Single",
    bool RequiresLineOfSight = false,
    // Weapon/Accessory/Relic, or null when this Type isn't equippable at all — same
    // CatalogRunItemMapper.MapEquipSlot authority as the catalog item listing and the
    // actual equip command, so the frontend never re-derives it.
    string? EquipSlot = null)
{
    public static RunItemDto FromDomain(RunItem item) => new(
        item.Id.Value,
        item.DefinitionKey,
        item.DisplayName,
        item.Description,
        item.Type.ToString(),
        item.Rarity.ToString(),
        item.Quantity,
        item.EffectType.ToString(),
        item.EffectAmount,
        item.IsUsable,
        item.IsContainer,
        item.ContainerCapacity,
        item.IsLiquid,
        item.ContainedLiquidDefinitionKey,
        item.TacticalRange,
        item.TacticalAreaShape,
        item.RequiresLineOfSight,
        CatalogRunItemMapper.MapEquipSlot(item.Type, item.UsageMode));
}
