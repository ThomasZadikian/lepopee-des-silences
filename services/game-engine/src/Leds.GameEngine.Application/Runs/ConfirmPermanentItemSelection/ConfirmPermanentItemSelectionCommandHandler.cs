using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.ConfirmPermanentItemSelection;

public sealed class ConfirmPermanentItemSelectionCommandHandler
    : IRequestHandler<ConfirmPermanentItemSelectionCommand, ConfirmPermanentItemSelectionResponse>
{
    private readonly IRunRepository _runRepository;
    private readonly ICatalogContentGateway _catalogGateway;
    private readonly IPlayerProfileGateway _playerProfileGateway;

    public ConfirmPermanentItemSelectionCommandHandler(
        IRunRepository runRepository,
        ICatalogContentGateway catalogGateway,
        IPlayerProfileGateway playerProfileGateway)
    {
        _runRepository = runRepository;
        _catalogGateway = catalogGateway;
        _playerProfileGateway = playerProfileGateway;
    }

    public async Task<ConfirmPermanentItemSelectionResponse> Handle(
        ConfirmPermanentItemSelectionCommand request,
        CancellationToken cancellationToken)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(request.RunId), cancellationToken)
            ?? throw new NotFoundException("Run", request.RunId);

        var requestedKeys = request.ItemDefinitionKeys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedKeys.Length == 0)
        {
            return new ConfirmPermanentItemSelectionResponse(run.Id.Value, []);
        }

        var runItemKeys = new HashSet<string>(
            run.RunItems.Select(item => item.DefinitionKey),
            StringComparer.OrdinalIgnoreCase);

        foreach (var key in requestedKeys)
        {
            if (!runItemKeys.Contains(key))
            {
                throw new DomainException($"L'objet '{key}' n'a pas été obtenu durant cette run.");
            }

            var result = await _catalogGateway.GetItemDefinitionByKeyAsync(key, cancellationToken);
            if (!result.IsSuccess || !result.Value.IsPermanentEligible)
            {
                throw new DomainException($"L'objet '{key}' n'est pas éligible au sac permanent.");
            }
        }

        await _playerProfileGateway.AddPermanentItemsAsync(
            run.PlayerId, requestedKeys, run.Id.Value, cancellationToken);

        // Carries a container's poured-in liquid over into the permanent backpack — the
        // permanent item itself was just added above, this only sets its mutable content.
        foreach (var key in requestedKeys)
        {
            var runItem = run.RunItems.FirstOrDefault(item =>
                string.Equals(item.DefinitionKey, key, StringComparison.OrdinalIgnoreCase)
                && item.IsContainer
                && item.ContainedLiquidDefinitionKey is not null);

            if (runItem is not null)
            {
                await _playerProfileGateway.SetPermanentItemContentAsync(
                    run.PlayerId, key, runItem.ContainedLiquidDefinitionKey!, cancellationToken);
            }
        }

        return new ConfirmPermanentItemSelectionResponse(run.Id.Value, requestedKeys);
    }
}
