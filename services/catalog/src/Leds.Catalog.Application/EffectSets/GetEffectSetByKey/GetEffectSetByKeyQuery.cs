using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.EffectSets.GetEffectSetByKey;

public sealed record GetEffectSetByKeyQuery(string Key)
    : IQuery<GetEffectSetByKeyResponse>;
