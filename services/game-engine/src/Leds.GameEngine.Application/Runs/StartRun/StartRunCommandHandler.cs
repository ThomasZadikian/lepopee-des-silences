using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed class StartRunCommandHandler : IRequestHandler<StartRunCommand, StartRunResponse>
{
    /// <summary>Carnet de bord — see SeedEmotionsAsync in the catalog seed.</summary>
    private const string JournalItemKey = "canon.item.carnet-de-bord";

    private readonly IRunGenerator _runGenerator;
    private readonly IRunRepository _runRepository;
    private readonly IPlayerRunSnapshotGateway _playerGateway;
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly ICatalogContentGateway _catalogGateway;
    private readonly IClock _clock;

    public StartRunCommandHandler(
        IRunGenerator runGenerator,
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        IPlayerProfileGateway playerProfileGateway,
        ICatalogContentGateway catalogGateway,
        IClock clock)
    {
        _runGenerator = runGenerator;
        _runRepository = runRepository;
        _playerGateway = playerGateway;
        _playerProfileGateway = playerProfileGateway;
        _catalogGateway = catalogGateway;
        _clock = clock;
    }

    public async Task<StartRunResponse> Handle(
        StartRunCommand request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _playerGateway.GetRunSnapshotAsync(request.PlayerId, cancellationToken);

        var profile = await _playerProfileGateway.GetProfileAsync(request.PlayerId, cancellationToken);
        var journalEnabled = profile.PermanentItems?.Any(item =>
            string.Equals(item.ItemDefinitionKey, JournalItemKey, StringComparison.OrdinalIgnoreCase)) ?? false;

        var seed = _runGenerator.GenerateSeed();
        var initialRoom = await _runGenerator.GenerateInitialRoomAsync(seed, cancellationToken);

        var mainCharacter = snapshot.Characters.FirstOrDefault()
            ?? throw new InvalidOperationException("Player snapshot has no available characters.");

        var equipmentEffects = await CollectEquippedItemEffectsAsync(
            mainCharacter.EquippedItems, cancellationToken);

        var statBonuses = equipmentEffects
            .Where(e => string.Equals(e.Kind, "StatBonus", StringComparison.OrdinalIgnoreCase)
                && e.StatKind is not null
                && e.Amount is not null)
            .GroupBy(e => e.StatKind!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount!.Value), StringComparer.OrdinalIgnoreCase);

        int StatBonus(string statKind) => statBonuses.TryGetValue(statKind, out var value) ? value : 0;

        // Percentage bonuses (e.g. Bague du courage: +10% Speed, +10% AttackPower) —
        // the default way to author stat-boosting equipment going forward. Computed
        // against the character's raw base stat, independent of any flat StatBonus,
        // then both are added to the base (see EffectiveStat below).
        var statBonusPercents = equipmentEffects
            .Where(e => string.Equals(e.Kind, "StatBonusPercent", StringComparison.OrdinalIgnoreCase)
                && e.StatKind is not null
                && e.Amount is not null)
            .GroupBy(e => e.StatKind!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount!.Value), StringComparer.OrdinalIgnoreCase);

        int StatBonusPercent(string statKind) => statBonusPercents.TryGetValue(statKind, out var value) ? value : 0;

        int EffectiveStat(string statKind, int baseValue) =>
            baseValue + StatBonus(statKind) + (int)Math.Round(baseValue * StatBonusPercent(statKind) / 100.0);

        var effectiveMaxHp = EffectiveStat("MaxVitality", mainCharacter.Stats.MaxVitality);
        var effectiveAttack = EffectiveStat("AttackPower", mainCharacter.Stats.AttackPower);
        var effectiveDefense = EffectiveStat("Defense", mainCharacter.Stats.Defense);
        var effectiveSpeed = EffectiveStat("Speed", mainCharacter.Stats.Speed);
        var effectiveFocus = EffectiveStat("Focus", mainCharacter.Stats.Focus);
        var effectiveMana = EffectiveStat("Mana", mainCharacter.Stats.Mana);
        var effectiveCharge = EffectiveStat("Charge", mainCharacter.Stats.Charge);
        var effectiveRunItemCapacity = EffectiveStat("RunItemCapacity", Run.DefaultRunItemCapacity);

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

        // e.g. Bague de Iris: +20% of whatever Guard the protagonist would otherwise
        // start combat with (Law/climate-derived — see CombatFactory's guardBonus).
        var guardBonusPercent = StatBonusPercent("Guard");

        var grantedSkillKeys = equipmentEffects
            .Where(e => string.Equals(e.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(e.SkillKey))
            .Select(e => e.SkillKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key => !mainCharacter.Skills.Any(
                s => string.Equals(s.SkillDefinitionKey, key, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var grantedSkills = await CollectGrantedSkillsAsync(grantedSkillKeys, cancellationToken);

        // mainCharacter.Skills comes from player-service's run-snapshot, which only
        // guarantees the equipped skill KEY is correct — DisplayName/EffectType/BasePower
        // there can be a best-effort guess (e.g. for a skill unlocked from an NPC offering
        // like Mané's "Favorite de Elise"). Re-resolve each key against the catalog (the
        // actual source of truth) and only fall back to the snapshot's own data if the
        // catalog lookup misses, so a legendary/rare skill's real mechanics (EffectType,
        // BasePower, Category, percent-of-max heal, ...) are what actually gets cast.
        var catalogLearnedSkills = await CollectGrantedSkillsAsync(mainCharacter.SkillKeys, cancellationToken);

        var playerSkills = mainCharacter.Skills
            .Select(fallback =>
            {
                var fromCatalog = catalogLearnedSkills.FirstOrDefault(
                    s => string.Equals(s.Key, fallback.SkillDefinitionKey, StringComparison.OrdinalIgnoreCase));

                return fromCatalog is not null
                    ? PlayerRuntimeSkill.Create(
                        key: fromCatalog.Key,
                        displayName: fromCatalog.DisplayName,
                        skillType: fromCatalog.SkillType,
                        targetingType: fromCatalog.TargetingType,
                        effectType: fromCatalog.EffectType,
                        manaCost: fromCatalog.ManaCost,
                        chargeCost: fromCatalog.ChargeCost,
                        basePower: fromCatalog.BasePower,
                        category: fromCatalog.Category,
                        basePowerIsPercentOfMaxVitality: fromCatalog.BasePowerIsPercentOfMaxVitality)
                    : PlayerRuntimeSkill.Create(
                        key: fallback.SkillDefinitionKey,
                        displayName: fallback.DisplayName,
                        skillType: fallback.SkillType,
                        targetingType: fallback.TargetingMode,
                        effectType: fallback.EffectType,
                        manaCost: fallback.ManaCost,
                        chargeCost: fallback.ChargeCost,
                        basePower: fallback.BasePower);
            })
            .Concat(grantedSkills.Select(s => PlayerRuntimeSkill.Create(
                key: s.Key,
                displayName: s.DisplayName,
                skillType: s.SkillType,
                targetingType: s.TargetingType,
                effectType: s.EffectType,
                manaCost: s.ManaCost,
                chargeCost: s.ChargeCost,
                basePower: s.BasePower,
                category: s.Category,
                basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality)))
            .ToArray();

        var run = Run.StartNew(
            request.PlayerId,
            seed,
            _runGenerator.GeneratorVersion,
            _runGenerator.MarkovMatrixVersion,
            initialRoom,
            _clock.UtcNow,
            maxHp: effectiveMaxHp,
            currentHp: effectiveMaxHp,
            attack: effectiveAttack,
            defense: effectiveDefense,
            speed: effectiveSpeed,
            focus: effectiveFocus,
            mana: effectiveMana,
            charge: effectiveCharge,
            playerSkills: playerSkills,
            runItemCapacity: effectiveRunItemCapacity,
            typedDamageReductions: typedDamageReductions,
            hitChanceBonusPercent: hitChanceBonusPercent,
            dotDurationReductionPercent: dotDurationReductionPercent,
            dotDamageReductionPercent: dotDamageReductionPercent,
            dotDamageBonusPercent: dotDamageBonusPercent,
            magicDamageBonusPercent: magicDamageBonusPercent,
            magicDamageReductionPercent: magicDamageReductionPercent,
            criticalChanceBonusPercent: criticalChanceBonusPercent,
            guardBonusPercent: guardBonusPercent,
            journalEnabled: journalEnabled);

        var characterSnapshots = snapshot.Characters
            .Select(c =>
            {
                var statSnapshot = RunCharacterStatSnapshot.Create(
                    maxVitality: c.Stats.MaxVitality,
                    attackPower: c.Stats.AttackPower,
                    defense: c.Stats.Defense,
                    startingGuard: c.Stats.StartingGuard,
                    speed: c.Stats.Speed,
                    initiative: c.Stats.Initiative,
                    recovery: c.Stats.Recovery,
                    focus: c.Stats.Focus,
                    mana: c.Stats.Mana,
                    charge: c.Stats.Charge);

                var skillSnapshots = c.Skills
                    .Select(s => RunCharacterSkillSnapshot.Create(
                        skillDefinitionKey: s.SkillDefinitionKey,
                        displayName: s.DisplayName,
                        skillType: s.SkillType,
                        targetingMode: s.TargetingMode,
                        effectType: s.EffectType,
                        manaCost: s.ManaCost,
                        chargeCost: s.ChargeCost,
                        basePower: s.BasePower))
                    .ToArray();

                return RunCharacterSnapshot.Create(
                    characterId: c.CharacterId,
                    definitionKey: c.DefinitionKey,
                    displayName: c.DisplayName,
                    statBlock: statSnapshot,
                    skills: skillSnapshots);
            })
            .ToArray();

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

    private async Task<IReadOnlyCollection<CatalogItemEquipmentEffect>> CollectEquippedItemEffectsAsync(
        IReadOnlyCollection<string> equippedItemKeys,
        CancellationToken cancellationToken)
    {
        if (equippedItemKeys.Count == 0)
        {
            return [];
        }

        var effects = new List<CatalogItemEquipmentEffect>();

        foreach (var itemKey in equippedItemKeys)
        {
            var result = await _catalogGateway.GetItemDefinitionByKeyAsync(itemKey, cancellationToken);
            if (result.IsSuccess && result.Value.EquipmentEffects is { Count: > 0 } itemEffects)
            {
                effects.AddRange(itemEffects);
            }
        }

        return effects;
    }

    private async Task<IReadOnlyCollection<CatalogSkillDefinition>> CollectGrantedSkillsAsync(
        IReadOnlyCollection<string> skillKeys,
        CancellationToken cancellationToken)
    {
        if (skillKeys.Count == 0)
        {
            return [];
        }

        var skills = new List<CatalogSkillDefinition>();

        foreach (var skillKey in skillKeys)
        {
            var skill = await _catalogGateway.GetSkillDefinitionByKeyAsync(skillKey, cancellationToken);
            if (skill is not null)
            {
                skills.Add(skill);
            }
        }

        return skills;
    }
}
