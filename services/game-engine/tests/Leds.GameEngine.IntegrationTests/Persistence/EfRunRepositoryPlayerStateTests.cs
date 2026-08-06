using FluentAssertions;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Repositories;

namespace Leds.GameEngine.IntegrationTests.Persistence;

[Collection("GameEnginePostgres")]
public sealed class EfRunRepositoryPlayerStateTests : IDisposable
{
    private readonly string _connStr;
    private readonly GameEngineDbContext _context;
    private readonly EfRunRepository _repository;
    private readonly GameEnginePostgresFixture _fixture;

    public EfRunRepositoryPlayerStateTests(GameEnginePostgresFixture fixture)
    {
        _fixture = fixture;
        (_context, _connStr) = fixture.CreateContext();
        _repository = new EfRunRepository(_context);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRestoreEquivalentPlayerRuntimeState()
    {
        var run = CreateTestRun();

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded.Should().NotBeNull();
        var loadedState = loaded!.PlayerState;
        var originalState = run.PlayerState;

        loadedState.Should().NotBeNull();
        loadedState!.MaxVitality.Should().Be(originalState.MaxVitality);
        loadedState.CurrentVitality.Should().Be(originalState.CurrentVitality);
        loadedState.Guard.Should().Be(originalState.Guard);
        loadedState.Mana.Should().Be(originalState.Mana);
        loadedState.Charge.Should().Be(originalState.Charge);
        loadedState.Skills.Should().HaveCount(originalState.Skills.Count);

        foreach (var originalSkill in originalState.Skills)
        {
            var loadedSkill = loadedState.Skills.FirstOrDefault(s => s.Key == originalSkill.Key);
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
    public async Task SaveAndLoad_ShouldPersistPlayerVitalityAfterDamage()
    {
        var run = CreateTestRun();
        run.PlayerState.TakeDamage(10);

        await _repository.AddAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded!.PlayerState.CurrentVitality.Should().Be(30);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistUpdatedPlayerState()
    {
        var run = CreateTestRun();
        await _repository.AddAsync(run, CancellationToken.None);

        run.PlayerState.TakeDamage(15);
        run.PlayerState.GainGuard(10);

        await _repository.UpdateAsync(run, CancellationToken.None);

        using var verifyContext = _fixture.CreateContext(_connStr);

        var verifyRepository = new EfRunRepository(verifyContext);
        var loaded = await verifyRepository.GetByIdAsync(run.Id, CancellationToken.None);

        loaded!.PlayerState.CurrentVitality.Should().Be(25);
        loaded.PlayerState.Guard.Should().Be(10);
    }

    private static Run CreateTestRun()
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
            Guid.NewGuid(),
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
