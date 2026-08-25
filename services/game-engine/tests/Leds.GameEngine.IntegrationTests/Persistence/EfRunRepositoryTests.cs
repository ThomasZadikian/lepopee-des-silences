using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Npcs;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.IntegrationTests.Persistence;

[Collection("GameEnginePostgres")]
public sealed class EfRunRepositoryTests : IDisposable
{
    private readonly string _connStr;
    private readonly GameEngineDbContext _context;
    private readonly EfRunRepository _repository;
    private readonly GameEnginePostgresFixture _fixture;

    public EfRunRepositoryTests(GameEnginePostgresFixture fixture)
    {
        _fixture = fixture;
        (_context, _connStr) = fixture.CreateContext();
        _repository = new EfRunRepository(_context);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRestoreEquivalentRunMap()
    {
        var run = CreateTestRun();

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(run.Id);
        loaded.PlayerId.Should().Be(run.PlayerId);
        loaded.Status.Should().Be(run.Status);
        loaded.Seed.Should().Be(run.Seed);
        loaded.GeneratorVersion.Should().Be(run.GeneratorVersion);
        loaded.MarkovMatrixVersion.Should().Be(run.MarkovMatrixVersion);
        loaded.MaxHp.Should().Be(run.MaxHp);
        loaded.CurrentHp.Should().Be(run.CurrentHp);
        loaded.Attack.Should().Be(run.Attack);
        loaded.Defense.Should().Be(run.Defense);
        loaded.Speed.Should().Be(run.Speed);
        loaded.CurrentRoomIndex.Should().Be(run.CurrentRoomIndex);

        loaded.Rooms.Should().HaveCount(run.Rooms.Count);
        loaded.CurrentRoomId.Should().Be(run.CurrentRoomId);

        var originalRoom = run.CurrentRoom;
        var loadedRoom = loaded.CurrentRoom;
        loadedRoom.Id.Should().Be(originalRoom.Id);
        loadedRoom.Depth.Should().Be(originalRoom.Depth);
        loadedRoom.RoomType.Should().Be(originalRoom.RoomType);
        loadedRoom.Theme.Should().Be(originalRoom.Theme);
        loadedRoom.State.Should().Be(originalRoom.State);
        loadedRoom.CurrentNodeDepth.Should().Be(originalRoom.CurrentNodeDepth);
        loadedRoom.MaxNodeDepth.Should().Be(originalRoom.MaxNodeDepth);
        loadedRoom.BossProfile.BossId.Should().Be(originalRoom.BossProfile.BossId);
        loadedRoom.BossProfile.Name.Should().Be(originalRoom.BossProfile.Name);
        loadedRoom.LayoutTemplateKey.Should().Be(originalRoom.LayoutTemplateKey);
        loadedRoom.LayoutTemplateVersion.Should().Be(originalRoom.LayoutTemplateVersion);

        loadedRoom.Nodes.Should().HaveCount(originalRoom.Nodes.Count);
        foreach (var originalNode in originalRoom.Nodes)
        {
            var loadedNode = loadedRoom.Nodes.FirstOrDefault(n => n.Id == originalNode.Id);
            loadedNode.Should().NotBeNull();
            loadedNode!.EventType.Should().Be(originalNode.EventType);
            loadedNode.Row.Should().Be(originalNode.Row);
            loadedNode.Lane.Should().Be(originalNode.Lane);
            loadedNode.RiskLevel.Should().Be(originalNode.RiskLevel);
            loadedNode.RewardProfile.Should().Be(originalNode.RewardProfile);
            loadedNode.IsBoss.Should().Be(originalNode.IsBoss);
            loadedNode.State.Should().Be(originalNode.State);
            loadedNode.ChosenEventOptionId.Should().Be(originalNode.ChosenEventOptionId);
            loadedNode.ParentNodeIds.Should().BeEquivalentTo(originalNode.ParentNodeIds);
        }
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreservePositionedRoomNpcs()
    {
        var run = CreateTestRun();
        var npc = RoomNpc.Create(
            "npc.majordome",
            x: 4,
            y: 0,
            behavior: NpcBehaviorArchetype.Fixed);
        run.CurrentRoom.AddRoomNpc(npc);

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);
        var loaded = await new EfRunRepository(verifyContext)
            .GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.CurrentRoom.RoomNpcs.Should().ContainSingle();
        loaded.CurrentRoom.RoomNpcs.Single().Should().BeEquivalentTo(npc);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveMemoryFragments()
    {
        var run = CreateTestRun();
        run.ApplyHeal(5);

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.MemoryFragments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenRunDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(RunId.New(), CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingRun()
    {
        var run = CreateTestRun();
        await _repository.AddAsync(run, CancellationToken.None);

        run.ApplyHeal(10);
        await _repository.UpdateAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.CurrentHp.Should().Be(run.CurrentHp);
        loaded.Revision.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectStaleRevision()
    {
        var run = CreateTestRun();
        await _repository.AddAsync(run, CancellationToken.None);

        using var firstContext = _fixture.CreateContext(_connStr);
        using var secondContext = _fixture.CreateContext(_connStr);
        var firstRepository = new EfRunRepository(firstContext);
        var secondRepository = new EfRunRepository(secondContext);
        var first = await firstRepository.GetByIdAsync(run.Id, CancellationToken.None);
        var stale = await secondRepository.GetByIdAsync(run.Id, CancellationToken.None);

        first!.ApplyHeal(1);
        await firstRepository.UpdateAsync(first, CancellationToken.None);

        stale!.ApplyHeal(2);
        var act = () => secondRepository.UpdateAsync(stale, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task HasActiveOrSuspendedAsync_ShouldFindOnlyOpenRun()
    {
        var playerId = Guid.NewGuid();
        var run = CreateTestRun(playerId);
        await _repository.AddAsync(run, CancellationToken.None);

        (await _repository.HasActiveOrSuspendedAsync(playerId, CancellationToken.None)).Should().BeTrue();

        run.CompleteRun(DateTimeOffset.UtcNow);
        await _repository.UpdateAsync(run, CancellationToken.None);

        (await _repository.HasActiveOrSuspendedAsync(playerId, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task GetOpenByPlayerIdAsync_ShouldReturnThePlayersActiveRun()
    {
        var playerId = Guid.NewGuid();
        var run = CreateTestRun(playerId);
        await _repository.AddAsync(run, CancellationToken.None);

        var loaded = await _repository.GetOpenByPlayerIdAsync(playerId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(run.Id);
    }

    [Theory]
    [InlineData("Completed", RunOutcome.Success)]
    [InlineData("Failed", RunOutcome.Defeat)]
    [InlineData("Abandoned", RunOutcome.Abandon)]
    public async Task GetByIdAsync_ShouldNormalizeLegacyTerminalStatus(
        string legacyStatus,
        RunOutcome expectedOutcome)
    {
        var run = CreateTestRun();
        await _repository.AddAsync(run, CancellationToken.None);
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE runs SET status = {legacyStatus}, outcome = NULL WHERE id = {run.Id.Value}");

        using var verifyContext = _fixture.CreateContext(_connStr);
        var loaded = await new EfRunRepository(verifyContext)
            .GetByIdAsync(run.Id, CancellationToken.None);

        loaded!.Status.Should().Be(RunStatus.Resolved);
        loaded.Outcome.Should().Be(expectedOutcome);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPersistInventoryItems()
    {
        var run = CreateTestRun();

        var offerId = RewardOfferId.New();
        run.SetPendingRewardOffer(offerId);
        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Éclat de garde",
            "Protection au combat.",
            "item:item.consumable.guard-shard:Éclat de garde:Protection au combat.:Consumable:Uncommon:Guard:8");
        run.ApplyReward(choice);
        run.ClearPendingRewardOffer();

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RunItems.Should().ContainSingle(
            i => i.DefinitionKey == "item.consumable.guard-shard",
            because: "The guard shard selected as reward must be persisted and reloaded.");
    }

    [Fact]
    public async Task UpdateAndReload_ShouldPreserveInventoryItems()
    {
        var run = CreateTestRun();
        await _repository.AddAsync(run, CancellationToken.None);

        var offerId = RewardOfferId.New();
        run.SetPendingRewardOffer(offerId);
        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Éclat de garde",
            "Protection au combat.",
            "item:item.consumable.guard-shard:Éclat de garde:Protection au combat.:Consumable:Uncommon:Guard:8");
        run.ApplyReward(choice);
        run.ClearPendingRewardOffer();

        await _repository.UpdateAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RunItems.Should().ContainSingle(
            i => i.DefinitionKey == "item.consumable.guard-shard",
            because: "InventoryItems added after initial save must survive an UpdateAsync round-trip.");
    }

    [Fact]
    public async Task UpdateAndReload_ShouldPreserveStartingGuardBonusModifier()
    {
        var run = CreateTestRun();
        await _repository.AddAsync(run, CancellationToken.None);

        var offerId = RewardOfferId.New();
        run.SetPendingRewardOffer(offerId);
        var choice = RewardChoice.Create(
            RewardType.TemporaryItem,
            "Éclat de garde",
            "Protection au combat.",
            "item:item.consumable.guard-shard:Éclat de garde:Protection au combat.:Consumable:Uncommon:Guard:8");
        run.ApplyReward(choice);
        run.ClearPendingRewardOffer();

        await _repository.UpdateAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RunModifiers.Should().ContainSingle(
            m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed,
            because: "The StartingGuardBonus modifier created by the guard shard must survive a full persistence round-trip.");
    }

    private static Run CreateTestRun(Guid? playerId = null)
    {
        var node1 = MapNode.Create(NodeEventType.Combat, 25, "standard", row: 0, lane: 1, []);
        var node2 = MapNode.Create(NodeEventType.Combat, 25, "standard", row: 0, lane: 2, []);
        var node3 = MapNode.Create(NodeEventType.Combat, 25, "standard", row: 0, lane: 3, []);
        var node4 = MapNode.Create(NodeEventType.Combat, 30, "standard", row: 1, lane: 1, []);
        var node5 = MapNode.Create(NodeEventType.Combat, 30, "standard", row: 1, lane: 2, []);
        var node6 = MapNode.Create(NodeEventType.Combat, 30, "standard", row: 1, lane: 3, []);
        var bossNode = MapNode.Create(NodeEventType.RoomBoss, 50, "boss", row: 4, lane: 4, [], isBoss: true);

        var bossProfile = RoomBossProfile.Create("boss-1", "Test Boss", RoomType.Memory, "A dark presence", "enemy.boss.1");

        var room = Room.Create(
            depth: 0,
            roomType: RoomType.Memory,
            theme: "Test Room",
            bossProfile: bossProfile,
            nodes: [node1, node2, node3, node4, node5, node6, bossNode],
            gridWidth: 5,
            gridHeight: 5,
            movementBudget: 10,
            startX: 0,
            startY: 0,
            layoutTemplateKey: "test-grid-v1",
            layoutTemplateVersion: "1.0.0");

        return Run.StartNew(
            playerId ?? Guid.NewGuid(),
            "test-seed-12345",
            "1.0.0",
            "1.0.0",
            room,
            DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.IntegrationTests.Common.TestEmotionalAffinityMatrix.Create());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
