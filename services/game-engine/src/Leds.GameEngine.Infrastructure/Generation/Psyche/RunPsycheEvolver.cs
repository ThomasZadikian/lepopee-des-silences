using Leds.GameEngine.Application.Markov;
using Leds.GameEngine.Domain.Markov.Psyche;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.Infrastructure.Generation.Psyche;

public sealed class RunPsycheEvolver : IRunPsycheEvolver
{
    private readonly EmotionalCalibration _calibration;

    public RunPsycheEvolver(EmotionalCalibration calibration) => _calibration = calibration;

    public RunPsyche Evolve(Run run)
    {
        var trajectory = EvolveTrajectory(run);
        return trajectory.Count > 0 ? trajectory[^1].Psyche : RunPsyche.Initial();
    }

    public IReadOnlyList<RunPsycheStep> EvolveTrajectory(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        var tendency = _calibration.BuildTendencyMatrix();
        var psyche = RunPsyche.Initial();
        var steps = new List<RunPsycheStep>();

        foreach (var room in run.Rooms.OrderBy(r => r.Depth))
        {
            var advanced = tendency.Advance(psyche.Distribution); // persistance (auto-influence)
            var signal = BuildSignal(run, room);                  // environnement + agentivité
            psyche = RunPsyche.From(_calibration.Nudge(advanced, signal));
            steps.Add(new RunPsycheStep(room.Depth, psyche));
        }

        return steps;
    }

    private static RoomPsycheSignal BuildSignal(Run run, Room room)
    {
        var choices = room.Nodes
            .Where(node => node.State == NodeState.Resolved)
            .Select(node => node.EventType)
            .ToArray();

        var optionIds = room.Nodes
            .Where(node => node.HasChosenEventOption)
            .Select(node => node.ChosenEventOptionId!)
            .ToArray();

        return new RoomPsycheSignal(
            room.PalaceState,
            room.RoomType,
            choices,
            optionIds,
            ClimateForRoom(run, room));
    }

    private static string? ClimateForRoom(Run run, Room room)
    {
        var modifier = run.RunModifiers
            .Where(m => m.Type == RunModifierType.RoomClimate && m.ExpiresAtRoomId == room.Id.Value)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefault();

        return modifier?.Value switch
        {
            1 => "Grey",
            2 => "Rain",
            3 => "Heatwave",
            4 => "Hail",
            _ => null
        };
    }
}