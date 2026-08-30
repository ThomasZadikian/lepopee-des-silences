using Leds.GameEngine.Application.Catalog;

namespace Leds.GameEngine.Application.Events.Npcs;

public interface INpcEncounterSelector
{
    CatalogNpcDefinition? SelectEligibleNpc(
        NpcEligibilityContext context,
        IReadOnlyCollection<CatalogNpcDefinition> allNpcs);
}
