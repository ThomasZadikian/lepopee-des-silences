namespace Leds.GameEngine.Application.Catalog;

public sealed record CatalogNpcOffering(
    string Key,
    string Kind,
    string? TargetKey,
    int Amount,
    bool IsMajor,
    IReadOnlyCollection<CatalogDialogueRequirement> UnlockConditions,
    CatalogCompanionKit? CompanionKit = null);

// The authored combat kit a recruited companion fights with — only meaningful when
// the owning offering's Kind is "Companion". See CompanionKitSpec on the catalog side.
public sealed record CatalogCompanionKit(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Recovery,
    int Focus,
    int Mana,
    int Charge,
    IReadOnlyCollection<string> SkillKeys);
