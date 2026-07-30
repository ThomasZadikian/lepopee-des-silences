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
    private readonly bool[] _isFloor;

    private TacticalBattlefield(
        int width, int height, int[] elevation, bool[] walkable, bool[] isFloor)
    {
        Width = width;
        Height = height;
        _elevation = elevation;
        _walkable = walkable;
        _isFloor = isFloor;
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

    /// <summary>
    /// La case appartient-elle à la salle ?
    /// </summary>
    /// <remarks>
    /// Distinct de <see cref="IsWalkable"/>, et la distinction compte : une case du vide et une
    /// case encombrée sont toutes deux infranchissables, mais l'une n'existe pas et l'autre
    /// porte un obstacle. Les confondre fait pousser un éboulis sur chaque case hors de la
    /// salle — la salle entière se retrouve alors ceinturée de décor qui n'existe pas.
    /// </remarks>
    public bool IsFloor(GridPosition position)
        => Contains(position) && _isFloor[Index(position)];

    /// <summary>Une case de la salle rendue infranchissable par ce qui l'encombre.</summary>
    public bool IsObstacle(GridPosition position)
        => IsFloor(position) && !_walkable[Index(position)];

    public int ElevationAt(GridPosition position)
        => Contains(position) ? _elevation[Index(position)] : 0;

    /// <summary>
    /// Coût de déplacement pour entrer sur <paramref name="to"/> depuis <paramref name="from"/> :
    /// une case sur terrain plat, deux points pour toute montée, zéro pour toute descente.
    /// </summary>
    public int StepCost(GridPosition from, GridPosition to)
    {
        var delta = ElevationAt(to) - ElevationAt(from);
        return delta switch
        {
            > 0 => 2,
            < 0 => 0,
            _ => 1
        };
    }

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
        var isFloor = new bool[size];

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var index = (y * grid.Width) + x;
                elevation[index] = grid.ElevationAt(x, y);
                walkable[index] = grid.IsWalkable(x, y);
                isFloor[index] = grid.IsFloor(x, y);
            }
        }

        return new TacticalBattlefield(grid.Width, grid.Height, elevation, walkable, isFloor);
    }

    /// <summary>Reconstruit un champ de bataille depuis sa forme persistée.</summary>
    public static TacticalBattlefield Rehydrate(
        int width,
        int height,
        IReadOnlyList<int> elevation,
        IReadOnlyList<bool> walkable,
        IReadOnlyList<bool>? isFloor = null)
    {
        if (width <= 0 || height <= 0)
            throw new DomainException("A tactical battlefield must have a positive width and height.");

        var size = width * height;
        if (elevation.Count != size || walkable.Count != size)
            throw new DomainException(
                $"Tactical battlefield terrain must hold exactly {size} cells "
                + $"(got {elevation.Count} elevation / {walkable.Count} walkable).");

        // Repli sur « praticable = dans la salle » pour les combats enregistrés avant que la
        // distinction n'existe : ils perdent leurs obstacles, jamais leur forme.
        var floor = isFloor is null || isFloor.Count != size
            ? walkable.ToArray()
            : isFloor.ToArray();

        return new TacticalBattlefield(width, height, [.. elevation], [.. walkable], floor);
    }

    /// <summary>
    /// Vérifie si une ligne de vue (Line of Sight) existe entre deux cases,
    /// en tenant compte des obstacles et des différences d'élévation.
    /// 
    /// Optimisé pour O-010 : utilise un cache interne pour éviter les recalculs.
    /// </summary>
    /// <remarks>
    /// Cette méthode est statique pour rester compatible avec l'existant, mais elle pourrait
    /// être instanciée avec un cache de visibilité pour de meilleures performances.
    /// </remarks>
    public bool HasLineOfSight(GridPosition from, GridPosition to)
        => TacticalMovement.HasLineOfSight(this, from, to);
}
