using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class BestiaryUnlockRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly BestiaryUnlockRepository _repository;

    public BestiaryUnlockRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new BestiaryUnlockRepository(_context);
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
        // Seed des entités requises par les Include
        var profile = new PlayerProfile
        {
            Id = 1,
            UserId = 1,
            CharacterName = "Test",
            Level = 1,
            CurrentHP = 100,
            MaxHP = 100,
            CurrentMP = 50,
            MaxMP = 50,
            Strength = 10,
            Intelligence = 10,
            Speed = 10,
            Experience = 0,
            Gold = 0,
            UpdatedAt = DateTime.UtcNow
        };
        var enemies = new List<Enemy>
    {
        new Enemy { Id = 1, Name = "Goblin", Type = Constants.EnemyTypeBasic, MaxHP = 50, Strength = 5, Intelligence = 3, Speed = 8, ExperienceReward = 10, GoldReward = 5, PhysicalResistance = 1.0f, MagicalResistance = 1.0f },
        new Enemy { Id = 2, Name = "Orc",    Type = Constants.EnemyTypeBasic, MaxHP = 80, Strength = 10, Intelligence = 4, Speed = 6, ExperienceReward = 20, GoldReward = 10, PhysicalResistance = 1.0f, MagicalResistance = 1.0f },
    };
        await _context.PlayerProfiles.AddAsync(profile);
        await _context.Enemies.AddRangeAsync(enemies);
        await _context.SaveChangesAsync();

        var unlocks = new List<BestiaryUnlock>
    {
        new BestiaryUnlock { Id = 2, PlayerId = 1, EnemyId = 2 },
        new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 1 }
    };
        await _context.BestiaryUnlocks.AddRangeAsync(unlocks);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var unlock = new BestiaryUnlock { Id = 5, PlayerId = 10, EnemyId = 20 };
        await _context.BestiaryUnlocks.AddAsync(unlock);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.PlayerId.Should().Be(10);
        result.EnemyId.Should().Be(20);
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
        var newUnlock = new BestiaryUnlock { Id = 10, PlayerId = 1, EnemyId = 5 };

        await _repository.AddAsync(newUnlock);

        var dbRecord = await _context.BestiaryUnlocks.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.PlayerId.Should().Be(1);
        dbRecord.EnemyId.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var unlock = new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 1 };
        await _context.BestiaryUnlocks.AddAsync(unlock);
        await _context.SaveChangesAsync();

        // Détachement de l'entité pour simuler un contexte de mise à jour déconnecté
        _context.Entry(unlock).State = EntityState.Detached;

        var updatedUnlock = new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 99 };
        await _repository.UpdateAsync(updatedUnlock);

        var dbRecord = await _context.BestiaryUnlocks.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.EnemyId.Should().Be(99);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var unlock = new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 1 };
        await _context.BestiaryUnlocks.AddAsync(unlock);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.BestiaryUnlocks.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var unlock = new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 1 };
        await _context.BestiaryUnlocks.AddAsync(unlock);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.BestiaryUnlocks.CountAsync();
        remainingCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByPlayerAndEnemyAsync_ReturnsUnlock_WhenExists()
    {
        var unlock = new BestiaryUnlock { Id = 1, PlayerId = 1, EnemyId = 2 };
        await _context.BestiaryUnlocks.AddAsync(unlock);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByPlayerAndEnemyAsync(1, 2);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByPlayerAndEnemyAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _repository.GetByPlayerAndEnemyAsync(99, 99);

        result.Should().BeNull();
    }
}