using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Items.Definitions.GetItemDefinitionByKey;

public sealed record GetItemDefinitionByKeyQuery(string Key)
    : IQuery<GetItemDefinitionByKeyResponse>;
