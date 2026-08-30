using Leds.Catalog.Domain.Skills.Definitions;

namespace Leds.Catalog.Application.Skills.Definitions.Ports;

public interface ISkillDefinitionReadStore
{
    Task<IReadOnlyCollection<ISkillDefinition>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<ISkillDefinition?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ISkillDefinition>> ListByTypeAsync(
        string skillType,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ISkillDefinition>> ListByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken);
}
