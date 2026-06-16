using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.PalaceLaws;

public sealed class ActivePalaceLaw
{
    private readonly List<PalaceLawDomain> _domains;

    private ActivePalaceLaw(
        PalaceLawId lawId,
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains,
        string? displayName,
        string? description,
        string? duration,
        DateTime? appliedAtUtc,
        Guid? expiresAtRoomId,
        DateTime? consumedAtUtc)
    {
        LawId = lawId;
        Key = key;
        Name = name;
        Version = version;
        _domains = domains.ToList();
        DisplayName = displayName ?? name;
        Description = description ?? string.Empty;
        Duration = duration ?? "UntilRunEnds";
        AppliedAtUtc = appliedAtUtc ?? DateTime.UtcNow;
        ExpiresAtRoomId = expiresAtRoomId;
        ConsumedAtUtc = consumedAtUtc;
    }

    public PalaceLawId LawId { get; }
    public string Key { get; }
    public string Name { get; }
    public string Version { get; }
    public IReadOnlyCollection<PalaceLawDomain> Domains => _domains.AsReadOnly();
    public string DisplayName { get; }
    public string Description { get; }
    public string Duration { get; }
    public DateTime AppliedAtUtc { get; }
    public Guid? ExpiresAtRoomId { get; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public bool IsConsumed => ConsumedAtUtc.HasValue;

    public void Consume(DateTime consumedAt)
    {
        if (!IsConsumed)
            ConsumedAtUtc = consumedAt;
    }

    public static ActivePalaceLaw From(PalaceLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);

        if (law.Domains.Count == 0)
        {
            throw new DomainException("An active palace law must target at least one domain.");
        }

        return new ActivePalaceLaw(
            law.Id,
            law.Key,
            law.Name,
            law.Version,
            law.Domains,
            displayName: law.Name,
            description: null,
            duration: "UntilRunEnds",
            appliedAtUtc: DateTime.UtcNow,
            expiresAtRoomId: null,
            consumedAtUtc: null);
    }

    public static ActivePalaceLaw CreateFromSnapshot(
        PalaceLawId lawId,
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains,
        string displayName,
        string? description,
        string duration,
        DateTime appliedAtUtc,
        Guid? expiresAtRoomId,
        DateTime? consumedAtUtc)
    {
        return new ActivePalaceLaw(
            lawId, key, name, version, domains,
            displayName, description, duration, appliedAtUtc,
            expiresAtRoomId, consumedAtUtc);
    }

    public static ActivePalaceLaw Rehydrate(
        PalaceLawId lawId,
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains,
        string? displayName = null,
        string? description = null,
        string? duration = null,
        DateTime? appliedAtUtc = null,
        Guid? expiresAtRoomId = null,
        DateTime? consumedAtUtc = null)
    {
        return new ActivePalaceLaw(
            lawId, key, name, version, domains,
            displayName, description, duration, appliedAtUtc,
            expiresAtRoomId, consumedAtUtc);
    }
}
