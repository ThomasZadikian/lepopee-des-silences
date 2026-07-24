using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;

namespace Leds.GameEngine.UnitTests.Common.Factories;

public static class TestGameEngineFactory
{
    public static MapNode CreateMapNode(
        NodeEventType eventType = NodeEventType.Combat,
        int riskLevel = 25,
        string rewardProfile = "standard",
        int row = 0,
        int lane = 0,
        IReadOnlyCollection<NodeId>? parentNodeIds = null,
        bool isBoss = false,
        NodeState initialState = NodeState.Available)
    {
        return MapNode.Create(
            eventType,
            riskLevel,
            rewardProfile,
            row,
            lane,
            parentNodeIds ?? Array.Empty<NodeId>(),
            isBoss,
            initialState);
    }

    /// <summary>
    /// Moves the party onto <paramref name="node"/>'s cell (if not already there) and enters
    /// it — the grid-room equivalent of the old Classic-mode "choose this node" one-liner.
    /// </summary>
    public static void EnterNode(Run run, MapNode node)
    {
        var grid = run.CurrentRoom.Grid;

        if (grid.PartyX != node.Lane || grid.PartyY != node.Row)
        {
            run.MoveParty(node.Lane, node.Row);
        }

        run.EnterGridNode(node.Id.Value);
    }

    public static Room CreateThresholdRoom(
        NodeEventType targetInitialEventType = NodeEventType.Combat,
        int depth = 0)
    {
        return CreateThresholdRoomWithTargetInitialNode(targetInitialEventType, depth).Room;
    }

    /// <summary>
    /// A 5x5 grid room: party starts at (0,0) (a cell no node may occupy — see
    /// <see cref="Room.Create(int, RoomType, PalaceRoomState, string, RoomBossProfile, IEnumerable{MapNode}, int, int, int, int, int, string, string)"/>),
    /// the target node one step away at (1,0), a handful of filler nodes, and the boss at
    /// (4,4) — Manhattan distance 8, within the default 10-budget.
    /// </summary>
    public static TestRoomWithTargetNode CreateThresholdRoomWithTargetInitialNode(
        NodeEventType targetInitialEventType,
        int depth = 0,
        int movementBudget = 10)
    {
        var roomType = RoomType.Threshold;

        var bossProfile = RoomBossProfile.Create(
            bossId: "threshold-guardian",
            name: "Gardien du Seuil",
            roomType: roomType,
            dangerHint: "High",
            enemyTemplateKey: "boss-threshold-guardian-v1");

        var targetNode = CreateMapNode(
            eventType: targetInitialEventType,
            riskLevel: 25,
            rewardProfile: "standard",
            row: 0,
            lane: 1,
            isBoss: false,
            initialState: NodeState.Available);

        var alternativeInitialNode = CreateMapNode(
            eventType: NodeEventType.Item,
            riskLevel: 10,
            rewardProfile: "standard",
            row: 1,
            lane: 0,
            initialState: NodeState.Available);

        var progressionNodeFromTarget = CreateMapNode(
            eventType: NodeEventType.Combat,
            riskLevel: 30,
            rewardProfile: "combat-common",
            row: 0,
            lane: 2,
            initialState: NodeState.Available);

        var progressionNodeFromAlternativeA = CreateMapNode(
            eventType: NodeEventType.Rest,
            riskLevel: 5,
            rewardProfile: "healing-only",
            row: 1,
            lane: 1,
            initialState: NodeState.Available);

        var progressionNodeFromAlternativeB = CreateMapNode(
            eventType: NodeEventType.Rare,
            riskLevel: 40,
            rewardProfile: "rare",
            row: 2,
            lane: 0,
            initialState: NodeState.Available);

        var bossNode = CreateMapNode(
            eventType: NodeEventType.RoomBoss,
            riskLevel: 85,
            rewardProfile: "room-boss",
            row: 4,
            lane: 4,
            isBoss: true,
            initialState: NodeState.Available);

        var room = Room.Create(
            depth: depth,
            roomType: roomType,
            theme: "Threshold",
            bossProfile: bossProfile,
            nodes: new[]
            {
                targetNode,
                alternativeInitialNode,
                progressionNodeFromTarget,
                progressionNodeFromAlternativeA,
                progressionNodeFromAlternativeB,
                bossNode
            },
            gridWidth: 5,
            gridHeight: 5,
            movementBudget: movementBudget,
            startX: 0,
            startY: 0,
            layoutTemplateKey: "test-grid-v1",
            layoutTemplateVersion: "1.0.0");

        return new TestRoomWithTargetNode(room, targetNode);
    }

    public static Run CreateRun(
        NodeEventType targetInitialEventType = NodeEventType.Combat,
        bool lawDenialEnabled = false,
        int reputationGainBonusPercent = 0,
        bool himLitProtectionEnabled = false,
        bool caliceInfiniEnabled = false)
    {
        var room = CreateThresholdRoom(targetInitialEventType);

        return Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: room,
            startedAt: DateTimeOffset.UtcNow,
            lawDenialEnabled: lawDenialEnabled,
            reputationGainBonusPercent: reputationGainBonusPercent,
            himLitProtectionEnabled: himLitProtectionEnabled,
            caliceInfiniEnabled: caliceInfiniEnabled);
    }

    public static Run CreateRunWithPlayerSnapshot(
        NodeEventType targetInitialEventType = NodeEventType.Combat)
    {
        var run = CreateRun(targetInitialEventType);

        var statBlock = RunCharacterStatSnapshot.Create(
            maxVitality: 100,
            attackPower: 12,
            defense: 6,
            startingGuard: 0,
            speed: 10,
            initiative: 10,
            recovery: 5,
            focus: 0,
            mana: 0,
            charge: 0);

        var skills = new[]
        {
            RunCharacterSkillSnapshot.Create(
                skillDefinitionKey: "skill.basic.strike",
                displayName: "Frappe",
                skillType: "Damage",
                targetingMode: "SingleEnemy",
                effectType: "Damage",
                manaCost: 0,
                chargeCost: 0,
                basePower: 10)
        };

        var character = RunCharacterSnapshot.Create(
            characterId: Guid.NewGuid(),
            definitionKey: "character.player.self",
            displayName: "Le Porteur",
            statBlock: statBlock,
            skills: skills);

        var snapshot = RunPlayerSnapshot.Create(
            playerId: run.PlayerId,
            displayName: "Joueur",
            characters: [character],
            createdAtUtc: DateTimeOffset.UtcNow);

        run.AttachPlayerSnapshot(snapshot);

        return run;
    }

    public static TestRunWithTargetNode CreateRunWithTargetInitialNode(
        NodeEventType targetInitialEventType)
    {
        var roomWithTargetNode = CreateThresholdRoomWithTargetInitialNode(
            targetInitialEventType);

        var run = Run.StartNew(
            playerId: Guid.NewGuid(),
            seed: "seed-unit-test-target-node",
            generatorVersion: "gen-test",
            markovMatrixVersion: "markov-test",
            initialRoom: roomWithTargetNode.Room,
            startedAt: DateTimeOffset.UtcNow);

        return new TestRunWithTargetNode(run, roomWithTargetNode.TargetNode);
    }

    public static TestRunWithTargetNode CreateRunWithSelectedTargetNode(
        NodeEventType targetInitialEventType)
    {
        var runWithTargetNode = CreateRunWithTargetInitialNode(
            targetInitialEventType);

        EnterNode(runWithTargetNode.Run, runWithTargetNode.TargetNode);

        return runWithTargetNode;
    }

    public static TestRunWithTargetNode CreateRunWithResolvedCurrentEvent(
        NodeEventType targetInitialEventType)
    {
        var runWithTargetNode = CreateRunWithSelectedTargetNode(
            targetInitialEventType);

        runWithTargetNode.Run.ResolveCurrentEvent();

        return runWithTargetNode;
    }

    /// <summary>
    /// A run whose current room is fully resolved (boss defeated, <see cref="RunStatus.RoomResolved"/>) —
    /// walks the party straight to the boss cell and resolves it, without bothering to visit the
    /// filler nodes along the way (irrelevant to what every caller of this fixture actually asserts).
    /// </summary>
    public static Run CreateRunWithCompletedCurrentRoom()
    {
        var run = CreateRun();

        var bossNode = run.CurrentRoom.Nodes.Single(n => n.IsBoss);

        EnterNode(run, bossNode);
        run.ResolveCurrentEvent();

        return run;
    }

    /// <summary>Alias retained for readability at call sites that build a grid room explicitly.</summary>
    public static TestRoomWithTargetNode CreateGridThresholdRoom(
        NodeEventType targetInitialEventType = NodeEventType.Combat,
        int depth = 0,
        int movementBudget = 10)
    {
        return CreateThresholdRoomWithTargetInitialNode(targetInitialEventType, depth, movementBudget);
    }

    /// <summary>Alias retained for readability at call sites that build a grid run explicitly.</summary>
    public static Run CreateGridRun(
        NodeEventType targetInitialEventType = NodeEventType.Combat)
    {
        return CreateRun(targetInitialEventType);
    }
}

public sealed record TestRoomWithTargetNode(
    Room Room,
    MapNode TargetNode);

public sealed record TestRunWithTargetNode(
    Run Run,
    MapNode TargetNode);
