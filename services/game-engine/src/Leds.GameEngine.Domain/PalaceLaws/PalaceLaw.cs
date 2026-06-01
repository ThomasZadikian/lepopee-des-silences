using Leds.GameEngine.Domain.Common;

namespace Leds.GameEngine.Domain.PalaceLaws;

public sealed class PalaceLaw
{
    private readonly List<PalaceLawDomain> _domains;

    private PalaceLaw(
        PalaceLawId id,
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains)
    {
        Id = id;
        Key = key;
        Name = name;
        Version = version;
        _domains = domains.ToList();
    }

    public PalaceLawId Id { get; }

    public string Key { get; }

    public string Name { get; }

    public string Version { get; }

    public IReadOnlyCollection<PalaceLawDomain> Domains => _domains.AsReadOnly();

    public static PalaceLaw Create(
        string key,
        string name,
        string version,
        IReadOnlyCollection<PalaceLawDomain> domains)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("Palace law key is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Palace law name is required.");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainException("Palace law version is required.");
        }

        if (domains is null || domains.Count == 0)
        {
            throw new DomainException("A palace law must target at least one domain.");
        }

        return new PalaceLaw(
            PalaceLawId.New(),
            key.Trim(),
            name.Trim(),
            version.Trim(),
            domains.Distinct().ToArray());
    }
}