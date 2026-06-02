using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.PalaceLaws.GetPalaceLawDefinitionByKey;

public sealed record GetPalaceLawDefinitionByKeyQuery(string Key)
    : IQuery<GetPalaceLawDefinitionByKeyResponse>;