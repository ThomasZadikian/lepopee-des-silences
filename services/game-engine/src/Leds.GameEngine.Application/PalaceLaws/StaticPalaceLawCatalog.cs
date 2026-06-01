using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Application.Events.ChooseEventOption;


namespace Leds.GameEngine.Application.PalaceLaws;

public sealed class StaticPalaceLawCatalog : IPalaceLawCatalog
{
    public PalaceLaw GetDefaultLawFor(CurrentEventChoiceResolutionContext context)
    {
        return PalaceLaw.Create(
            key: "law-silence-v1",
            name: "Loi du Silence",
            version: "1.0.0",
            domains: new[]
            {
                PalaceLawDomain.Narrative,
                PalaceLawDomain.Generation
            });
    }
}