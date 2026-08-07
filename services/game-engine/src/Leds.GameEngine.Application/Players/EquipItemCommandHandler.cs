using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Application.Catalog;
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

        // Derived from the same catalog-authored (itemType, category) pair — and the same
        // CatalogRunItemMapper — that resolves every other item classification in the
        // runtime, rather than re-deriving an independent notion of "what kind of item is
        // this" here. Grimoire/WeatherInstrument/SkillEssence share the Relic slot: none
        // of the three has a dedicated equipment slot of its own.
        var runItemType = CatalogRunItemMapper.MapType(definition.Value.ItemType, definition.Value.Category);
        var slot = runItemType switch
        {
            RunItemType.Weapon => "Weapon",
            RunItemType.Equipment => "Accessory",
            RunItemType.Relic or RunItemType.Grimoire or RunItemType.WeatherInstrument or RunItemType.SkillEssence
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
