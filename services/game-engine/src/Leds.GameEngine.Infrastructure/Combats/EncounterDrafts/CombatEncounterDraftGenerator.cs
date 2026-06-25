using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

public sealed class CombatEncounterDraftGenerator : ICombatEncounterDraftGenerator
{
    private const string PlayerAllyKey = "player.self";
    private const string PlayerDisplayName = "Le Joueur";
    private const string PlayerRole = "Protagonist";
    private static readonly IReadOnlyCollection<string> PlayerTags = new[] { "player", "protagonist" };

    private readonly ICatalogContentGateway _catalogContentGateway;
    private readonly IEncounterCompositionPolicy _compositionPolicy;
    private readonly ICombatRiskProfileResolver _riskProfileResolver;

    public CombatEncounterDraftGenerator(
        ICatalogContentGateway catalogContentGateway,
        IEncounterCompositionPolicy compositionPolicy,
        ICombatRiskProfileResolver riskProfileResolver)
    {
        _catalogContentGateway = catalogContentGateway;
        _compositionPolicy = compositionPolicy;
        _riskProfileResolver = riskProfileResolver;
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

        var compositionContext = new EncounterCompositionContext(
            RoomType: context.RoomType,
            RoomIndex: context.RoomIndex,
            RiskLevel: context.RiskLevel,
            EncounterType: context.EncounterType,
            AvailableEnemies: compatibleEnemies,
            NodeDepth: context.NodeDepth,
            ActivePalaceLaws: context.ActivePalaceLaws,
            PalaceRoomState: context.PalaceRoomState,
            RoomClimate: context.RoomClimate);

        var compositionResult = _compositionPolicy.Compose(compositionContext);

        var selected = compositionResult.SelectedEnemies;

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

        var allies = context.PartyAllies is { Count: > 0 }
            ? context.PartyAllies.ToArray()
            : new[]
            {
                new CombatEncounterDraftAlly(
                    AllyKey: PlayerAllyKey,
                    DisplayName: PlayerDisplayName,
                    Role: PlayerRole,
                    Tags: PlayerTags,
                    IsProtagonist: true)
            };

        var riskProfile = _riskProfileResolver.Resolve(
            Enum.Parse<Leds.GameEngine.Domain.Nodes.NodeEventType>(context.EncounterType),
            context.RiskLevel);

        return new CombatEncounterDraft(
            RunId: context.RunId,
            RoomId: context.RoomId,
            NodeId: context.NodeId,
            RoomType: context.RoomType,
            RoomIndex: context.RoomIndex,
            RiskLevel: context.RiskLevel,
            EncounterType: context.EncounterType,
            Enemies: enemies,
            Allies: allies,
            DifficultyMultiplier: riskProfile.DifficultyMultiplier * EnemyStatScaler.DepthMultiplier(context.RoomIndex));
    }
}