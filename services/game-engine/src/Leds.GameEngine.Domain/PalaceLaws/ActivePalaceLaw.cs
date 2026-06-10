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
        IReadOnlyCollection<PalaceLawDomain> domains)
    {
        LawId = lawId;
        Key = key;
        Name = name;
        Version = version;
        _domains = domains.ToList();
    }

    public PalaceLawId LawId { get; }

    public string Key { get; }

    public string Name { get; }

    public string Version { get; }

    public IReadOnlyCollection<PalaceLawDomain> Domains => _domains.AsReadOnly();

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
            law.Domains);
    }

    public static ActivePalaceLaw Rehydrate(
        PalaceLawId lawId,
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains)
    {
        return new ActivePalaceLaw(lawId, key, name, version, domains);
    }
}