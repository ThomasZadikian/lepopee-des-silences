namespace Leds.GameEngine.Domain.Combats;

/// <summary>
/// L'unité d'authoring des durées du combat tactique.
/// </summary>
/// <remarks>
/// <para>
/// Les durées de statut, de DoT et de garde continue restent exprimées en ticks dans le
/// catalogue. Un tour tactique vaut <see cref="TicksPerTurn"/>, ce qui conserve un contrat
/// d'authoring déterministe sans exposer cette unité technique au joueur.
/// </para>
/// </remarks>
public static class CombatTime
{
    /// <summary>Durée d'un tour de combat, en ticks.</summary>
    public const int TicksPerTurn = 2500;
}
