using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Combats.Typing;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Common;
using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed class StartRunCommandHandler : IRequestHandler<StartRunCommand, StartRunResponse>
{
    private readonly IRunGenerator _runGenerator;
    private readonly IRunRepository _runRepository;
    private readonly IPlayerRunSnapshotGateway _playerGateway;
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly IAmbientPalaceLawPromulgator _palaceLawPromulgator;
    private readonly PlayerSkillMerger _skillMerger;
    private readonly PlayerStatMerger _statMerger;
    private readonly IClock _clock;
    private readonly ICatalogContentGateway _catalogContentGateway;

    public StartRunCommandHandler(
        IRunGenerator runGenerator,
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        IPlayerProfileGateway playerProfileGateway,
        IAmbientPalaceLawPromulgator palaceLawPromulgator,
        PlayerSkillMerger skillMerger,
        PlayerStatMerger statMerger,
        IClock clock,
        ICatalogContentGateway catalogContentGateway)
    {
        _runGenerator = runGenerator;
        _runRepository = runRepository;
        _playerGateway = playerGateway;
        _playerProfileGateway = playerProfileGateway;
        _palaceLawPromulgator = palaceLawPromulgator;
        _skillMerger = skillMerger;
        _statMerger = statMerger;
        _clock = clock;
        _catalogContentGateway = catalogContentGateway;
    }

    public async Task<StartRunResponse> Handle(
        StartRunCommand request,
        CancellationToken cancellationToken)
    {
        if (await _runRepository.HasActiveOrSuspendedAsync(request.PlayerId, cancellationToken))
        {
            throw new DomainException(
                "The account already has an active or suspended run.");
        }

        var snapshot = await _playerGateway.GetRunSnapshotAsync(request.PlayerId, cancellationToken);

        var profile = await _playerProfileGateway.GetProfileAsync(request.PlayerId, cancellationToken);
        var permanentItemKeys = (profile.PermanentItems ?? [])
            .Select(item => item.ItemDefinitionKey)
            .ToArray();
        var permanentItemDefinitions = await _skillMerger.ResolveEquippedItemsAsync(
            permanentItemKeys, cancellationToken);
        var permanentBehaviors = permanentItemDefinitions
            .SelectMany(item => item.EquipmentEffects ?? [])
            .Where(effect => string.Equals(effect.Kind, "RuntimeBehavior", StringComparison.OrdinalIgnoreCase))
            .Select(effect => effect.BehaviorCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var journalEnabled = permanentBehaviors.Contains("run-journal");
        var lawDenialEnabled = permanentBehaviors.Contains("deny-palace-law");
        var reputationGainBonusPercent = permanentBehaviors.Contains("reputation-gain-plus-ten") ? 10 : 0;
        var himLitProtectionEnabled = permanentBehaviors.Contains("himlit-protection");
        var caliceInfiniEnabled = permanentBehaviors.Contains("infinite-chalice");

        var seed = _runGenerator.GenerateSeed();
        var initialRoom = await _runGenerator.GenerateInitialRoomAsync(seed, cancellationToken);
        var emotionalAffinityMatrix = ToDomainMatrix(
            await _catalogContentGateway.GetEmotionalAffinityMatrixAsync(cancellationToken));
        var characterDefinitions = (await _catalogContentGateway
                .ListCharacterCombatDefinitionsAsync(cancellationToken))
            .ToDictionary(definition => definition.DefinitionKey, StringComparer.OrdinalIgnoreCase);

        var mainCharacter = snapshot.Characters.FirstOrDefault()
            ?? throw new InvalidOperationException("Player snapshot has no available characters.");

        var equippedDefinitions = await _skillMerger.ResolveEquippedItemsAsync(
            mainCharacter.EquippedItems, cancellationToken);
        var equipmentEffects = equippedDefinitions
            .SelectMany(item => (item.EquipmentEffects ?? [])
                .Select(effect => effect with { SourceDefinitionKey = item.Key }))
            .ToArray();
        var equippedWeapon = equippedDefinitions.FirstOrDefault(item =>
            string.Equals(item.Category, "Weapon", StringComparison.OrdinalIgnoreCase));

        var effectiveStats = _statMerger.ComputeEffectiveStats(mainCharacter.Stats, equipmentEffects);

        var typedDamageReductions = equipmentEffects
            .Where(e => string.Equals(e.Kind, "DamageReductionByType", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(e.AffinityRegister)
                && e.Amount is not null)
            .GroupBy(e => e.AffinityRegister!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => Math.Min(100, g.Sum(e => e.Amount!.Value)), StringComparer.OrdinalIgnoreCase);

        var hitChanceBonusPercent = equipmentEffects
            .Where(e => string.Equals(e.Kind, "HitChanceBonus", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value);

        var dotDurationReductionPercent = Math.Min(100, equipmentEffects
            .Where(e => string.Equals(e.Kind, "DotDurationReduction", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value));

        var dotDamageReductionPercent = Math.Min(100, equipmentEffects
            .Where(e => string.Equals(e.Kind, "DotDamageReduction", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value));

        var dotDamageBonusPercent = equipmentEffects
            .Where(e => string.Equals(e.Kind, "DotDamageBonusPercent", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value);

        var magicDamageBonusPercent = equipmentEffects
            .Where(e => string.Equals(e.Kind, "MagicDamageBonusPercent", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value);

        var magicDamageReductionPercent = Math.Min(100, equipmentEffects
            .Where(e => string.Equals(e.Kind, "MagicDamageReductionPercent", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value));

        var criticalChanceBonusPercent = equipmentEffects
            .Where(e => string.Equals(e.Kind, "CriticalChanceBonusPercent", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value);

        // e.g. Majordome's legendary "La tasse du majordome": +15% to all healing effects.
        var healingBonusPercent = equipmentEffects
            .Where(e => string.Equals(e.Kind, "HealingBonusPercent", StringComparison.OrdinalIgnoreCase)
                && e.Amount is not null)
            .Sum(e => e.Amount!.Value);

        var mergedSkills = await _skillMerger.MergeSkillsAsync(
            mainCharacter,
            equipmentEffects,
            cancellationToken,
            equippedWeapon);

        var playerSkills = mergedSkills
            .Select(s => PlayerRuntimeSkill.Create(
                key: s.Key,
                displayName: s.DisplayName,
                skillType: s.SkillType,
                targetingType: s.TargetingType,
                effectType: s.EffectType,
                manaCost: s.ManaCost,
                chargeCost: s.ChargeCost,
                basePower: s.BasePower,
                category: s.Category,
                basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                tacticalRange: s.TacticalRange,
                tacticalAreaShape: s.TacticalAreaShape,
                requiresLineOfSight: s.RequiresLineOfSight,
                cooldown: s.Cooldown,
                isUltimate: s.IsUltimate,
                emotionalRegister: s.EmotionalRegister))
            .ToArray();

        var run = Run.StartNew(
            request.PlayerId,
            seed,
            _runGenerator.GeneratorVersion,
            _runGenerator.MarkovMatrixVersion,
            initialRoom,
            _clock.UtcNow,
            maxHp: effectiveStats.MaxVitality,
            currentHp: effectiveStats.MaxVitality,
            attack: effectiveStats.AttackPower,
            defense: effectiveStats.Defense,
            speed: effectiveStats.Speed,
            focus: effectiveStats.Focus,
            magicAttack: effectiveStats.MagicAttack,
            magicDefense: effectiveStats.MagicDefense,
            mana: effectiveStats.Mana,
            maxMana: effectiveStats.Mana,
            charge: effectiveStats.Charge,
            playerSkills: playerSkills,
            runItemCapacity: effectiveStats.RunItemCapacity,
            typedDamageReductions: typedDamageReductions,
            hitChanceBonusPercent: hitChanceBonusPercent,
            dotDurationReductionPercent: dotDurationReductionPercent,
            dotDamageReductionPercent: dotDamageReductionPercent,
            dotDamageBonusPercent: dotDamageBonusPercent,
            magicDamageBonusPercent: magicDamageBonusPercent,
            magicDamageReductionPercent: magicDamageReductionPercent,
            criticalChanceBonusPercent: criticalChanceBonusPercent,
            guardBonusPercent: effectiveStats.GuardBonusPercent,
            journalEnabled: journalEnabled,
            lawDenialEnabled: lawDenialEnabled,
            reputationGainBonusPercent: reputationGainBonusPercent,
            himLitProtectionEnabled: himLitProtectionEnabled,
            healingBonusPercent: healingBonusPercent,
            caliceInfiniEnabled: caliceInfiniEnabled,
            emotionalAffinityMatrix: emotionalAffinityMatrix);

        foreach (var effect in equipmentEffects.Where(effect =>
                     string.Equals(effect.Kind, "GrantAffinity", StringComparison.OrdinalIgnoreCase)))
        {
            var register = EmotionalTypeCode.ParseRequired(
                effect.AffinityRegister,
                "GrantAffinity equipment effect AffinityRegister");
            run.AddRunModifier(RunModifier.Create(
                RunModifierType.AttackTypeOverride,
                (int)register,
                RunModifierDuration.UntilRunEnds,
                "Equipment",
                effect.SourceDefinitionKey ?? "equipped-item"));
        }

        // Ambient promulgation ("irréfusabilité") also applies to the very first room of
        // the run: without this, MoveToNextRoomCommandHandler's guaranteed first-floor
        // draw would never fire before the player leaves the Hall (StartRun never used to
        // call the promulgator at all).
        await _palaceLawPromulgator.PromulgateForRoomTransitionAsync(run, run.CurrentRoom, cancellationToken);

        // Plafond d'équipe (SFD v2, §5) : le porteur et au plus trois compagnons. Le roster
        // permanent n'est pas plafonné — le joueur recrute autant qu'il veut — mais seuls les
        // MaxPartySize premiers partent en run. L'ordre du roster fait foi, et le personnage
        // principal en est la tête (cf. snapshot.Characters.FirstOrDefault() plus haut).
        var characterSnapshots = new List<RunCharacterSnapshot>();
        foreach (var character in snapshot.Characters.Take(Run.MaxPartySize))
        {
            if (!characterDefinitions.TryGetValue(character.DefinitionKey, out var characterDefinition))
            {
                throw new InvalidOperationException(
                    $"Catalog has no combat definition for player character '{character.DefinitionKey}'.");
            }

            var definitions = await _skillMerger.ResolveEquippedItemsAsync(
                character.EquippedItems, cancellationToken);
            var effects = definitions.SelectMany(item => item.EquipmentEffects ?? []).ToArray();
            var effective = _statMerger.ComputeEffectiveStats(character.Stats, effects);
            var weapon = definitions.FirstOrDefault(item =>
                string.Equals(item.Category, "Weapon", StringComparison.OrdinalIgnoreCase));
            var skills = await _skillMerger.MergeSkillsAsync(
                character, effects, cancellationToken, weapon);

            var statSnapshot = RunCharacterStatSnapshot.Create(
                maxVitality: effective.MaxVitality,
                attackPower: effective.AttackPower,
                defense: effective.Defense,
                startingGuard: character.Stats.StartingGuard,
                speed: effective.Speed,
                initiative: character.Stats.Initiative,
                focus: effective.Focus,
                mana: effective.Mana,
                charge: effective.Charge,
                magicAttack: effective.MagicAttack,
                magicDefense: effective.MagicDefense,
                movement: effective.Movement);

            var skillSnapshots = skills
                .Select(s => RunCharacterSkillSnapshot.Create(
                    skillDefinitionKey: s.Key,
                    displayName: s.DisplayName,
                    skillType: s.SkillType,
                    targetingMode: s.TargetingType,
                    effectType: s.EffectType,
                    manaCost: s.ManaCost,
                    chargeCost: s.ChargeCost,
                    basePower: s.BasePower,
                    category: s.Category,
                    basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality,
                    tacticalRange: s.TacticalRange,
                    tacticalAreaShape: s.TacticalAreaShape,
                    requiresLineOfSight: s.RequiresLineOfSight,
                    cooldown: s.Cooldown,
                    isUltimate: s.IsUltimate,
                    emotionalRegister: s.EmotionalRegister))
                .ToArray();

            characterSnapshots.Add(RunCharacterSnapshot.Create(
                characterId: character.CharacterId,
                definitionKey: character.DefinitionKey,
                displayName: character.DisplayName,
                emotionalRegisterCode: EmotionalTypeCode.ParseRequired(
                    characterDefinition.EmotionalRegister,
                    $"Catalog character '{character.DefinitionKey}' emotional register").ToString(),
                statBlock: statSnapshot,
                skills: skillSnapshots,
                equippedItemKeys: character.EquippedItems));
        }

        var playerSnapshot = RunPlayerSnapshot.Create(
            playerId: snapshot.PlayerId,
            displayName: snapshot.DisplayName,
            characters: characterSnapshots,
            createdAtUtc: _clock.UtcNow);

        run.AttachPlayerSnapshot(playerSnapshot);

        var savedScores = await _playerProfileGateway.GetNpcReputationScoresAsync(request.PlayerId, cancellationToken);
        foreach (var score in savedScores)
        {
            var relationship = Domain.Npcs.NpcRelationship.Rehydrate(
                score.NpcKey,
                score.Score,
                woundStates: new Dictionary<string, Domain.Npcs.WoundState>(),
                flags: [],
                score.TimesMet,
                score.CurrentDialogueNodeKey);
            run.RehydrateNpcRelationship(relationship);
        }

        await _runRepository.AddAsync(run, cancellationToken);

        return new StartRunResponse(RunDto.FromDomain(run));
    }

    private static EmotionalAffinityMatrixSnapshot ToDomainMatrix(
        CatalogEmotionalAffinityMatrixSnapshot source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return EmotionalAffinityMatrixSnapshot.Create(
            source.Version,
            source.Rules.Select(rule => new EmotionalAffinityRuleSnapshot(
                EmotionalTypeCode.ParseRequired(rule.AttackingRegister, "Affinity attacking register"),
                EmotionalTypeCode.ParseRequired(rule.DefendingRegister, "Affinity defending register"),
                Enum.TryParse<DamageEffectiveness>(rule.Outcome, true, out var outcome)
                    ? outcome
                    : throw new InvalidOperationException(
                        $"Catalog affinity outcome '{rule.Outcome}' is invalid."),
                rule.Multiplier)));
    }
}
