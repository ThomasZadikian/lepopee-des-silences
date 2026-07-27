using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Rooms;

namespace Leds.GameEngine.Domain.Combats.Tactical;

/// <summary>
/// Le terrain sur lequel un combat tactique se joue : la salle d'exploration débarrassée de ses
/// nœuds (cf. SFD v2, §4).
/// </summary>
/// <remarks>
/// <para>
/// C'est un <b>instantané immuable</b>, pas une vue sur la <see cref="RoomGrid"/> vivante. Deux
/// raisons. D'abord le vidage des nœuds : le combat ne doit voir que la matière brute — forme de
/// la salle, cases infranchissables, élévation — et un instantané rend ce vidage structurel au
/// lieu de reposer sur la discipline de l'appelant. Ensuite l'isolation : la grille d'exploration
/// continue de porter la position du groupe et son budget de déplacement, qui n'ont aucun sens
/// pendant un combat et ne doivent pas pouvoir être modifiés par lui.
/// </para>
/// <para>
/// La diversité du terrain vient donc gratuitement de la génération procédurale existante :
/// aucun authoring de carte de combat n'est nécessaire.
/// </para>
/// </remarks>
public sealed class TacticalBattlefield
{
    private readonly int[] _elevation;
    private readonly bool[] _walkable;

    private TacticalBattlefield(int width, int height, int[] elevation, bool[] walkable)
    {
        Width = width;
        Height = height;
        _elevation = elevation;
        _walkable = walkable;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Nombre de cases praticables — la surface réellement disponible pour déployer.</summary>
    public int WalkableCellCount => _walkable.Count(w => w);

    public bool Contains(GridPosition position)
        => position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

    /// <summary>
    /// Praticable = dans la salle, et sans obstacle. Ne dit rien de l'occupation : un combattant
    /// vivant bloque sa case, mais c'est au combat de le savoir, pas au terrain.
    /// </summary>
    public bool IsWalkable(GridPosition position)
        => Contains(position) && _walkable[Index(position)];

    public int ElevationAt(GridPosition position)
        => Contains(position) ? _elevation[Index(position)] : 0;

    /// <summary>
    /// Coût de déplacement pour entrer sur <paramref name="to"/> depuis <paramref name="from"/> :
    /// une case de base, plus une par palier d'élévation franchi. Gravir coûte donc autant que
    /// descendre — le terrain freine dans les deux sens (cf. SFD v2, §9 et §11).
    /// </summary>
    public int StepCost(GridPosition from, GridPosition to)
        => 1 + Math.Abs(ElevationAt(to) - ElevationAt(from));

    private int Index(GridPosition position) => (position.Y * Width) + position.X;

    /// <summary>
    /// Fige le terrain d'une salle d'exploration pour en faire un champ de bataille. Les nœuds
    /// ne sont pas lus : c'est ici que « la grille se vide de ses nœuds » devient concret.
    /// </summary>
    public static TacticalBattlefield FromRoomGrid(RoomGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        var size = grid.Width * grid.Height;
        var elevation = new int[size];
        var walkable = new bool[size];

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var index = (y * grid.Width) + x;
                elevation[index] = grid.ElevationAt(x, y);
                walkable[index] = grid.IsWalkable(x, y);
            }
        }

        return new TacticalBattlefield(grid.Width, grid.Height, elevation, walkable);
    }

    /// <summary>Reconstruit un champ de bataille depuis sa forme persistée.</summary>
    public static TacticalBattlefield Rehydrate(
        int width, int height, IReadOnlyList<int> elevation, IReadOnlyList<bool> walkable)
    {
        if (width <= 0 || height <= 0)
            throw new DomainException("A tactical battlefield must have a positive width and height.");

        var size = width * height;
        if (elevation.Count != size || walkable.Count != size)
            throw new DomainException(
                $"Tactical battlefield terrain must hold exactly {size} cells "
                + $"(got {elevation.Count} elevation / {walkable.Count} walkable).");

        return new TacticalBattlefield(width, height, [.. elevation], [.. walkable]);
    }
}
