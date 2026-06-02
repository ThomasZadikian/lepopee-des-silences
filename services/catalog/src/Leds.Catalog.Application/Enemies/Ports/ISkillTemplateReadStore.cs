using Leds.Catalog.Domain.Skills;

namespace Leds.Catalog.Application.Skills.Ports;

public interface ISkillTemplateReadStore
{
    Task<IReadOnlyCollection<ISkillTemplate>> ListActiveAsync(
        CancellationToken cancellationToken);

    Task<ISkillTemplate?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}