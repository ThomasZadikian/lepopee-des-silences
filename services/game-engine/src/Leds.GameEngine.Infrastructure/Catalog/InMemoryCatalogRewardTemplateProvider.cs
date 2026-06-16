using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;

namespace Leds.GameEngine.Infrastructure.Catalog;

public sealed class InMemoryCatalogRewardTemplateProvider : ICatalogRewardTemplateProvider
{
    private readonly Dictionary<string, CatalogRewardTemplateSnapshot> _templates = new()
    {
        ["reward.combat.default"] = new CatalogRewardTemplateSnapshot(
            "reward.combat.default",
            "1.0",
            "Récompense de combat",
            "Récompense standard après un combat.",
            "Combat",
            2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin léger", "Soin léger", null, null, null, 10, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Souffle retrouvé", "Souffle retrouvé", null, null, null, 14, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", null, 15, "Flat", 1),
            ]),
        ["reward.combat.rare"] = new CatalogRewardTemplateSnapshot(
            "reward.combat.rare",
            "1.0",
            "Récompense rare",
            "Récompense pour un combat rare.",
            "Rare",
            2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin rare", "Soin rare", null, null, null, 15, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Répit lucide", "Répit lucide", null, null, null, 20, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin substantiel", "Soin substantiel", null, null, null, 25, "Flat", 1),
            ]),
        ["reward.combat.elite"] = new CatalogRewardTemplateSnapshot(
            "reward.combat.elite",
            "1.0",
            "Récompense élite",
            "Récompense pour un combat élite.",
            "Elite",
            2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin important", "Soin important", null, null, null, 20, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Volonté restaurée", "Volonté restaurée", null, null, null, 28, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Suture mentale", "Suture mentale", null, null, null, 36, "Flat", 1),
            ]),
        ["reward.combat.boss"] = new CatalogRewardTemplateSnapshot(
            "reward.combat.boss",
            "1.0",
            "Récompense de boss",
            "Récompense pour avoir vaincu un boss.",
            "RoomBoss",
            2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin majeur", "Soin majeur", null, null, null, 30, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Souffle du Gardien", "Souffle du Gardien", null, null, null, 42, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Silence recomposé", "Silence recomposé", null, null, null, 54, "Flat", 1),
            ]),
        ["reward.item.default"] = new CatalogRewardTemplateSnapshot(
            "reward.item.default",
            "1.0",
            "Récompense d'objet",
            "Récompense d'un noeud objet.",
            "NodeEvent",
            2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", "Item", null, 8, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", null, 15, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Souffle du passé", "Souffle du passé", null, null, null, 10, "Flat", 1),
            ]),
        ["reward.merchant.default"] = new CatalogRewardTemplateSnapshot(
            "reward.merchant.default",
            "1.0",
            "Offre du marchand",
            "Récompense proposée par un marchand.",
            "NodeEvent",
            2, 3,
            [
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", null, 15, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", "Item", null, 8, "Flat", 1),
                new CatalogRewardTemplateOptionSnapshot("Heal", "Soin du marchand", "Soin du marchand", null, null, null, 12, "Flat", 1),
            ]),
    };

    public void Register(CatalogRewardTemplateSnapshot template)
    {
        _templates[template.Key] = template;
    }

    public Task<CatalogRewardTemplateSnapshot?> GetRewardTemplateAsync(
        string templateKey,
        CancellationToken cancellationToken = default)
    {
        _templates.TryGetValue(templateKey, out var result);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<CatalogRewardTemplateSnapshot>> ListEligibleRewardTemplatesAsync(
        RewardTemplateEligibilityContext context,
        CancellationToken cancellationToken = default)
    {
        var eligible = _templates.Values
            .Where(t => t.SourceType == context.SourceType)
            .ToList()
            .AsReadOnly();

        return Task.FromResult((IReadOnlyCollection<CatalogRewardTemplateSnapshot>)eligible);
    }
}
