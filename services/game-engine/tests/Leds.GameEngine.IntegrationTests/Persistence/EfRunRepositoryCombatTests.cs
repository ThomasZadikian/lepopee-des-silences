using FluentAssertions;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Combats.Tactical;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;

namespace Leds.GameEngine.IntegrationTests.Persistence;

[Collection("GameEnginePostgres")]
public sealed class EfRunRepositoryCombatTests : IDisposable
{
    private readonly string _connStr;
    private readonly GameEngineDbContext _context;
    private readonly EfRunRepository _repository;
    private readonly GameEnginePostgresFixture _fixture;

    public EfRunRepositoryCombatTests(GameEnginePostgresFixture fixture)
    {
        _fixture = fixture;
        (_context, _connStr) = fixture.CreateContext();
        _repository = new EfRunRepository(_context);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRestoreEquivalentActiveCombat()
    {
        var run = CreateTestRunWithCombat();
        var combat = run.ActiveTacticalCombat!;

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ActiveTacticalCombat.Should().NotBeNull();
        loaded.ActiveCombatId.Should().NotBeNull();

        var loadedCombat = loaded.ActiveTacticalCombat!;
        loadedCombat.Id.Should().Be(combat.Id);
        loadedCombat.RunId.Should().Be(combat.RunId);
        loadedCombat.RoomId.Should().Be(combat.RoomId);
        loadedCombat.NodeId.Should().Be(combat.NodeId);
        loadedCombat.Status.Should().Be(combat.Status);
        loadedCombat.TurnNumber.Should().Be(combat.TurnNumber);
        loadedCombat.ActiveCombatantId.Should().Be(combat.ActiveCombatantId);
        loadedCombat.CreatedAtUtc.Should().BeCloseTo(combat.CreatedAtUtc, TimeSpan.FromMilliseconds(1));

        loadedCombat.Allies.Should().HaveCount(combat.Allies.Count);
        loadedCombat.Enemies.Should().HaveCount(combat.Enemies.Count);

        foreach (var originalAlly in combat.Allies)
        {
            var loadedAlly = loadedCombat.Allies.FirstOrDefault(a => a.Id == originalAlly.Id);
            loadedAlly.Should().NotBeNull();
            loadedAlly!.SourceKey.Should().Be(originalAlly.SourceKey);
            loadedAlly.SourceDefinitionKey.Should().Be(originalAlly.SourceDefinitionKey);
            loadedAlly.CharacterInstanceId.Should().Be(originalAlly.CharacterInstanceId);
            loadedAlly.PersistentEmotionalAffinityModifiers.Should().BeEquivalentTo(
                originalAlly.PersistentEmotionalAffinityModifiers);
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
        var combat = run.ActiveTacticalCombat!;
        var ally = combat.Allies.First();

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        var loadedAlly = loaded!.ActiveTacticalCombat!.Allies.First();
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
        var node1 = run.CurrentRoom.Nodes.First(n => n.Row == 0 && n.Lane == 1);
        run.MoveParty(node1.Lane, node1.Row);
        await _repository.AddAsync(run, CancellationToken.None);

        foreach (var enemy in run.ActiveTacticalCombat!.Enemies)
            enemy.ApplyDamage(999);
        run.ActiveTacticalCombat!.CompleteIfAllEnemiesDefeated();
        run.CompleteActiveCombat();
        await _repository.UpdateAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.ActiveTacticalCombat.Should().BeNull();
        loaded.ActiveCombatId.Should().BeNull();
    }

    private static Run CreateTestRunWithCombat()
    {
        var ally = Combatant.CreateAlly("runtime.player.test", "Hero", "Warrior", 100, 0,
            [CombatantSkill.Create("skill.strike", "Strike", "Damage", "SingleEnemy", "Damage", 0, 0, 10,
                emotionalRegister: "Neutral")],
            naturalEmotionalType: EmotionalType.Memoire,
            characterInstanceId: Guid.NewGuid(),
            sourceDefinitionKey: "character.player.test");
        ally.ApplyEmotionalAffinityModifier(EmotionalAffinityModifier.Create(
            "item.test:AffinityOutcomeOverride", EmotionalType.Effroi,
            DamageEffectiveness.Resistant, priority: 20, durationActivations: 3));

        var enemy = Combatant.CreateEnemy("enemy.test", "Goblin", "Scout", 30,
            [CombatantSkill.Create("skill.bite", "Bite", "Damage", "SingleEnemy", "Damage", 0, 0, 5,
                emotionalRegister: "Neutral")]);

        var node1 = MapNode.Create(NodeEventType.Combat, 25, "standard", row: 0, lane: 1, []);
        var node2 = MapNode.Create(NodeEventType.Combat, 25, "standard", row: 0, lane: 2, []);
        var node3 = MapNode.Create(NodeEventType.Combat, 30, "standard", row: 1, lane: 1, []);
        var node4 = MapNode.Create(NodeEventType.Combat, 30, "standard", row: 1, lane: 2, []);
        var node5 = MapNode.Create(NodeEventType.Combat, 25, "standard", row: 0, lane: 3, []);
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

        var run = Run.StartNew(Guid.NewGuid(), "test-seed", "1.0.0", "1.0.0", room, DateTimeOffset.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.IntegrationTests.Common.TestEmotionalAffinityMatrix.Create());

        var battlefield = TacticalBattlefield.Rehydrate(
            4, 2,
            Enumerable.Repeat(0, 8).ToArray(),
            Enumerable.Repeat(true, 8).ToArray(),
            Enumerable.Repeat(true, 8).ToArray());

        var allyPosition = new GridPosition(0, 0);
        var enemyPosition = new GridPosition(3, 0);
        var combat = TacticalCombat.Create(
            CombatId.New(),
            run.Id,
            room.Id,
            node1.Id,
            battlefield,
            [(ally, allyPosition)],
            [(enemy, enemyPosition)],
            DateTime.UtcNow,
            emotionalAffinityMatrix: Leds.GameEngine.IntegrationTests.Common.TestEmotionalAffinityMatrix.Create());

        run.StartTacticalCombat(combat);

        return run;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
