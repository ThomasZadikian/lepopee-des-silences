namespace Leds.GameEngine.Application.Combats.Dtos;

/// <summary>Ce qu'un combattant a encaissé, à l'endroit où il l'a encaissé.</summary>
public sealed record TacticalImpactDto(
    Guid CombatantId,
    int X,
    int Y,
    /// <summary>Vitalité perdue. Négative pour un soin — la même case sert aux deux.</summary>
    int VitalityDelta,
    bool Defeated);

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
    /// <summary>« Move » ou « Skill ».</summary>
    string Kind,
    Guid ActorId,
    string ActorName,
    /// <summary>Le trajet, case par case. Vide pour une action.</summary>
    IReadOnlyList<TacticalStepDto> Path,
    string? SkillKey,
    string? SkillName,
    IReadOnlyList<TacticalImpactDto> Impacts)
{
    public const string MoveKind = "Move";
    public const string SkillKind = "Skill";

    public static TacticalCombatEventDto Move(
        Guid actorId, string actorName, IEnumerable<Domain.Combats.Tactical.GridPosition> path) =>
        new(MoveKind, actorId, actorName,
            [.. path.Select(p => new TacticalStepDto(p.X, p.Y))], null, null, []);

    public static TacticalCombatEventDto Skill(
        Guid actorId,
        string actorName,
        string skillKey,
        string skillName,
        IReadOnlyList<TacticalImpactDto> impacts) =>
        new(SkillKind, actorId, actorName, [], skillKey, skillName, impacts);
}
