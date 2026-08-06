using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterComposition;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Typing;

namespace Leds.GameEngine.Infrastructure.Combats.EncounterDrafts;

public sealed class CombatEncounterDraftGenerator : ICombatEncounterDraftGenerator
{
    private const string PlayerAllyKey = "player.self";
    private const string PlayerDisplayName = "Le Joueur";
    private const string PlayerRole = "Protagonist";
    private static readonly IReadOnlyCollection<string> PlayerTags = new[] { "player", "protagonist" };

    // BALANCE KNOB — stat bonus applied ONLY to the Elite/Rare "preferred" enemy pick
    // (EncounterCompositionResult.PreferredEnemyKey), multiplicatively on top of the base
    // per-tier DifficultyMultiplier every enemy already gets. An Elite's escort (if any) —
    // always a strictly weaker enemy, see EncounterCompositionPolicy.SelectEliteEnemies —
    // never receives this bonus, only the preferred pick does.
    private const double EliteStatMultiplier = 1.5;
    private const double RareStatMultiplier = 1.25;

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
            RoomClimate: context.RoomClimate,
            RoomKey: context.RoomKey);

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
            .Select(e =>
            {
                // Only the Elite/Rare "preferred" pick gets a stat bonus — never its escort.
                var statMultiplier = e.Key == compositionResult.PreferredEnemyKey
                    ? context.EncounterType switch
                    {
                        "Elite" => EliteStatMultiplier,
                        "Rare" => RareStatMultiplier,
                        _ => 1.0,
                    }
                    : 1.0;

                return new CombatEncounterDraftEnemy(
                    EnemyKey: e.Key,
                    DisplayName: e.DisplayName,
                    Description: e.Description,
                    Archetype: e.Archetype,
                    BaseDifficulty: e.BaseDifficulty,
                    MinRiskLevel: e.MinRiskLevel,
                    MaxRiskLevel: e.MaxRiskLevel,
                    Tags: e.Tags,
                    SkillKeys: e.SkillKeys,
                    AttackPower: ScaleStat(e.AttackPower, statMultiplier),
                    Defense: ScaleStat(e.Defense, statMultiplier),
                    Speed: e.Speed,
                    Focus: e.Focus,
                    MagicAttack: ScaleStat(e.MagicAttack, statMultiplier),
                    MagicDefense: ScaleStat(e.MagicDefense, statMultiplier),
                    Mana: e.Mana,
                    Movement: e.Movement,
                    EmotionalRegister: EmotionalTypeCode.ParseRequired(
                        e.Registre,
                        $"Enemy '{e.Key}' Registre").ToString(),
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
                            Tags: s.Tags,
                            Category: s.Category,
                            BasePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                            TacticalRange: s.TacticalRange,
                            TacticalAreaShape: s.TacticalAreaShape,
                            RequiresLineOfSight: s.RequiresLineOfSight,
                            Cooldown: s.Cooldown,
                            IsUltimate: s.IsUltimate,
                            EmotionalRegister: s.EmotionalRegister))
                        .ToArray());
            })
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
            DifficultyMultiplier: riskProfile.DifficultyMultiplier,
            RoomKey: context.RoomKey);
    }

    private static int ScaleStat(int baseValue, double multiplier) =>
        multiplier == 1.0 ? baseValue : (int)Math.Ceiling(baseValue * multiplier);
}
