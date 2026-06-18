using Leds.Catalog.Application.Npcs.Definitions.Dtos;

namespace Leds.Catalog.Application.Npcs.Definitions.ListActiveNpcDefinitions;

public sealed record ListActiveNpcDefinitionsResponse(
    IReadOnlyCollection<NpcDefinitionDto> Definitions);
