namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Une case du champ de bataille tactique.
/// </summary>
/// <remarks>
/// La position vit ici, et non sur <see cref="Combatant"/>, parce que le combattant est partagé
/// avec l'ATB — qui n'a pas de coordonnées, seulement des rangs. Charger <c>Combatant</c> d'un
/// couple (x, y) obligerait l'ATB à porter un état qui n'a aucun sens pour lui (cf. SFD v2, §2 :
/// le modèle spatial est ce qui sépare les deux systèmes, pas ce qu'ils partagent).
/// </remarks>
public readonly record struct GridPosition(int X, int Y)
{
    /// <summary>
    /// Distance de Manhattan. C'est la métrique du jeu : les déplacements et les portées sont
    /// à quatre directions, jamais en diagonale.
    /// </summary>
    public int ManhattanDistanceTo(GridPosition other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>Les quatre cases adjacentes, sans considération de terrain.</summary>
    public IEnumerable<GridPosition> Neighbours()
    {
        yield return new GridPosition(X + 1, Y);
        yield return new GridPosition(X - 1, Y);
        yield return new GridPosition(X, Y + 1);
        yield return new GridPosition(X, Y - 1);
    }

    public override string ToString() => $"({X}, {Y})";
}
