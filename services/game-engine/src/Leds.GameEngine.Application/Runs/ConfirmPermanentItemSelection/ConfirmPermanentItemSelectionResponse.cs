namespace Leds.GameEngine.Application.Runs.ConfirmPermanentItemSelection;

public sealed record ConfirmPermanentItemSelectionResponse(
    Guid RunId,
    IReadOnlyCollection<string> ConfirmedItemDefinitionKeys);
