using Leds.GameEngine.Domain.Interlude;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Interlude;

/// <summary>
/// Deterministic provider that always returns the six standard interlude nodes
/// regardless of run state.
///
/// Node layout (positionSlot):
/// <code>
///          [top = Journal]
///  [left = Elise]  [center = Player]  [right = Inventory]
///       [bottom-left]        [bottom-right]
///           (Placeholder)      (Placeholder)
/// </code>
/// </summary>
public sealed class DefaultInterludeNodeProvider : IInterludeNodeProvider
{
    public IReadOnlyCollection<InterludeNode> GetNodes(Run run)
    {
        return
        [
            InterludeNode.Create(
                type:         InterludeNodeType.Player,
                label:        "Aventurier",
                description:  "Consultez votre fiche de personnage — statistiques, état actuel et mémoires collectées.",
                positionSlot: "center",
                isEnabled:    true,
                actionKey:    InterludeActionKey.ViewPlayer),

            InterludeNode.Create(
                type:         InterludeNodeType.Journal,
                label:        "Journal de Descente",
                description:  "Retracez les salles traversées, les lois du Palais actives et les événements marquants.",
                positionSlot: "top",
                isEnabled:    true,
                actionKey:    InterludeActionKey.OpenJournal),

            InterludeNode.Create(
                type:         InterludeNodeType.Elise,
                label:        "Elise",
                description:  "Votre compagne de descente observe le Palais. Elle a peut-être quelque chose à vous dire.",
                positionSlot: "left",
                isEnabled:    true,
                actionKey:    InterludeActionKey.TalkToElise),

            InterludeNode.Create(
                type:         InterludeNodeType.Inventory,
                label:        "Sac à dos",
                description:  "Parcourez vos objets collectés et préparez-vous pour la prochaine salle.",
                positionSlot: "right",
                isEnabled:    true,
                actionKey:    InterludeActionKey.OpenInventory),

            InterludeNode.Create(
                type:         InterludeNodeType.Placeholder,
                label:        "—",
                description:  "Emplacement réservé. Une nouvelle option de Repli apparaîtra ici.",
                positionSlot: "bottom-left",
                isEnabled:    false,
                actionKey:    InterludeActionKey.Placeholder),

            InterludeNode.Create(
                type:         InterludeNodeType.Placeholder,
                label:        "—",
                description:  "Emplacement réservé. Une nouvelle option de Repli apparaîtra ici.",
                positionSlot: "bottom-right",
                isEnabled:    false,
                actionKey:    InterludeActionKey.Placeholder),
        ];
    }
}
