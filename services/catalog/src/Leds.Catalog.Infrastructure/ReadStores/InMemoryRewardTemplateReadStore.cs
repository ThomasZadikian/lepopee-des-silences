using Leds.Catalog.Application.RewardTemplates.Dtos;
using Leds.Catalog.Application.RewardTemplates.Ports;

namespace Leds.Catalog.Infrastructure.ReadStores;

public sealed class InMemoryRewardTemplateReadStore : IRewardTemplateReadStore
{
    private readonly IReadOnlyDictionary<string, RewardTemplateDto> _templates =
        new Dictionary<string, RewardTemplateDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["reward.combat.default"] = Template("reward.combat.default", "Récompense de combat", "Récompense standard après un combat.", "Combat", [
                Option("Heal", "Soin léger", "Soin léger", null, null, 10),
                Option("Heal", "Souffle retrouvé", "Souffle retrouvé", null, null, 14),
                Option("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", 15)]),
            ["reward.combat.rare"] = Template("reward.combat.rare", "Récompense rare", "Récompense pour un combat rare.", "Rare", [
                Option("Heal", "Soin rare", "Soin rare", null, null, 15),
                Option("Heal", "Répit lucide", "Répit lucide", null, null, 20),
                Option("Heal", "Soin substantiel", "Soin substantiel", null, null, 25)]),
            ["reward.combat.elite"] = Template("reward.combat.elite", "Récompense élite", "Récompense pour un combat élite.", "Elite", [
                Option("Heal", "Soin important", "Soin important", null, null, 20),
                Option("Heal", "Volonté restaurée", "Volonté restaurée", null, null, 28),
                Option("Heal", "Suture mentale", "Suture mentale", null, null, 36)]),
            ["reward.combat.boss"] = Template("reward.combat.boss", "Récompense de boss", "Récompense pour avoir vaincu un boss.", "RoomBoss", [
                Option("Heal", "Soin majeur", "Soin majeur", null, null, 30),
                Option("Heal", "Souffle du Gardien", "Souffle du Gardien", null, null, 42),
                Option("Heal", "Silence recomposé", "Silence recomposé", null, null, 54)]),
            ["reward.item.default"] = Template("reward.item.default", "Récompense d'objet", "Récompense d'un noeud objet.", "NodeEvent", [
                Option("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", "Item", 8),
                Option("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", 15),
                Option("Heal", "Souffle du passé", "Souffle du passé", null, null, 10)]),
            ["reward.merchant.default"] = Template("reward.merchant.default", "Offre du marchand", "Récompense proposée par un marchand.", "NodeEvent", [
                Option("TemporaryItem", "Baume de mémoire", "Baume de mémoire", "item.consumable.minor-heal", "Item", 15),
                Option("TemporaryItem", "Éclat de garde", "Éclat de garde", "item.consumable.guard-shard", "Item", 8),
                Option("Heal", "Soin du marchand", "Soin du marchand", null, null, 12)])
        };

    public Task<RewardTemplateDto?> GetDtoByKeyAsync(string key, CancellationToken cancellationToken)
    {
        _templates.TryGetValue(key, out var template);
        return Task.FromResult(template);
    }

    public Task<IReadOnlyCollection<RewardTemplateDto>> ListEligibleDtosAsync(
        RewardTemplateEligibilityQueryContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.SourceType))
        {
            return Task.FromResult<IReadOnlyCollection<RewardTemplateDto>>([]);
        }

        var templates = _templates.Values
            .Where(template => string.Equals(template.SourceType, context.SourceType, StringComparison.Ordinal))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<RewardTemplateDto>>(templates);
    }

    private static RewardTemplateDto Template(
        string key,
        string displayName,
        string description,
        string sourceType,
        IReadOnlyCollection<RewardTemplateOptionDto> options)
    {
        return new RewardTemplateDto(Guid.NewGuid(), key, "1.0", displayName, description, sourceType, 2, 3, "Active", options);
    }

    private static RewardTemplateOptionDto Option(
        string rewardType,
        string label,
        string description,
        string? payloadKey,
        string? payloadType,
        int baseAmount)
    {
        return new RewardTemplateOptionDto(rewardType, label, description, payloadKey, payloadType, null, baseAmount, "Flat", 1);
    }
}
