using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Curses.GetCurseDefinitionByKey;

public sealed record GetCurseDefinitionByKeyQuery(string Key)
    : IQuery<GetCurseDefinitionByKeyResponse>;
