namespace Leds.Player.Domain.Players;

// Cross-service contract, not a coincidence of naming: game-engine's
// EquipItemCommandHandler derives one of exactly these three member names (via
// CatalogRunItemMapper.MapEquipSlot) and sends it as a plain string over HTTP
// (?slot=...), bound here by ASP.NET's case-insensitive enum model binding
// (PlayersController.EquipItem). Player-service has no Catalog access and never
// re-derives a slot from an item's category — it only trusts the caller. Renaming
// a member here without updating MapEquipSlot's switch breaks that binding silently
// (an unrecognized query value 400s, it does not throw at compile time).
public enum EquipmentSlotKind
{
    Weapon = 1,
    Accessory = 2,
    Relic = 3
}
