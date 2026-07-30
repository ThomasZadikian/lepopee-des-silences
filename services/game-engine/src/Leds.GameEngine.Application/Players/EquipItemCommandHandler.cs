using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Application.Players.Ports;
using MediatR;

namespace Leds.GameEngine.Application.Players;

public sealed class EquipItemCommandHandler : IRequestHandler<EquipItemCommand, PlayerProfileView>
{
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly ICatalogContentGateway _catalog;

    public EquipItemCommandHandler(
        IPlayerProfileGateway playerProfileGateway,
        ICatalogContentGateway catalog)
    {
        _playerProfileGateway = playerProfileGateway;
        _catalog = catalog;
    }

    public async Task<PlayerProfileView> Handle(
        EquipItemCommand request,
        CancellationToken cancellationToken)
    {
        var definition = await _catalog.GetItemDefinitionByKeyAsync(
            request.ItemKey, cancellationToken);
        if (definition.IsFailure)
            throw new DomainException($"Unknown equipment item '{request.ItemKey}'.");

        var slot = definition.Value.ItemType switch
        {
            "Weapon" => "Weapon",
            "Accessory" => "Accessory",
            "Relic" or "Heritage" => "Relic",
            _ when string.Equals(definition.Value.Category, "Relic", StringComparison.OrdinalIgnoreCase)
                => "Relic",
            _ => throw new DomainException(
                $"Item '{request.ItemKey}' is not an equippable weapon, accessory or relic.")
        };

        return await _playerProfileGateway.EquipItemAsync(
            request.PlayerId,
            request.CharacterId,
            request.ItemKey,
            slot,
            cancellationToken);
    }
}
