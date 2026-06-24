using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
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
    private readonly IClock _clock;

    public StartRunCommandHandler(
        IRunGenerator runGenerator,
        IRunRepository runRepository,
        IPlayerRunSnapshotGateway playerGateway,
        IClock clock)
    {
        _runGenerator = runGenerator;
        _runRepository = runRepository;
        _playerGateway = playerGateway;
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
            .ToArray();

        var run = Run.StartNew(
            request.PlayerId,
            seed,
            _runGenerator.GeneratorVersion,
            _runGenerator.MarkovMatrixVersion,
            initialRoom,
            _clock.UtcNow,
            maxHp: mainCharacter.Stats.MaxVitality,
            currentHp: mainCharacter.Stats.MaxVitality,
            attack: mainCharacter.Stats.AttackPower,
            defense: mainCharacter.Stats.Defense,
            speed: mainCharacter.Stats.Speed,
            focus: mainCharacter.Stats.Focus,
            playerSkills: playerSkills);

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
}
