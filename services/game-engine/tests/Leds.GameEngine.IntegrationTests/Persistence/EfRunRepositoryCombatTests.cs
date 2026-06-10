using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Common;
using Leds.GameEngine.Domain.NodeEvents;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.IntegrationTests.Persistence;

public sealed class EfRunRepositoryCombatTests : IDisposable
{
    private readonly string _databaseName;
    private readonly GameEngineDbContext _context;
    private readonly EfRunRepository _repository;

    public EfRunRepositoryCombatTests()
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
    public async Task SaveAndLoad_ShouldRestoreEquivalentActiveCombat()
    {
        var run = CreateTestRunWithCombat();
        var combat = run.ActiveCombat!;

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ActiveCombat.Should().NotBeNull();
        loaded.ActiveCombatId.Should().NotBeNull();

        var loadedCombat = loaded.ActiveCombat!;
        loadedCombat.Id.Should().Be(combat.Id);
        loadedCombat.RunId.Should().Be(combat.RunId);
        loadedCombat.RoomId.Should().Be(combat.RoomId);
        loadedCombat.NodeId.Should().Be(combat.NodeId);
        loadedCombat.Status.Should().Be(combat.Status);
        loadedCombat.TurnNumber.Should().Be(combat.TurnNumber);
        loadedCombat.ActiveCombatantId.Should().Be(combat.ActiveCombatantId);
        loadedCombat.CreatedAtUtc.Should().Be(combat.CreatedAtUtc);

        loadedCombat.Allies.Should().HaveCount(combat.Allies.Count);
        loadedCombat.Enemies.Should().HaveCount(combat.Enemies.Count);

        foreach (var originalAlly in combat.Allies)
        {
            var loadedAlly = loadedCombat.Allies.FirstOrDefault(a => a.Id == originalAlly.Id);
            loadedAlly.Should().NotBeNull();
            loadedAlly!.SourceKey.Should().Be(originalAlly.SourceKey);
            loadedAlly.DisplayName.Should().Be(originalAlly.DisplayName);
            loadedAlly.Side.Should().Be(originalAlly.Side);
            loadedAlly.Archetype.Should().Be(originalAlly.Archetype);
            loadedAlly.MaxVitality.Should().Be(originalAlly.MaxVitality);
            loadedAlly.CurrentVitality.Should().Be(originalAlly.CurrentVitality);
            loadedAlly.Guard.Should().Be(originalAlly.Guard);
            loadedAlly.Mana.Should().Be(originalAlly.Mana);
            loadedAlly.Charge.Should().Be(originalAlly.Charge);
            loadedAlly.Status.Should().Be(originalAlly.Status);
            loadedAlly.Skills.Should().HaveCount(originalAlly.Skills.Count);
        }

        foreach (var originalEnemy in combat.Enemies)
        {
            var loadedEnemy = loadedCombat.Enemies.FirstOrDefault(e => e.Id == originalEnemy.Id);
            loadedEnemy.Should().NotBeNull();
            loadedEnemy!.SourceKey.Should().Be(originalEnemy.SourceKey);
            loadedEnemy.CurrentVitality.Should().Be(originalEnemy.CurrentVitality);
            loadedEnemy.Status.Should().Be(originalEnemy.Status);
        }
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRestoreCombatantSkills()
    {
        var run = CreateTestRunWithCombat();
        var combat = run.ActiveCombat!;
        var ally = combat.Allies.First();

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        var loadedAlly = loaded!.ActiveCombat!.Allies.First();
        loadedAlly.Skills.Should().HaveCount(ally.Skills.Count);

        foreach (var originalSkill in ally.Skills)
        {
            var loadedSkill = loadedAlly.Skills.FirstOrDefault(s => s.Key == originalSkill.Key);
            loadedSkill.Should().NotBeNull();
            loadedSkill!.DisplayName.Should().Be(originalSkill.DisplayName);
            loadedSkill.SkillType.Should().Be(originalSkill.SkillType);
            loadedSkill.TargetingType.Should().Be(originalSkill.TargetingType);
            loadedSkill.EffectType.Should().Be(originalSkill.EffectType);
            loadedSkill.ManaCost.Should().Be(originalSkill.ManaCost);
            loadedSkill.ChargeCost.Should().Be(originalSkill.ChargeCost);
            loadedSkill.BasePower.Should().Be(originalSkill.BasePower);
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldRemoveActiveCombat_WhenRunHasNoActiveCombat()
    {
        var run = CreateTestRunWithCombat();
        run.CurrentRoom.SelectNode(run.CurrentRoom.Nodes.First(n => n.Row == 0).Id);
        await _repository.AddAsync(run, CancellationToken.None);

        run.ActiveCombat!.MarkCompleted();
        run.CompleteActiveCombat();
        await _repository.UpdateAsync(run, CancellationToken.None);

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ActiveCombat.Should().BeNull();
        loaded.ActiveCombatId.Should().BeNull();
    }

    private static Run CreateTestRunWithCombat()
    {
        var ally = Combatant.CreateAlly("player.test", "Hero", "Warrior", 100,
            [CombatantSkill.Create("skill.strike", "Strike", "Damage", "SingleEnemy", "Damage", 0, 0, 10)]);

        var enemy = Combatant.CreateEnemy("enemy.test", "Goblin", "Scout", 30,
            [CombatantSkill.Create("skill.bite", "Bite", "Damage", "SingleEnemy", "Damage", 0, 0, 5)]);

        var node1 = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 0, []);
        var node2 = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 1, []);
        var node3 = MapNode.Create(NodeEventType.Combat, 30, "standard", 1, 0, [node1.Id], initialState: NodeState.Planned);
        var node4 = MapNode.Create(NodeEventType.Combat, 30, "standard", 1, 1, [node1.Id, node2.Id], initialState: NodeState.Planned);
        var node5 = MapNode.Create(NodeEventType.Combat, 25, "standard", 0, 2, []);
        var node6 = MapNode.Create(NodeEventType.Combat, 30, "standard", 1, 2, [node2.Id, node5.Id], initialState: NodeState.Planned);
        var bossNode = MapNode.Create(NodeEventType.RoomBoss, 50, "boss", 2, 1, [node3.Id, node4.Id, node6.Id], isBoss: true, initialState: NodeState.Planned);

        var bossProfile = RoomBossProfile.Create("boss-1", "Test Boss", RoomType.Memory, "A dark presence", "enemy.boss.1");
        var room = Room.Create(0, RoomType.Memory, "Test Room", bossProfile, [node1, node2, node3, node4, node5, node6, bossNode]);

        var run = Run.StartNew(Guid.NewGuid(), "test-seed", "1.0.0", "1.0.0", room, DateTimeOffset.UtcNow);

        var combatId = CombatId.New();
        var combat = Combat.Create(combatId, run.Id, room.Id, node1.Id, [ally], [enemy]);

        run.StartCombat(combat);

        return run;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
