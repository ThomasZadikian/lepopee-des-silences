using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class EnemyRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly EnemyRepository _repository;

    public EnemyRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new EnemyRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecords_OrderedByTypeThenMaxHP()
    {
        var enemies = new List<Enemy>
        {
            new Enemy { Id = 1, Name = "Dragon", Type = Constants.EnemyTypeBoss, MaxHP = 500 },
            new Enemy { Id = 2, Name = "Troll", Type = Constants.EnemyTypeBoss, MaxHP = 300 },
            new Enemy { Id = 3, Name = "Goblin", Type = Constants.EnemyTypeBasic, MaxHP = 100 }
        };
        await _context.Enemies.AddRangeAsync(enemies);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(3); // Type: basic, MaxHP: 100
        result[1].Id.Should().Be(2); // Type: boss, MaxHP: 300
        result[2].Id.Should().Be(1); // Type: boss, MaxHP: 500
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsMatchingRecords_CaseInsensitive()
    {
        var enemies = new List<Enemy>
        {
            new Enemy { Id = 1, Name = "Dragon", Type = "Boss" },
            new Enemy { Id = 2, Name = "Goblin", Type = Constants.EnemyTypeBasic },
            new Enemy { Id = 3, Name = "Troll", Type = Constants.EnemyTypeBoss }
        };
        await _context.Enemies.AddRangeAsync(enemies);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByTypeAsync("bOsS");

        result.Should().HaveCount(2);
        result.Should().ContainSingle(e => e.Id == 1);
        result.Should().ContainSingle(e => e.Id == 3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var enemy = new Enemy { Id = 5, Name = "Wolf", Type = Constants.EnemyTypeBasic };
        await _context.Enemies.AddAsync(enemy);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("Wolf");
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
        var newEnemy = new Enemy { Id = 10, Name = "Slime", Type = Constants.EnemyTypeBasic };

        await _repository.AddAsync(newEnemy);

        var dbRecord = await _context.Enemies.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("Slime");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var enemy = new Enemy { Id = 1, Name = "OldName", Type = Constants.EnemyTypeBasic };
        await _context.Enemies.AddAsync(enemy);
        await _context.SaveChangesAsync();

        _context.Entry(enemy).State = EntityState.Detached;

        var updatedEnemy = new Enemy { Id = 1, Name = "NewName", Type = Constants.EnemyTypeBasic };
        await _repository.UpdateAsync(updatedEnemy);

        var dbRecord = await _context.Enemies.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var enemy = new Enemy { Id = 1, Name = "ToDelete", Type = Constants.EnemyTypeBasic };
        await _context.Enemies.AddAsync(enemy);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.Enemies.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var enemy = new Enemy { Id = 1, Name = "ToKeep", Type = Constants.EnemyTypeBasic };
        await _context.Enemies.AddAsync(enemy);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.Enemies.CountAsync();
        remainingCount.Should().Be(1);
    }
}