using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class CombatStatsRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CombatStatsRepository _repository;

    public CombatStatsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new CombatStatsRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRecords_OrderedById()
    {
        var stats = new List<CombatStats>
        {
            new CombatStats { Id = 2, PlayerId = 2, TotalCombats = 50 },
            new CombatStats { Id = 1, PlayerId = 1, TotalCombats = 10 }
        };
        await _context.CombatStats.AddRangeAsync(stats);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(i => i.Id == 1);
        result.Should().ContainSingle(i => i.Id == 2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var stats = new CombatStats { Id = 5, PlayerId = 10, TotalCombats = 100 };
        await _context.CombatStats.AddAsync(stats);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.PlayerId.Should().Be(10);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_InsertsRecordAndSavesChanges()
    {
        var newStats = new CombatStats { Id = 10, PlayerId = 5, TotalCombats = 0 };

        await _repository.AddAsync(newStats);

        var dbRecord = await _context.CombatStats.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.PlayerId.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var stats = new CombatStats { Id = 1, PlayerId = 1, TotalCombats = 10 };
        await _context.CombatStats.AddAsync(stats);
        await _context.SaveChangesAsync();

        _context.Entry(stats).State = EntityState.Detached;

        var updatedStats = new CombatStats { Id = 1, PlayerId = 1, TotalCombats = 15 };
        await _repository.UpdateAsync(updatedStats);

        var dbRecord = await _context.CombatStats.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.TotalCombats.Should().Be(15);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var stats = new CombatStats { Id = 1, PlayerId = 1, TotalCombats = 10 };
        await _context.CombatStats.AddAsync(stats);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.CombatStats.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var stats = new CombatStats { Id = 1, PlayerId = 1, TotalCombats = 10 };
        await _context.CombatStats.AddAsync(stats);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.CombatStats.CountAsync();
        remainingCount.Should().Be(1);
    }
}