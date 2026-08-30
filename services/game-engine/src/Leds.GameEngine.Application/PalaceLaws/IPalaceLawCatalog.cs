using Leds.GameEngine.Application.Events.ChooseEventOption;
using Leds.GameEngine.Domain.PalaceLaws;

namespace Leds.GameEngine.Application.PalaceLaws;

public interface IPalaceLawCatalog
{
    PalaceLaw GetDefaultLawFor(CurrentEventChoiceResolutionContext context);
}