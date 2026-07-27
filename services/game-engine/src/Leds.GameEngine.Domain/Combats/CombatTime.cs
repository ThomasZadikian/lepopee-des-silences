namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// L'unité de temps commune aux deux systèmes de combat.
/// </summary>
/// <remarks>
/// <para>
/// Les durées de statut, de DoT et de garde continue sont exprimées en ticks et authorées une
/// seule fois au catalogue. Pour qu'elles gardent le même sens des deux côtés, l'ATB et le
/// tactique comptent le temps avec la même règle : un tour vaut <see cref="TicksPerTurn"/>.
/// L'ATB y arrive en remplissant des jauges, le tactique en franchissant un round entier d'un
/// coup — mais « trois tours de poison » dure trois tours dans les deux cas.
/// </para>
/// <para>
/// Cette constante vivait dans <c>Combats/Atb/AtbConstants</c>, qui la conserve désormais par
/// délégation. Elle n'a jamais été une notion d'ATB : c'est l'échelle du temps de combat, et la
/// laisser là aurait obligé l'agrégat tactique à importer le namespace du système dont il doit
/// justement rester indépendant (cf. SFD v2, §2).
/// </para>
/// </remarks>
public static class CombatTime
{
    /// <summary>Durée d'un tour de combat, en ticks.</summary>
    public const int TicksPerTurn = 2500;
}
