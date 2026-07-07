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
    private readonly IRunGenerator _runGenerator;
    private readonly IRunRepository _runRepository;
    private readonly IPlayerRunSnapshotGateway _playerGateway;
    private readonly ICatalogContentGateway _catalogGateway;
    private readonly IClock _clock;

    public StartRunCommandHandler(
        IRunGenerator runGenerator,
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        ICatalogContentGateway catalogGateway,
        IClock clock)
    {
        _runGenerator = runGenerator;
        _runRepository = runRepository;
        _playerGateway = playerGateway;
        _catalogGateway = catalogGateway;
        _clock = clock;
    }

    public async Task<StartRunResponse> Handle(
        StartRunCommand request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _playerGateway.GetRunSnapshotAsync(request.PlayerId, cancellationToken);

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

        var effectiveMaxHp = mainCharacter.Stats.MaxVitality + StatBonus("MaxVitality");
        var effectiveAttack = mainCharacter.Stats.AttackPower + StatBonus("AttackPower");
        var effectiveDefense = mainCharacter.Stats.Defense + StatBonus("Defense");
        var effectiveSpeed = mainCharacter.Stats.Speed + StatBonus("Speed");
        var effectiveFocus = mainCharacter.Stats.Focus + StatBonus("Focus");
        var effectiveRunItemCapacity = Run.DefaultRunItemCapacity + StatBonus("RunItemCapacity");

        var grantedSkillKeys = equipmentEffects
            .Where(e => string.Equals(e.Kind, "GrantSkill", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(e.SkillKey))
            .Select(e => e.SkillKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(key => !mainCharacter.Skills.Any(
                s => string.Equals(s.SkillDefinitionKey, key, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var grantedSkills = await CollectGrantedSkillsAsync(grantedSkillKeys, cancellationToken);

        var playerSkills = mainCharacter.Skills
            .Select(s => PlayerRuntimeSkill.Create(
                key: s.SkillDefinitionKey,
                displayName: s.DisplayName,
                skillType: s.SkillType,
                targetingType: s.TargetingMode,
                effectType: s.EffectType,
                manaCost: s.ManaCost,
                chargeCost: s.ChargeCost,
                basePower: s.BasePower))
            .Concat(grantedSkills.Select(s => PlayerRuntimeSkill.Create(
                key: s.Key,
                displayName: s.DisplayName,
                skillType: s.SkillType,
                targetingType: s.TargetingType,
                effectType: s.EffectType,
                manaCost: s.ManaCost,
                chargeCost: s.ChargeCost,
                basePower: s.BasePower)))
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
            playerSkills: playerSkills,
            runItemCapacity: effectiveRunItemCapacity);

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
