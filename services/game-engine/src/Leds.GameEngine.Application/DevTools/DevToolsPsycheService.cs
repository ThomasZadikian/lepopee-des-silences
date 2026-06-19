using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Application.DevTools;

public sealed class DevToolsPsycheService : IDevToolsPsycheService
{
    private readonly IRunRepository _runRepository;
    private readonly IRunPsycheEvolver _psycheEvolver;

    public DevToolsPsycheService(IRunRepository runRepository, IRunPsycheEvolver psycheEvolver)
    {
        _runRepository = runRepository;
        _psycheEvolver = psycheEvolver;
    }

    public async Task<DevToolsRunPsycheResult> GetPsycheAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _runRepository.GetByIdAsync(new RunId(runId), cancellationToken)
            ?? throw new NotFoundException("Run", runId);

        var trajectory = _psycheEvolver.EvolveTrajectory(run);
        var current = trajectory.Count > 0 ? trajectory[^1].Psyche : RunPsyche.Initial();

        return new DevToolsRunPsycheResult(
            runId,
            current.Dominant().ToString(),
            ToMap(current),
            trajectory
                .Select(step => new DevToolsPsycheStep(
                    step.Depth,
                    step.Psyche.Dominant().ToString(),
                    ToMap(step.Psyche)))
                .ToList());
    }

    private static IReadOnlyDictionary<string, decimal> ToMap(RunPsyche psyche) =>
        psyche.Distribution.Probabilities.ToDictionary(kv => kv.Key.Value, kv => kv.Value);
}