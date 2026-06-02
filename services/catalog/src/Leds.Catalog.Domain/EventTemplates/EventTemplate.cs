using Leds.Catalog.Domain.Abstractions;
using Leds.Catalog.Domain.CatalogContent;
using Leds.Catalog.Domain.Errors;

namespace Leds.Catalog.Domain.EventTemplates;

public sealed class EventTemplate : CatalogContentBase, IEventTemplate
{
    private readonly List<string> _narrativeTags;

    private EventTemplate(
        CatalogContentId id,
        CatalogContentKey key,
        CatalogContentName name,
        CatalogContentDescription description,
        CatalogContentVersion version,
        CatalogContentStatus status,
        EventTemplateType type,
        EventOutcomeKind defaultOutcomeKind,
        int minRiskLevel,
        int maxRiskLevel,
        bool requiresPlayerChoice,
        IReadOnlyCollection<string> narrativeTags)
        : base(id, key, name, description, version, status)
    {
        Type = type;
        DefaultOutcomeKind = defaultOutcomeKind;
        MinRiskLevel = minRiskLevel;
        MaxRiskLevel = maxRiskLevel;
        RequiresPlayerChoice = requiresPlayerChoice;
        _narrativeTags = narrativeTags.ToList();
    }

    public EventTemplateType Type { get; }

    public EventOutcomeKind DefaultOutcomeKind { get; }

    public int MinRiskLevel { get; }

    public int MaxRiskLevel { get; }

    public bool RequiresPlayerChoice { get; }

    public IReadOnlyCollection<string> NarrativeTags => _narrativeTags.AsReadOnly();

    public static EventTemplate Create(
        string key,
        string name,
        string? description,
        string version,
        EventTemplateType type,
        EventOutcomeKind defaultOutcomeKind,
        int minRiskLevel,
        int maxRiskLevel,
        bool requiresPlayerChoice,
        IReadOnlyCollection<string>? narrativeTags = null,
        CatalogContentStatus status = CatalogContentStatus.Draft)
    {
        EnsureRiskLevelIsValid(minRiskLevel, "Event template min risk level");
        EnsureRiskLevelIsValid(maxRiskLevel, "Event template max risk level");

        if (minRiskLevel > maxRiskLevel)
        {
            throw new DomainException(
                "Event template min risk level cannot be greater than max risk level.");
        }

        var normalizedTags = NormalizeNarrativeTags(narrativeTags);

        return new EventTemplate(
            CatalogContentId.New(),
            CatalogContentKey.From(key),
            CatalogContentName.From(name),
            CatalogContentDescription.From(description),
            CatalogContentVersion.From(version),
            status,
            type,
            defaultOutcomeKind,
            minRiskLevel,
            maxRiskLevel,
            requiresPlayerChoice,
            normalizedTags);
    }

    private static void EnsureRiskLevelIsValid(int value, string fieldName)
    {
        if (value is < 0 or > 100)
        {
            throw new DomainException($"{fieldName} must be between 0 and 100.");
        }
    }

    private static IReadOnlyCollection<string> NormalizeNarrativeTags(
        IReadOnlyCollection<string>? narrativeTags)
    {
        if (narrativeTags is null || narrativeTags.Count == 0)
        {
            return Array.Empty<string>();
        }

        return narrativeTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}