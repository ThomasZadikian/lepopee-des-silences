using FluentAssertions;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Entities;

namespace Leds.GameEngine.IntegrationTests.Persistence;

[Collection("GameEnginePostgres")]
public sealed class GameEngineDbContextTests : IDisposable
{
    private readonly string _connStr;
    private readonly GameEngineDbContext _context;
    private readonly GameEnginePostgresFixture _fixture;

    public GameEngineDbContextTests(GameEnginePostgresFixture fixture)
    {
        _fixture = fixture;
        (_context, _connStr) = fixture.CreateContext();
    }

    [Fact]
    public async Task GameEngineDbContext_ShouldPersistRunEntity()
    {
        var runId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entity = new RunEntity
        {
            Id = runId,
            PlayerId = playerId,
            Status = "Active",
            Seed = "test-seed-12345",
            GeneratorVersion = "1.0.0",
            MarkovMatrixVersion = "1.0.0",
            CurrentRoomId = roomId,
            CurrentRoomIndex = 0,
            MaxHp = 40,
            CurrentHp = 40,
            Attack = 12,
            Defense = 6,
            Speed = 10,
            StartedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Runs.Add(entity);
        await _context.SaveChangesAsync();

        using var verifyContext = _fixture.CreateContext(_connStr);

        var loaded = await verifyContext.Runs.FindAsync(runId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(runId);
        loaded.PlayerId.Should().Be(playerId);
        loaded.Status.Should().Be("Active");
        loaded.Seed.Should().Be("test-seed-12345");
        loaded.GeneratorVersion.Should().Be("1.0.0");
        loaded.MarkovMatrixVersion.Should().Be("1.0.0");
        loaded.CurrentRoomId.Should().Be(roomId);
        loaded.CurrentRoomIndex.Should().Be(0);
        loaded.MaxHp.Should().Be(40);
        loaded.CurrentHp.Should().Be(40);
        loaded.Attack.Should().Be(12);
        loaded.Defense.Should().Be(6);
        loaded.Speed.Should().Be(10);
        loaded.ActiveCombatId.Should().BeNull();
        loaded.PendingRewardOfferId.Should().BeNull();
        loaded.CreatedAtUtc.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
        loaded.UpdatedAtUtc.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task GameEngineDbContext_ShouldUpdateRunEntity()
    {
        var runId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entity = new RunEntity
        {
            Id = runId,
            PlayerId = Guid.NewGuid(),
            Status = "Active",
            Seed = "test-seed",
            GeneratorVersion = "1.0.0",
            MarkovMatrixVersion = "1.0.0",
            CurrentRoomId = Guid.NewGuid(),
            CurrentRoomIndex = 0,
            MaxHp = 40,
            CurrentHp = 40,
            Attack = 12,
            Defense = 6,
            Speed = 10,
            StartedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Runs.Add(entity);
        await _context.SaveChangesAsync();

        entity.Status = "Resolved";
        entity.Outcome = "Success";
        entity.CurrentRoomIndex = 3;
        entity.CurrentHp = 25;
        entity.UpdatedAtUtc = now.AddMinutes(10);
        await _context.SaveChangesAsync();

        var loaded = await _context.Runs.FindAsync(runId);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be("Resolved");
        loaded.Outcome.Should().Be("Success");
        loaded.CurrentRoomIndex.Should().Be(3);
        loaded.CurrentHp.Should().Be(25);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
