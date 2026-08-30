using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Runs.PalaceIndicators;

public interface IPalacePublicIndicatorProjectionService
{
    IReadOnlyCollection<PalacePublicIndicatorDto> Project(
        Run run,
        IReadOnlyCollection<PalaceIndicator>? persistedIndicators = null);
}
