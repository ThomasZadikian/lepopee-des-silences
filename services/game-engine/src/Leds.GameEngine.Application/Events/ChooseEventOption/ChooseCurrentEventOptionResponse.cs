using Leds.GameEngine.Application.Runs.Dtos;

namespace Leds.GameEngine.Application.Events.ChooseEventOption;

public sealed record ChooseCurrentEventOptionResponse(
    RunDto Run,
    ChosenEventOptionResultDto Result);