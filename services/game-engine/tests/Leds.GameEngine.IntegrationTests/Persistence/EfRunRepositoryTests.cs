using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rewards;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.IntegrationTests.Persistence;

public sealed class EfRunRepositoryTests : IDisposable
{
    private readonly string _databaseName;
    private readonly GameEngineDbContext _context;
    private readonly EfRunRepository _repository;

    public EfRunRepositoryTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<GameEngineDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        _context = new GameEngineDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new EfRunRepository(_context);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRestoreEquivalentRunMap()
    {
        var run = CreateTestRun();

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

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
    public async Task SaveAndLoad_ShouldPreserveMemoryFragments()
    {
        var run = CreateTestRun();
        run.ApplyHeal(5);

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

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

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.CurrentHp.Should().Be(run.CurrentHp);
    }

    // -----------------------------------------------------------------------
    // InventoryItems round-trip
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveAndLoad_ShouldPersistInventoryItems()
    {
        var run = CreateTestRun();

        // Give the run a pending reward and apply a guard-shard item
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

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

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

        // Add item via update
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

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

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

        // Apply guard shard reward
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

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RunModifiers.Should().ContainSingle(
            m => m.Type == RunModifierType.StartingGuardBonus && !m.IsConsumed,
            because: "The StartingGuardBonus modifier created by the guard shard must survive a full persistence round-trip.");
    }

    private static Run CreateTestRun()
    {
        var node1 = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 0, []);
        var node2 = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 1, []);
        var node3 = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 2, []);
        var node4 = MapNode.Create(NodeEventType.Combat, 30, "standard", 1, 0, [node1.Id], initialState: NodeState.Planned);
        var node5 = MapNode.Create(NodeEventType.Combat, 30, "standard", 1, 1, [node1.Id, node2.Id], initialState: NodeState.Planned);
        var node6 = MapNode.Create(NodeEventType.Combat, 30, "standard", 1, 2, [node2.Id, node3.Id], initialState: NodeState.Planned);
        var bossNode = MapNode.Create(NodeEventType.RoomBoss, 50, "boss", 2, 1, [node4.Id, node5.Id, node6.Id], isBoss: true, initialState: NodeState.Planned);

        var bossProfile = RoomBossProfile.Create("boss-1", "Test Boss", RoomType.Memory, "A dark presence", "enemy.boss.1");

        var room = Room.Create(0, RoomType.Memory, "Test Room", bossProfile, [node1, node2, node3, node4, node5, node6, bossNode]);

        return Run.StartNew(
            Guid.NewGuid(),
            "test-seed-12345",
            "1.0.0",
            "1.0.0",
            room,
            DateTimeOffset.UtcNow);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}