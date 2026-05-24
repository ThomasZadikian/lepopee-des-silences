using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class NpcInteractionRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly NpcInteractionRepository _repository;

    public NpcInteractionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new NpcInteractionRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedBaseDataAsync()
    {
        var npc = new Npc { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        var player = new PlayerProfile { Id = 1, CharacterName = "Elara", UserId = 1 };
        var user = new User { Id = 1, Username = "devuser", PasswordHash = "hash", Role = Constants.RolePlayer };

        await _context.Users.AddAsync(user);
        await _context.PlayerProfiles.AddAsync(player);
        await _context.Npcs.AddAsync(npc);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecords_OrderedByInteractedAtDescending()
    {
        await SeedBaseDataAsync();

        var interactions = new List<NpcInteraction>
        {
            new() { Id = 1, NpcId = 1, PlayerId = 1, EventType = "DIALOGUE", InteractedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Id = 2, NpcId = 1, PlayerId = 1, EventType = "TRADE",    InteractedAt = DateTime.UtcNow.AddHours(-1) },
        };
        await _context.NpcInteractions.AddRangeAsync(interactions);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(2);
        result[1].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByPlayerIdAsync_ReturnsOnlyPlayerInteractions()
    {
        await SeedBaseDataAsync();

        var interactions = new List<NpcInteraction>
        {
            new() { Id = 1, NpcId = 1, PlayerId = 1, EventType = "DIALOGUE", InteractedAt = DateTime.UtcNow },
            new() { Id = 2, NpcId = 1, PlayerId = 1, EventType = "TRADE",    InteractedAt = DateTime.UtcNow },
        };
        await _context.NpcInteractions.AddRangeAsync(interactions);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByPlayerIdAsync(1);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.PlayerId.Should().Be(1));
    }

    [Fact]
    public async Task GetByNpcIdAsync_ReturnsOnlyNpcInteractions()
    {
        await SeedBaseDataAsync();

        var interaction = new NpcInteraction { Id = 1, NpcId = 1, PlayerId = 1, EventType = "DIALOGUE", InteractedAt = DateTime.UtcNow };
        await _context.NpcInteractions.AddAsync(interaction);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNpcIdAsync(1);

        result.Should().HaveCount(1);
        result[0].NpcId.Should().Be(1);
    }

    [Fact]
    public async Task AddAsync_InsertsRecordAndSavesChanges()
    {
        await SeedBaseDataAsync();

        var interaction = new NpcInteraction { Id = 1, NpcId = 1, PlayerId = 1, EventType = "DIALOGUE", InteractedAt = DateTime.UtcNow };
        await _repository.AddAsync(interaction);

        var dbRecord = await _context.NpcInteractions.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.EventType.Should().Be("DIALOGUE");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        await SeedBaseDataAsync();

        var interaction = new NpcInteraction { Id = 1, NpcId = 1, PlayerId = 1, EventType = "DIALOGUE", InteractedAt = DateTime.UtcNow };
        await _context.NpcInteractions.AddAsync(interaction);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.NpcInteractions.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        await SeedBaseDataAsync();

        var interaction = new NpcInteraction { Id = 1, NpcId = 1, PlayerId = 1, EventType = "DIALOGUE", InteractedAt = DateTime.UtcNow };
        await _context.NpcInteractions.AddAsync(interaction);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var count = await _context.NpcInteractions.CountAsync();
        count.Should().Be(1);
    }
}