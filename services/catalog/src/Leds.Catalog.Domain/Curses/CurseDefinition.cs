using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;

namespace Leds.Catalog.Domain.Curses;

public sealed class CurseDefinition : CatalogContentBase, ICurseDefinition
{
    private CurseDefinition(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        string? narrativeText,
        int severity,
        string duration,
        string? trigger,
        string? effectSetKey)
        : base(id, key, name, description, version, status)
    {
        NarrativeText = string.IsNullOrWhiteSpace(narrativeText) ? null : narrativeText.Trim();
        Severity = severity;
        Duration = string.IsNullOrWhiteSpace(duration) ? "NextCombatOnly" : duration.Trim();
        Trigger = string.IsNullOrWhiteSpace(trigger) ? null : trigger.Trim();
        EffectSetKey = string.IsNullOrWhiteSpace(effectSetKey) ? null : effectSetKey.Trim();
    }

    public string? NarrativeText { get; }

    public int Severity { get; }

    public string Duration { get; }

    public string? Trigger { get; }

    public string? EffectSetKey { get; }

    public static CurseDefinition Create(
        string key,
        string displayName,
        string description,
        string version,
        string? narrativeText,
        int severity,
        string duration,
        string? trigger,
        string? effectSetKey,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        return new CurseDefinition(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(displayName),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            narrativeText,
            severity,
            duration,
            trigger,
            effectSetKey);
    }
}
