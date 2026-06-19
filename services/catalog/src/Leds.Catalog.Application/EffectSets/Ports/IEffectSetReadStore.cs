using Leds.Catalog.Application.EffectSets.Dtos;

namespace Leds.Catalog.Application.EffectSets.Ports;

public interface IEffectSetReadStore
{
    Task<EffectSetDto?> GetDtoByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}
