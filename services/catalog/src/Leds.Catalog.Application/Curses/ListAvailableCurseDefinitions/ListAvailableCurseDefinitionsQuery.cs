using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.Curses.ListAvailableCurseDefinitions;

public sealed record ListAvailableCurseDefinitionsQuery()
    : IQuery<ListAvailableCurseDefinitionsResponse>;
