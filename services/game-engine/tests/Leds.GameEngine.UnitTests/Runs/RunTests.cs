using FluentAssertions;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;

namespace Leds.GameEngine.UnitTests.Runs;

public sealed class RunTests
{
    [Fact]
    public void StartNew_ShouldCreateActiveRun_WithInitialRoomAndMetadata()
    {
        var playerId = Guid.NewGuid();
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            playerId,
            "seed-001",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.Id.Value.Should().NotBeEmpty();
        run.PlayerId.Should().Be(playerId);
        run.Seed.Should().Be("seed-001");
        run.GeneratorVersion.Should().Be("gen-0.4.0");
        run.MarkovMatrixVersion.Should().Be("markov-0.2.0");
        run.Status.Should().Be(RunStatus.Active);
        run.CurrentDepth.Should().Be(0);

        run.CurrentRoom.Depth.Should().Be(0);
        run.CurrentRoom.RoomType.Should().Be(RoomType.Threshold);
        run.CurrentRoom.Theme.Should().Be("Threshold");
        run.CurrentRoom.CurrentNodeDepth.Should().Be(0);
        run.CurrentRoom.MaxNodeDepth.Should().Be(4);
        run.CurrentRoom.State.Should().Be(RoomState.Active);
        run.CurrentRoom.TotalNodeCount.Should().Be(6);

        run.CurrentRoom.AvailableNodes.Should().HaveCount(5);
        run.CurrentRoom.Nodes.Should().ContainSingle(node => node.IsBoss);
    }

    [Fact]
    public void StartNew_ShouldSeedPlayerStateManaAndCharge_FromArguments()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-mana",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            mana: 25,
            charge: 3,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.PlayerState!.Mana.Should().Be(25);
        run.PlayerState!.Charge.Should().Be(3);
    }

    [Fact]
    public void StartNew_ShouldSeedMagicAttackAndMagicDefense_FromArguments()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-magic-stats",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            magicAttack: 9,
            magicDefense: 4,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.MagicAttack.Should().Be(9);
        run.MagicDefense.Should().Be(4);
    }

    [Fact]
    public void StartNew_ShouldDefaultMagicAttackAndMagicDefense_ToZero_WhenNotProvided()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-magic-stats-default",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.MagicAttack.Should().Be(0);
        run.MagicDefense.Should().Be(0);
    }

    [Fact]
    public void AppendJournalEntry_ShouldRecordText_WhenJournalEnabled()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-journal",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            journalEnabled: true,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.AppendJournalEntry("J'ai trouvé un objet abandonné dans la Pièce des émotions, c'était un carnet.");

        run.JournalEntries.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                RoomIndex = 0,
                Text = "J'ai trouvé un objet abandonné dans la Pièce des émotions, c'était un carnet."
            });
    }

    [Fact]
    public void AppendJournalEntry_ShouldBeNoOp_WhenJournalDisabled()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var run = Run.StartNew(
            Guid.NewGuid(),
            "seed-no-journal",
            "gen-0.4.0",
            "markov-0.2.0",
            initialRoom,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.AppendJournalEntry("Ceci ne devrait jamais être écrit.");

        run.JournalEntries.Should().BeEmpty();
    }

    [Fact]
    public void EnterGridNode_ShouldSelectRequestedNode()
    {
        var run = TestGameEngineFactory.CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        TestGameEngineFactory.EnterNode(run, selectedNode);

        selectedNode.State.Should().Be(NodeState.Selected);
        run.CurrentRoom.State.Should().Be(RoomState.NodeSelected);
    }

    [Fact]
    public void EnterGridNode_ShouldThrow_WhenAnotherNodeIsAlreadySelected()
    {
        var run = TestGameEngineFactory.CreateRun();

        var firstNode = run.CurrentRoom.AvailableNodes.First();
        var secondNode = run.CurrentRoom.AvailableNodes.Last();

        TestGameEngineFactory.EnterNode(run, firstNode);

        var act = () => run.EnterGridNode(secondNode.Id.Value);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Room is not waiting for a node selection.");
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldResolveSelectedNode_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = TestGameEngineFactory.CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        TestGameEngineFactory.EnterNode(run, selectedNode);
        run.ResolveCurrentEvent();

        selectedNode.State.Should().Be(NodeState.Resolved);
        run.Status.Should().Be(RunStatus.Active);
        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);
    }

    [Fact]
    public void ProgressCurrentRoom_ShouldReturnToActiveExploration_AfterCurrentEventIsResolved()
    {
        var run = TestGameEngineFactory.CreateRun();

        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        TestGameEngineFactory.EnterNode(run, selectedNode);
        run.ResolveCurrentEvent();

        run.CurrentRoom.State.Should().Be(RoomState.NodeResolved);

        run.ProgressCurrentRoom();

        run.CurrentRoom.State.Should().Be(RoomState.Active);
    }

    [Fact]
    public void ResolveCurrentEvent_ShouldCompleteRoom_AndSetRunRoomResolved_WhenRoomBossIsResolved()
    {
        var run = TestGameEngineFactory.CreateRun();

        var bossNode = run.CurrentRoom.Nodes.Single(n => n.IsBoss);

        TestGameEngineFactory.EnterNode(run, bossNode);
        run.ResolveCurrentEvent();

        bossNode.State.Should().Be(NodeState.Resolved);
        run.CurrentRoom.State.Should().Be(RoomState.Completed);
        run.Status.Should().Be(RunStatus.RoomResolved);
    }

    [Fact]
    public void MoveToNextRoom_ShouldThrow_WhenRunIsCompleted()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.MoveToNextRoom(
            TestGameEngineFactory.CreateThresholdRoom(depth: 1));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is closed.");
    }

    [Fact]
    public void MoveToNextRoom_ShouldThrow_WhenRunIsFailed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.FailRun(DateTimeOffset.UtcNow);

        var act = () => run.MoveToNextRoom(
            TestGameEngineFactory.CreateThresholdRoom(depth: 1));

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is closed.");
    }

    [Fact]
    public void CompleteRun_ShouldCloseRun_AsCompleted()
    {
        var run = TestGameEngineFactory.CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        run.CompleteRun(endedAt);

        run.Status.Should().Be(RunStatus.Completed);
        run.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void CompleteRun_ShouldThrow_WhenRunIsAlreadyClosed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.CompleteRun(DateTimeOffset.UtcNow);

        var act = () => run.CompleteRun(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is already closed.");
    }

    [Fact]
    public void FailRun_ShouldCloseRun_AsFailed()
    {
        var run = TestGameEngineFactory.CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(3);

        run.FailRun(endedAt);

        run.Status.Should().Be(RunStatus.Failed);
        run.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void FailRun_ShouldThrow_WhenRunIsAlreadyClosed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.FailRun(DateTimeOffset.UtcNow);

        var act = () => run.FailRun(DateTimeOffset.UtcNow);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run is already closed.");
    }

    [Fact]
    public void Abandon_ShouldCloseRun()
    {
        var run = TestGameEngineFactory.CreateRun();

        var endedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        run.Abandon(endedAt);

        run.Status.Should().Be(RunStatus.Abandoned);
        run.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldAddActiveLaw_WhenRunIsActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[]
            {
            PalaceLawDomain.Narrative,
            PalaceLawDomain.Generation
            });

        run.ActivatePalaceLaw(law);

        run.ActivePalaceLaws.Should().ContainSingle();

        var activeLaw = run.ActivePalaceLaws.Single();

        activeLaw.Key.Should().Be("law-silence-v1");
        activeLaw.Name.Should().Be("Loi du Silence");
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldNotDuplicateLaw_WhenLawIsAlreadyActive()
    {
        var run = TestGameEngineFactory.CreateRun();

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative });

        run.ActivatePalaceLaw(law);
        run.ActivatePalaceLaw(law);

        run.ActivePalaceLaws.Should().ContainSingle();
    }

    [Fact]
    public void ActivatePalaceLaw_ShouldThrowDomainException_WhenRunIsClosed()
    {
        var run = TestGameEngineFactory.CreateRun();

        run.Abandon(DateTimeOffset.UtcNow);

        var law = PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "1.0.0",
            new[] { PalaceLawDomain.Narrative });

        var act = () => run.ActivatePalaceLaw(law);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Cannot activate a palace law on a closed run.");
    }

    // -----------------------------------------------------------------------
    // ApplyReward — MVP heal rewards
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyReward_ShouldHealPlayer_WhenRewardTypeIsHeal()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "reward-heal-seed",
            "gen-test",
            "markov-test",
            TestGameEngineFactory.CreateThresholdRoom(),
            DateTimeOffset.UtcNow,
            maxHp: 40,
            currentHp: 10,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 15 PV.",
            "heal:15");

        run.ApplyReward(choice);

        run.CurrentHp.Should().Be(25);
    }

    [Fact]
    public void ApplyReward_ShouldNotExceedMaxVitality()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "reward-heal-cap-seed",
            "gen-test",
            "markov-test",
            TestGameEngineFactory.CreateThresholdRoom(),
            DateTimeOffset.UtcNow,
            maxHp: 40,
            currentHp: 35,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 15 PV.",
            "heal:15");

        run.ApplyReward(choice);

        run.CurrentHp.Should().Be(40);
    }

    [Fact]
    public void ApplyReward_ShouldBeANoOp_WhenRewardTypeIsDecline()
    {
        var run = Run.StartNew(
            Guid.NewGuid(),
            "reward-decline-seed",
            "gen-test",
            "markov-test",
            TestGameEngineFactory.CreateThresholdRoom(),
            DateTimeOffset.UtcNow,
            maxHp: 40,
            currentHp: 20,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.Decline,
            "Refuser",
            "Tu quittes le marchand les mains vides.",
            "decline:merchant");

        run.ApplyReward(choice);

        run.CurrentHp.Should().Be(20, because: "declining a merchant offer must not heal, damage, or grant anything");
        run.RunItems.Should().BeEmpty();
    }

    [Fact]
    public void ApplyReward_ShouldThrow_WhenNoPendingReward()
    {
        var run = TestGameEngineFactory.CreateRun();
        var choice = RewardChoice.Create(
            RewardType.Heal,
            "Soin",
            "Restaure 10 PV.",
            "heal:10");

        var act = () => run.ApplyReward(choice);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Run has no pending reward offer.");
    }

    [Fact]
    public void ApplyReward_ShouldThrow_WhenRewardTypeIsUnsupported()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.SetPendingRewardOffer(RewardOfferId.New());

        var choice = RewardChoice.Create(
            RewardType.StatBonus,
            "Bonus d'attaque",
            "Non exposé dans le MVP.",
            "stat_bonus:attack:5");

        var act = () => run.ApplyReward(choice);

        act.Should()
            .Throw<DomainException>()
            .WithMessage("Reward type 'StatBonus' is not supported.");
    }

    [Fact]
    public void TryAddRunItem_ShouldAccept_UntilCapacityIsReached()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-cap", "gen-0.4.0", "markov-0.2.0",
            initialRoom, DateTimeOffset.UtcNow, runItemCapacity: 2,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        var accepted1 = run.TryAddRunItem(CreateDistinctItem("item.a"));
        var accepted2 = run.TryAddRunItem(CreateDistinctItem("item.b"));

        accepted1.Should().BeTrue();
        accepted2.Should().BeTrue();
        run.RunItems.Should().HaveCount(2);
    }

    [Fact]
    public void TryAddRunItem_ShouldReject_WhenBagIsFull()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-cap", "gen-0.4.0", "markov-0.2.0",
            initialRoom, DateTimeOffset.UtcNow, runItemCapacity: 2,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.TryAddRunItem(CreateDistinctItem("item.a"));
        run.TryAddRunItem(CreateDistinctItem("item.b"));

        var accepted3 = run.TryAddRunItem(CreateDistinctItem("item.c"));

        accepted3.Should().BeFalse();
        run.RunItems.Should().HaveCount(2);
    }

    [Fact]
    public void TryAddRunItem_ShouldStackIntoExistingItem_WithoutCountingAgainstCapacity()
    {
        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();
        var run = Run.StartNew(
            Guid.NewGuid(), "seed-cap", "gen-0.4.0", "markov-0.2.0",
            initialRoom, DateTimeOffset.UtcNow, runItemCapacity: 1,
            emotionalAffinityMatrix: Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create());

        run.TryAddRunItem(CreateDistinctItem("item.a", quantity: 1));
        var accepted = run.TryAddRunItem(CreateDistinctItem("item.a", quantity: 1));

        accepted.Should().BeTrue();
        run.RunItems.Should().ContainSingle();
        run.RunItems.Single().Quantity.Should().Be(2);
    }

    private static RunItem CreateDistinctItem(string definitionKey, int quantity = 1) =>
        RunItem.Create(
            definitionKey,
            definitionKey,
            "Un objet de test.",
            RunItemType.Consumable,
            RunItemRarity.Common,
            quantity,
            RunItemEffectType.Heal,
            10);
}