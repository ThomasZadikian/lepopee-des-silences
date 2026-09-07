namespace Leds.Catalog.Application.Archetypes;

public interface IArchetypeDefinitionReadStore
{
    Task<ArchetypeDefinitionDto?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ArchetypeDefinitionDto>> ListActiveAsync(CancellationToken cancellationToken);
}
