using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class CharacterRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CharacterRepository _repository;

    public CharacterRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new CharacterRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecords_OrderedByLevelThenExperience()
    {
        var profiles = new List<PlayerProfile>
        {
            new PlayerProfile { Id = 1, CharacterName = "A", Level = 10, Experience = 500 },
            new PlayerProfile { Id = 2, CharacterName = "B", Level = 5, Experience = 100 },
            new PlayerProfile { Id = 3, CharacterName = "C", Level = 5, Experience = 50 }
        };
        await _context.PlayerProfiles.AddRangeAsync(profiles);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(3); // Level 5, Exp 50
        result[1].Id.Should().Be(2); // Level 5, Exp 100
        result[2].Id.Should().Be(1); // Level 10, Exp 500
    }

    [Fact]
    public async Task GetBySpeedAsync_ReturnsRecords_OrderedBySpeed()
    {
        var profiles = new List<PlayerProfile>
        {
            new PlayerProfile { Id = 1, CharacterName = "A", Speed = 15 },
            new PlayerProfile { Id = 2, CharacterName = "B", Speed = 5 },
            new PlayerProfile { Id = 3, CharacterName = "C", Speed = 10 }
        };
        await _context.PlayerProfiles.AddRangeAsync(profiles);
        await _context.SaveChangesAsync();

        var result = await _repository.GetBySpeedAsync();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(2); // Speed 5
        result[1].Id.Should().Be(3); // Speed 10
        result[2].Id.Should().Be(1); // Speed 15
    }

    [Fact]
    public async Task GetByLevelAsync_ReturnsAllRecords_OrderedByLevel_IgnoringParameter()
    {
        var profiles = new List<PlayerProfile>
        {
            new PlayerProfile { Id = 1, CharacterName = "A", Level = 20 },
            new PlayerProfile { Id = 2, CharacterName = "B", Level = 10 }
        };
        await _context.PlayerProfiles.AddRangeAsync(profiles);
        await _context.SaveChangesAsync();

        // Le paramètre 99 n'a aucun impact selon l'implémentation actuelle
        var result = await _repository.GetByLevelAsync(99);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(2); // Level 10
        result[1].Id.Should().Be(1); // Level 20
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var profile = new PlayerProfile { Id = 5, CharacterName = "Hero", Level = 1 };
        await _context.PlayerProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.CharacterName.Should().Be("Hero");
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
        var newProfile = new PlayerProfile { Id = 10, CharacterName = "NewHero", Level = 1 };

        await _repository.AddAsync(newProfile);

        var dbRecord = await _context.PlayerProfiles.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.CharacterName.Should().Be("NewHero");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var profile = new PlayerProfile { Id = 1, CharacterName = "OldName", Level = 1 };
        await _context.PlayerProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();

        _context.Entry(profile).State = EntityState.Detached;

        var updatedProfile = new PlayerProfile { Id = 1, CharacterName = "NewName", Level = 1 };
        await _repository.UpdateAsync(updatedProfile);

        var dbRecord = await _context.PlayerProfiles.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.CharacterName.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var profile = new PlayerProfile { Id = 1, CharacterName = "ToDelete", Level = 1 };
        await _context.PlayerProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.PlayerProfiles.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var profile = new PlayerProfile { Id = 1, CharacterName = "ToKeep", Level = 1 };
        await _context.PlayerProfiles.AddAsync(profile);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.PlayerProfiles.CountAsync();
        remainingCount.Should().Be(1);
    }
}