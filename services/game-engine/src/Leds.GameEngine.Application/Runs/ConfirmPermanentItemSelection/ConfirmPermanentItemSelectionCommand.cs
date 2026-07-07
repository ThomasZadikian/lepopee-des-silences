using MediatR;

namespace Leds.GameEngine.Application.Runs.ConfirmPermanentItemSelection;

public sealed record ConfirmPermanentItemSelectionCommand(Guid RunId, IReadOnlyCollection<string> ItemDefinitionKeys)
    : IRequest<ConfirmPermanentItemSelectionResponse>;
