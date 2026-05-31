using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestRunFactory
{
    public static Run CreateActiveRun()
    {
        return Run.StartNew(
            Guid.NewGuid(),
            "seed-test-001",
            "gen-0.2.0",
            "markov-0.2.0",
            TestRoomFactory.CreatePlannedRoom(depth: 0),
            DateTimeOffset.UtcNow);
    }

    public static Run CreateRunWithSelectedNode()
    {
        var run = CreateActiveRun();

        var node = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(node.Id);

        return run;
    }

    public static Run CreateRunWithResolvedCurrentEvent()
    {
        var run = CreateRunWithSelectedNode();

        run.ResolveCurrentEvent();

        return run;
    }

    public static Run CreateRunWithCompletedCurrentRoom()
    {
        var run = CreateActiveRun();

        while (run.Status == RunStatus.Active)
        {
            var node = run.CurrentRoom.AvailableNodes.First();

            run.ChooseNode(node.Id);
            run.ResolveCurrentEvent();

            if (run.Status == RunStatus.RoomResolved)
            {
                break;
            }

            run.ProgressCurrentRoom();
        }

        return run;
    }
}