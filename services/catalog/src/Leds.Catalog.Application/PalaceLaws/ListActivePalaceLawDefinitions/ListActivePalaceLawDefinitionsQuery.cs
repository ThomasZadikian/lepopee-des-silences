using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.PalaceLaws.ListActivePalaceLawDefinitions;

public sealed record ListActivePalaceLawDefinitionsQuery()
    : IQuery<ListActivePalaceLawDefinitionsResponse>;