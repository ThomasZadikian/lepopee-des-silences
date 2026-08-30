namespace Leds.GameEngine.Domain.Combats.StatusEffects;

/// <summary>One thing a status effect did on a tick — surfaced for combat logs.</summary>
public sealed record StatusTickEvent(
    string EffectKey,
    string DisplayName,
    StatusEffectKind Kind,
    int Amount,    // damage dealt / vitality healed (0 for non-periodic)
    bool Expired,  // true if the effect ended on this tick
    IReadOnlyList<Guid?> StackSourceIds)
{
    public int StackCount => StackSourceIds.Count;
    public Guid? FirstStackSourceId => StackSourceIds.FirstOrDefault();
}
