namespace Leds.GameEngine.Application.Events.Dtos;

public sealed record NodeEventChoiceDto(
    string ChoiceId,
    string Label,
    string ConsequencePreview);