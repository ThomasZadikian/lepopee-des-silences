namespace Leds.GameEngine.Domain.Combats;

public sealed record DamageResult(
    CombatantId AttackerId,
    CombatantId DefenderId,
    int RawPower,
    int MitigatedByDefense,
    int Damage);