using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class ItemRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ItemRepository _repository;

    public ItemRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new ItemRepository(_context);
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
        var items = new List<Item>
        {
            new Item { Id = 2, Name = "Shield", Type = "Armor" },
            new Item { Id = 1, Name = "Sword", Type = "Weapon" }
        };
        await _context.Items.AddRangeAsync(items);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var item = new Item { Id = 5, Name = "Potion", Type = "Consumable" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("Potion");
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
        var newItem = new Item { Id = 10, Name = "Bow", Type = "Weapon" };

        await _repository.AddAsync(newItem);

        var dbRecord = await _context.Items.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("Bow");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var item = new Item { Id = 1, Name = "OldName", Type = "Weapon" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        _context.Entry(item).State = EntityState.Detached;

        var updatedItem = new Item { Id = 1, Name = "NewName", Type = "Weapon" };
        await _repository.UpdateAsync(updatedItem);

        var dbRecord = await _context.Items.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("NewName");
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var item = new Item { Id = 1, Name = "ToDelete", Type = "Junk" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.Items.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var item = new Item { Id = 1, Name = "ToKeep", Type = "Valuable" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.Items.CountAsync();
        remainingCount.Should().Be(1);
    }
}