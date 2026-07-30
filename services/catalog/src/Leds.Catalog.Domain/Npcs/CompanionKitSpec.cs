namespace Leds.Catalog.Domain.Npcs;

// The authored combat kit a recruited companion fights with — only meaningful on an
// NpcOffering whose Kind is Companion. Mirrors the stat/skill shape RecruitCompanionAsync
// already accepts on the game-engine side, so this flows straight through unchanged.
public sealed record CompanionKitSpec(
    int MaxVitality,
    int AttackPower,
    int Defense,
    int StartingGuard,
    int Speed,
    int Initiative,
    int Focus,
    int Mana,
    int Charge,
    IReadOnlyList<string> SkillKeys,
    int MagicAttack = 0,
    int MagicDefense = 0);
