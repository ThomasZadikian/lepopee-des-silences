namespace Leds.GameEngine.Application.Runs.Dtos;

public sealed record NodeLayerDto(
    int Depth,
    IReadOnlyCollection<NodeDto> Nodes);