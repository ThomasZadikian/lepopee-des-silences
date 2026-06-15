using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.Interlude.Dtos;

/// <summary>
/// Full interlude payload returned by EnterInterlude and GetInterlude.
/// Contains the interlude hub nodes and a minimal run summary.
/// </summary>
public sealed record InterludeDto(
    Guid RunId,
    int CurrentRoomIndex,
    int DisplayRoomNumber,
    IReadOnlyCollection<InterludeNodeDto> Nodes,
    IReadOnlyCollection<InterludeActionDto> AvailableActions,
    RunSummaryDto RunSummary)
{
    public static InterludeDto FromDomain(
        Run run,
        IReadOnlyCollection<InterludeNodeDto> nodes,
        IReadOnlyCollection<InterludeActionDto> availableActions) =>
        new(
            run.Id.Value,
            run.CurrentRoomIndex,
            run.CurrentRoomIndex + 1,
            nodes,
            availableActions,
            RunSummaryDto.FromDomain(run));

    /// <summary>
    /// Standard available actions for the interlude screen.
    /// Includes: enter next room, save and exit, and abandon run.
    /// </summary>
    public static IReadOnlyCollection<InterludeActionDto> BuildDefaultActions() =>
    [
        new InterludeActionDto(
            Key: "enter-next-room",
            Label: "Reprendre la descente",
            Description: "Entrer dans la prochaine salle du Palais.",
            RequiresConfirmation: false,
            IsDangerous: false,
            IsEnabled: true),
        new InterludeActionDto(
            Key: "save-and-exit",
            Label: "Sauvegarder et quitter",
            Description: "Sauvegarder la progression et retourner au menu principal. La run peut être reprise plus tard.",
            RequiresConfirmation: false,
            IsDangerous: false,
            IsEnabled: true),
        new InterludeActionDto(
            Key: "abandon-run",
            Label: "Abandonner la run",
            Description: "Mettre fin définitivement à cette run. Cette action est irréversible.",
            RequiresConfirmation: true,
            IsDangerous: true,
            IsEnabled: true),
    ];
}