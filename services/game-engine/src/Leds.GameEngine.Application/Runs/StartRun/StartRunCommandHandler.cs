using Leds.GameEngine.Application.Abstractions;
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

        var playerSkills = mainCharacter.SkillKeys
            .Select(key => PlayerRuntimeSkill.Create(
                key: key,
                displayName: key,
                skillType: "Damage",
                targetingType: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Self" : "SingleEnemy",
                effectType: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Guard" : "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? 5 : 10))
            .ToArray();

        var run = Run.StartNew(
            request.PlayerId,
            seed,
            _runGenerator.GeneratorVersion,
            _runGenerator.MarkovMatrixVersion,
            initialRoom,
            _clock.UtcNow,
            maxHp: mainCharacter.MaxVitality,
            currentHp: mainCharacter.MaxVitality,
            playerSkills: playerSkills);

        await _runRepository.AddAsync(run, cancellationToken);

        return new StartRunResponse(RunDto.FromDomain(run));
    }
}