using Leds.Catalog.Application.EffectSets.Dtos;

namespace Leds.Catalog.Application.EffectSets.GetEffectSetByKey;

public sealed record GetEffectSetByKeyResponse(
    EffectSetDto? Definition);
