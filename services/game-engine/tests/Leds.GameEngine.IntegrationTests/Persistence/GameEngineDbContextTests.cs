using FluentAssertions;
using Leds.GameEngine.Infrastructure.Persistence;
using Leds.GameEngine.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Leds.GameEngine.IntegrationTests.Persistence;

public sealed class GameEngineDbContextTests : IDisposable
{
    private readonly string _databaseName;
    private readonly GameEngineDbContext _context;

    public GameEngineDbContextTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<GameEngineDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;

        _context = new GameEngineDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GameEngineDbContext_ShouldPersistRunEntity()
    {
        var runId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entity = new RunEntity
        {
            Id = runId,
            PlayerId = playerId,
            Status = "Active",
            Seed = "test-seed-12345",
            GeneratorVersion = "1.0.0",
            MarkovMatrixVersion = "1.0.0",
            CurrentDepth = 3,
            ActiveCombatId = null,
            PendingRewardOfferId = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Runs.Add(entity);
        await _context.SaveChangesAsync();

        using var verifyContext = new GameEngineDbContext(
            new DbContextOptionsBuilder<GameEngineDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options);

        var loaded = await verifyContext.Runs.FindAsync(runId);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(runId);
        loaded.PlayerId.Should().Be(playerId);
        loaded.Status.Should().Be("Active");
        loaded.Seed.Should().Be("test-seed-12345");
        loaded.GeneratorVersion.Should().Be("1.0.0");
        loaded.MarkovMatrixVersion.Should().Be("1.0.0");
        loaded.CurrentDepth.Should().Be(3);
        loaded.ActiveCombatId.Should().BeNull();
        loaded.PendingRewardOfferId.Should().BeNull();
        loaded.CreatedAtUtc.Should().Be(now);
        loaded.UpdatedAtUtc.Should().Be(now);
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
            CurrentDepth = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _context.Runs.Add(entity);
        await _context.SaveChangesAsync();

        entity.Status = "Completed";
        entity.CurrentDepth = 5;
        entity.UpdatedAtUtc = now.AddMinutes(10);
        await _context.SaveChangesAsync();

        var loaded = await _context.Runs.FindAsync(runId);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be("Completed");
        loaded.CurrentDepth.Should().Be(5);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
