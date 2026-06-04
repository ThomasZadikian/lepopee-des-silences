using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class NpcRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly NpcRepository _repository;

    public NpcRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new NpcRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecords_OrderedByZoneThenName()
    {
        var npcs = new List<Npc>
        {
            new() { Id = 1, Name = "Zara", Zone = "Dev Island", Type = Constants.NpcTypeNeutral },
            new() { Id = 2, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeMerchant },
            new() { Id = 3, Name = "Boris", Zone = "BossFinal", Type = Constants.NpcTypeNeutral }
        };
        await _context.Npcs.AddRangeAsync(npcs);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(3); // Zone: BossFinal
        result[1].Id.Should().Be(2); // Zone: Dev Island, Name: Aldric
        result[2].Id.Should().Be(1); // Zone: Dev Island, Name: Zara
    }

    [Fact]
    public async Task GetByZoneAsync_ReturnsMatchingRecords_CaseInsensitive()
    {
        var npcs = new List<Npc>
        {
            new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral },
            new() { Id = 2, Name = "Boris", Zone = "BossFinal", Type = Constants.NpcTypeNeutral }
        };
        await _context.Npcs.AddRangeAsync(npcs);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByZoneAsync("dev island");

        result.Should().HaveCount(1);
        result.Should().ContainSingle(n => n.Id == 1);
    }

    [Fact]
    public async Task GetByTypeAsync_ReturnsMatchingRecords_CaseInsensitive()
    {
        var npcs = new List<Npc>
        {
            new() { Id = 1, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeMerchant },
            new() { Id = 2, Name = "Boris", Zone = "Dev Island", Type = Constants.NpcTypeNeutral }
        };
        await _context.Npcs.AddRangeAsync(npcs);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByTypeAsync("MERCHANT");

        result.Should().HaveCount(1);
        result.Should().ContainSingle(n => n.Id == 1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var npc = new Npc { Id = 5, Name = "Aldric", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        await _context.Npcs.AddAsync(npc);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("Aldric");
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
        var npc = new Npc { Id = 10, Name = "Zara", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };

        await _repository.AddAsync(npc);

        var dbRecord = await _context.Npcs.FirstOrDefaultAsync(n => n.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("Zara");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var npc = new Npc { Id = 1, Name = "OldName", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        await _context.Npcs.AddAsync(npc);
        await _context.SaveChangesAsync();
        _context.Entry(npc).State = EntityState.Detached;

        var updated = new Npc { Id = 1, Name = "NewName", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        await _repository.UpdateAsync(updated);

        var dbRecord = await _context.Npcs.FindAsync(1);
        dbRecord!.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var npc = new Npc { Id = 1, Name = "ToDelete", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        await _context.Npcs.AddAsync(npc);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.Npcs.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var npc = new Npc { Id = 1, Name = "ToKeep", Zone = "Dev Island", Type = Constants.NpcTypeNeutral };
        await _context.Npcs.AddAsync(npc);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var count = await _context.Npcs.CountAsync();
        count.Should().Be(1);
    }
}