using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats.EncounterDrafts;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

public sealed class CombatEncounterDraftGenerator : ICombatEncounterDraftGenerator
{
    private const string PlayerAllyKey = "player.self";
    private const string PlayerDisplayName = "Le Joueur";
    private const string PlayerRole = "Protagonist";
    private static readonly IReadOnlyCollection<string> PlayerTags = new[] { "player", "protagonist" };

    private static readonly IReadOnlyCollection<string> EmptyTags = Array.Empty<string>();

    private readonly ICatalogContentGateway _catalogContentGateway;

    public CombatEncounterDraftGenerator(ICatalogContentGateway catalogContentGateway)
    {
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<CombatEncounterDraft> GenerateAsync(
        CombatEncounterDraftContext context,
        CancellationToken cancellationToken = default)
    {
        var compatibleEnemies = await _catalogContentGateway
            .ListCompatibleEnemyDefinitionsAsync(
                context.RoomType,
                context.RiskLevel,
                cancellationToken);

        var ordered = compatibleEnemies
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .ToArray();

        var selected = SelectEnemies(ordered, context.EncounterType, context.EnemyCount);

        if (selected.Length == 0)
        {
            throw new InvalidOperationException(
                $"No compatible enemy definitions found for RoomType='{context.RoomType}', " +
                $"RiskLevel={context.RiskLevel}, EncounterType='{context.EncounterType}'. " +
                "Cannot generate a combat encounter draft without enemies.");
        }

        var skillKeys = selected
            .SelectMany(e => e.SkillKeys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var skillDefinitions = await _catalogContentGateway
            .ListSkillDefinitionsByKeysAsync(skillKeys, cancellationToken);

        var skillLookup = skillDefinitions
            .ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

        var missingKeys = skillKeys
            .Where(k => !skillLookup.ContainsKey(k))
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Missing skill definitions for keys: {string.Join(", ", missingKeys)}");
        }

        var enemies = selected
            .Select(e => new CombatEncounterDraftEnemy(
                EnemyKey: e.Key,
                DisplayName: e.DisplayName,
                Description: e.Description,
                Archetype: e.Archetype,
                BaseDifficulty: e.BaseDifficulty,
                MinRiskLevel: e.MinRiskLevel,
                MaxRiskLevel: e.MaxRiskLevel,
                Tags: e.Tags,
                SkillKeys: e.SkillKeys,
                Skills: e.SkillKeys
                    .Select(sk => skillLookup.GetValueOrDefault(sk))
                    .Where(s => s is not null)
                    .Select(s => new CombatEncounterDraftSkill(
                        Key: s!.Key,
                        DisplayName: s.DisplayName,
                        Description: s.Description,
                        SkillType: s.SkillType,
                        TargetingType: s.TargetingType,
                        EffectType: s.EffectType,
                        ManaCost: s.ManaCost,
                        ChargeCost: s.ChargeCost,
                        BasePower: s.BasePower,
                        Tags: s.Tags))
                    .ToArray()))
            .ToArray();

        var allies = new[]
        {
            new CombatEncounterDraftAlly(
                AllyKey: PlayerAllyKey,
                DisplayName: PlayerDisplayName,
                Role: PlayerRole,
                Tags: PlayerTags)
        };

        return new CombatEncounterDraft(
            RunId: context.RunId,
            RoomId: context.RoomId,
            NodeId: context.NodeId,
            RoomType: context.RoomType,
            RoomIndex: context.RoomIndex,
            RiskLevel: context.RiskLevel,
            EncounterType: context.EncounterType,
            Enemies: enemies,
            Allies: allies);
    }

    private static CatalogEnemyDefinition[] SelectEnemies(
        CatalogEnemyDefinition[] ordered,
        string encounterType,
        int requestedCount)
    {
        if (ordered.Length == 0)
        {
            return [];
        }

        if (encounterType == "Elite")
        {
            var elite = ordered.FirstOrDefault(e =>
                e.Tags.Contains("elite", StringComparer.OrdinalIgnoreCase));

            if (elite is not null)
            {
                return [elite];
            }

            return [ordered[^1]];
        }

        if (encounterType == "Rare")
        {
            return [ordered[^1]];
        }

        var count = Math.Min(requestedCount, ordered.Length);
        return ordered[..count];
    }
}
