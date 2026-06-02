using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Items.GetItemTemplateByKey;

public sealed record GetItemTemplateByKeyQuery(string Key)
    : IQuery<GetItemTemplateByKeyResponse>;