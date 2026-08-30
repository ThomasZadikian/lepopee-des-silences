using Leds.GameEngine.Domain.Protocol;

namespace Leds.GameEngine.Infrastructure.Generation.RoomMaps.Hall;

/// <summary>
/// The Hall's two confirmed protocol rules — SFD Hall d'entrée §V's "Règles confirmées à ce
/// stade" table, verbatim: the tapis (essuyez vos pieds en y revenant) and the threshold of the
/// Pièce des émotions (ne pas s'approcher). The SFD itself says the exhaustive rule list "reste à
/// écrire dans la SFD des événements" — these two are the only ones canonically confirmed, so
/// this is the complete authored set, not a partial cut of a longer list.
/// <para>
/// Both are <see cref="LocalRuleConditionType.ZoneEntry"/>, not NpcInteraction: the Émotions
/// rule's "ne pas parler aux Émotions" half can now use the generic RoomNpc interaction path,
/// but remains unauthored until its exact NPC target is available; the "ne pas s'approcher"
/// half is covered by the threshold zone below.
/// </para>
/// <para>
/// Consequence ladders stay faithful to the SFD's own wording (attention → attitude/relocation →
/// explicit warning → surveillance → Veilleurs/agressivité) even though only NpcRelocate and
/// IncreasedSurveillance have runtime machinery today (see
/// <see cref="Application.Protocol.LocalRuleProtocolEvaluator"/>) — the higher rungs are still
/// correct DATA, just not yet consumed by anything that spawns a Veilleur or starts combat.
/// </para>
/// </summary>
public static class HallEntreeProtocol
{
    public const string TapisRuleKey = "hall.protocole.tapis";
    public const string EmotionsRuleKey = "hall.protocole.emotions";

    public static LocalRule TapisRule { get; } = LocalRule.Create(
        key: TapisRuleKey,
        name: "Essuyez vos pieds",
        conditionType: LocalRuleConditionType.ZoneEntry,
        infoMessage:
            "Le tapis bordeaux réclame qu'on s'essuie les pieds en y revenant — un usage que le " +
            "Majordome a rappelé dès l'accueil.",
        warningMessage:
            "Vous foulez de nouveau le tapis sans vous être essuyé les pieds. Le Majordome le remarque.",
        consequences:
        [
            LocalRuleConsequence.Create(1, LocalRuleConsequenceType.Look, "npc.majordome"),
            LocalRuleConsequence.Create(2, LocalRuleConsequenceType.NpcRelocate, "npc.majordome"),
            LocalRuleConsequence.Create(3, LocalRuleConsequenceType.Warning),
            LocalRuleConsequence.Create(4, LocalRuleConsequenceType.IncreasedSurveillance, "npc.veilleur-tapis"),
            LocalRuleConsequence.Create(5, LocalRuleConsequenceType.VeilleurApproach),
        ],
        conditionCells: HallEntreeLayout.TapisCells);

    public static LocalRule EmotionsRule { get; } = LocalRule.Create(
        key: EmotionsRuleKey,
        name: "Le seuil des émotions",
        conditionType: LocalRuleConditionType.ZoneEntry,
        infoMessage:
            "Le Majordome a prévenu : mieux vaut ne pas s'approcher de la Pièce des émotions.",
        warningMessage:
            "Vous franchissez de nouveau le seuil des émotions malgré l'avertissement.",
        consequences:
        [
            LocalRuleConsequence.Create(1, LocalRuleConsequenceType.Look, "npc.emotion#5"),
            LocalRuleConsequence.Create(2, LocalRuleConsequenceType.Warning),
            LocalRuleConsequence.Create(3, LocalRuleConsequenceType.IncreasedSurveillance, "npc.emotion#5"),
            LocalRuleConsequence.Create(4, LocalRuleConsequenceType.Combat),
        ],
        conditionCells: [(HallEntreeLayout.EmotionsThresholdX, HallEntreeLayout.EmotionsThresholdY)]);

    public static IReadOnlyList<LocalRule> Rules { get; } = [TapisRule, EmotionsRule];
}
