namespace Leds.GameEngine.Application.Combats.Dtos;

/// <summary>Ce qu'un combattant a encaissé, à l'endroit où il l'a encaissé.</summary>
public sealed record TacticalImpactDto(
    Guid CombatantId,
    int X,
    int Y,
    /// <summary>Vitalité perdue. Négative pour un soin — la même case sert aux deux.</summary>
    int VitalityDelta,
    bool Defeated,
    /// <summary>
    /// Le coup est parti mais n'a pas touché. Sans ce drapeau, une esquive est indistinguable
    /// d'une compétence sans effet chiffré : dans les deux cas la vitalité ne bouge pas, et
    /// le joueur voit une action se lancer sans jamais savoir pourquoi elle n'a rien fait.
    /// </summary>
    bool Missed = false,
    /// <summary>
    /// Ce que la Garde a encaissé à la place de la vitalité. Un coup entièrement absorbé laisse
    /// <see cref="VitalityDelta"/> à zéro — sans ce champ, il serait aussi indistinguable d'une
    /// action sans effet que ne l'est un coup manqué, mais avec la mauvaise cause affichée
    /// (rien ne dirait que la Garde a fait son travail).
    /// </summary>
    int GuardAbsorbed = 0);

/// <summary>Une case du trajet, dans l'ordre où elle est foulée.</summary>
public sealed record TacticalStepDto(int X, int Y);

/// <summary>
/// Un moment du combat, décrit assez précisément pour être <b>rejoué</b> à l'écran.
/// </summary>
/// <remarks>
/// <para>
/// Le serveur résout un tour entier d'un coup — c'est la bonne façon de garantir l'autorité et
/// la cohérence. Mais un joueur qui voit l'état d'avant puis l'état d'après ne comprend rien à
/// ce qui s'est passé : les figures se téléportent et les dégâts apparaissent sans cause.
/// </para>
/// <para>
/// Cette chronologie est le remède : elle ne rejoue pas la <i>décision</i> — elle est prise, et
/// définitivement — mais sa <i>mise en scène</i>. Le client la déroule à son rythme, avec un
/// temps de réflexion avant chaque geste ennemi et un pas de marche par case.
/// </para>
/// </remarks>
public sealed record TacticalCombatEventDto(
    /// <summary>« Move », « Skill » ou « Item ».</summary>
    string Kind,
    Guid ActorId,
    string ActorName,
    /// <summary>Le trajet, case par case. Vide pour une action.</summary>
    IReadOnlyList<TacticalStepDto> Path,
    string? SkillKey,
    string? SkillName,
    int? TargetX,
    int? TargetY,
    IReadOnlyList<TacticalImpactDto> Impacts,
    /// <summary>
    /// Les cases que ce geste va couvrir, annoncées <b>avant</b> qu'il ne parte.
    /// </summary>
    /// <remarks>
    /// C'est la seule information qu'aucun état ne porte : une fois le coup résolu, la zone a
    /// disparu et il n'en reste que les conséquences. Renseignée par le serveur plutôt que
    /// devinée côté client, parce que lui seul connaît la forme réellement employée et les
    /// cibles réellement retenues — et parce qu'une cible manquée doit tout de même figurer
    /// dans la zone annoncée. Pour un déplacement, c'est le trajet lui-même.
    /// </remarks>
    IReadOnlyList<TacticalStepDto>? TelegraphCells = null)
{
    public const string MoveKind = "Move";
    public const string SkillKind = "Skill";
    public const string ItemKind = "Item";
    public const string TickKind = "Tick";

    private static IReadOnlyList<TacticalStepDto> Cells(
        IEnumerable<Domain.Combats.Tactical.GridPosition> cells) =>
        [.. cells.Select(p => new TacticalStepDto(p.X, p.Y))];

    public static TacticalCombatEventDto Move(
        Guid actorId, string actorName, IEnumerable<Domain.Combats.Tactical.GridPosition> path)
    {
        // Le trajet EST l'annonce : le montrer avant de l'emprunter dit d'où la créature va
        // surgir, ce qui devient illisible une fois qu'elle est arrivée.
        var steps = Cells(path);

        return new(MoveKind, actorId, actorName, steps, null, null, null, null, [], steps);
    }

    public static TacticalCombatEventDto Skill(
        Guid actorId,
        string actorName,
        string skillKey,
        string skillName,
        Domain.Combats.Tactical.GridPosition target,
        IReadOnlyList<TacticalImpactDto> impacts,
        IEnumerable<Domain.Combats.Tactical.GridPosition>? telegraphCells = null) =>
        new(SkillKind, actorId, actorName, [], skillKey, skillName, target.X, target.Y, impacts,
            telegraphCells is null ? [new TacticalStepDto(target.X, target.Y)] : Cells(telegraphCells));

    public static TacticalCombatEventDto Item(
        Guid actorId,
        string actorName,
        string itemKey,
        string itemName,
        Domain.Combats.Tactical.GridPosition target,
        IReadOnlyList<TacticalImpactDto> impacts,
        IEnumerable<Domain.Combats.Tactical.GridPosition>? telegraphCells = null) =>
        new(ItemKind, actorId, actorName, [], itemKey, itemName, target.X, target.Y, impacts,
            telegraphCells is null ? [new TacticalStepDto(target.X, target.Y)] : Cells(telegraphCells));

    /// <summary>
    /// A DoT/HoT tick landing on whoever's activation just began — no actor decision, no path,
    /// no telegraph, just what a status effect did to its owner on its own. <paramref
    /// name="impacts"/> carries one entry per periodic effect that dealt damage or healing this
    /// activation (several stacked DoTs can tick in the same one).
    /// </summary>
    public static TacticalCombatEventDto Tick(
        Guid combatantId,
        string combatantName,
        Domain.Combats.Tactical.GridPosition position,
        IReadOnlyList<TacticalImpactDto> impacts) =>
        new(TickKind, combatantId, combatantName, [], null, null, position.X, position.Y, impacts, null);
}
