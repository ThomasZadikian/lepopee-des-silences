using Leds.Catalog.Application.Worlds.Dtos;

namespace Leds.Catalog.Application.Worlds.ListActiveWorldDefinitions;

public sealed record ListActiveWorldDefinitionsResponse(IReadOnlyCollection<WorldDefinitionDto> Definitions);
