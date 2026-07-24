using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.Dtos;
using Leds.GameEngine.Domain.Runs;
using MediatR;

namespace Leds.GameEngine.Application.Runs.StartRun;

public sealed class StartRunCommandHandler : IRequestHandler<StartRunCommand, StartRunResponse>
{
    /// <summary>Carnet de bord — see SeedEmotionsAsync in the catalog seed.</summary>
    private const string JournalItemKey = "canon.item.carnet-de-bord";

    /// <summary>Déni permanent — Erika's legendary offering. See SeedErikaAsync in the catalog seed.</summary>
    private const string LawDenialItemKey = "canon.item.deni-permanent";

    /// <summary>Peluche de Mina — Mina's rare offering. See SeedMinaAsync in the catalog seed.</summary>
    private const string ReputationBoostItemKey = "canon.item.peluche-mina";

    /// <summary>Protection de Him'Lit — Mina's legendary offering. See SeedMinaAsync in the catalog seed.</summary>
    private const string HimLitProtectionItemKey = "canon.item.protection-himlit";

    /// <summary>Calice infini — John's legendary offering. See SeedJohnAsync in the catalog seed.</summary>
    private const string CaliceInfiniItemKey = "canon.item.calice-infini";

    /// <summary>Flat percentage granted by <see cref="ReputationBoostItemKey"/> — see Run.ReputationGainBonusPercent.</summary>
    private const int ReputationBoostPercent = 10;

    private readonly IRunGenerator _runGenerator;
    private readonly IRunRepository _runRepository;
    private readonly IPlayerRunSnapshotGateway _playerGateway;
    private readonly IPlayerProfileGateway _playerProfileGateway;
    private readonly IAmbientPalaceLawPromulgator _palaceLawPromulgator;
    private readonly PlayerSkillMerger _skillMerger;
    private readonly PlayerStatMerger _statMerger;
    private readonly IClock _clock;

    public StartRunCommandHandler(
        IRunGenerator runGenerator,
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        IPlayerProfileGateway playerProfileGateway,
        IAmbientPalaceLawPromulgator palaceLawPromulgator,
        PlayerSkillMerger skillMerger,
        PlayerStatMerger statMerger,
        IClock clock)
    {
        _runGenerator = runGenerator;
        _runRepository = runRepository;
        _playerGateway = playerGateway;
        _playerProfileGateway = playerProfileGateway;
        _palaceLawPromulgator = palaceLawPromulgator;
        _skillMerger = skillMerger;
        _statMerger = statMerger;
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
        var lawDenialEnabled = profile.PermanentItems?.Any(item =>
            string.Equals(item.ItemDefinitionKey, LawDenialItemKey, StringComparison.OrdinalIgnoreCase)) ?? false;
        var reputationGainBonusPercent = profile.PermanentItems?.Any(item =>
            string.Equals(item.ItemDefinitionKey, ReputationBoostItemKey, StringComparison.OrdinalIgnoreCase)) ?? false
                ? ReputationBoostPercent : 0;
        var himLitProtectionEnabled = profile.PermanentItems?.Any(item =>
            string.Equals(item.ItemDefinitionKey, HimLitProtectionItemKey, StringComparison.OrdinalIgnoreCase)) ?? false;
        var caliceInfiniEnabled = profile.PermanentItems?.Any(item =>
            string.Equals(item.ItemDefinitionKey, CaliceInfiniItemKey, StringComparison.OrdinalIgnoreCase)) ?? false;

        var seed = _runGenerator.GenerateSeed();
        var initialRoom = await _runGenerator.GenerateInitialRoomAsync(seed, cancellationToken);

        var mainCharacter = snapshot.Characters.FirstOrDefault()
            ?? throw new InvalidOperationException("Player snapshot has no available characters.");

        var equipmentEffects = await _skillMerger.CollectEquippedItemEffectsAsync(
            mainCharacter.EquippedItems, cancellationToken);

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

        var mergedSkills = await _skillMerger.MergeSkillsAsync(mainCharacter, equipmentEffects, cancellationToken);

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
                basePowerIsPercentOfMaxVitality: s.BasePowerIsPercentOfMaxVitality))
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
            caliceInfiniEnabled: caliceInfiniEnabled);

        // Ambient promulgation ("irréfusabilité") also applies to the very first room of
        // the run: without this, MoveToNextRoomCommandHandler's guaranteed first-floor
        // draw would never fire before the player leaves the Hall (StartRun never used to
        // call the promulgator at all).
        await _palaceLawPromulgator.PromulgateForRoomTransitionAsync(run, run.CurrentRoom, cancellationToken);

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
                    charge: c.Stats.Charge,
                    magicAttack: c.Stats.MagicAttack,
                    magicDefense: c.Stats.MagicDefense);

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
}
