using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class PlayerInventoryRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PlayerInventoryRepository _repository;

    public PlayerInventoryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new PlayerInventoryRepository(_context);
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
        // Ajouter les Items requis
        var items = new List<Item>
    {
        new Item { Id = 3, Name = "Item3", Type = Constants.ItemTypeWeapon, Price = 10 },
        new Item { Id = 5, Name = "Item5", Type = Constants.ItemTypeConsumable, Price = 5 },
    };
        await _context.Items.AddRangeAsync(items);

        var inventories = new List<PlayerInventory>
    {
        new PlayerInventory { Id = 2, PlayerId = 1, ItemId = 5, Quantity = 10 },
        new PlayerInventory { Id = 1, PlayerId = 1, ItemId = 3, Quantity = 5 }
    };
        await _context.PlayerInventory.AddRangeAsync(inventories);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenIdExists()
    {
        var item = new Item { Id = 2, Name = "Item2", Type = Constants.ItemTypeArmor, Price = 100 };
        await _context.Items.AddAsync(item);

        var inventory = new PlayerInventory { Id = 5, PlayerId = 10, ItemId = 2, Quantity = 1 };
        await _context.PlayerInventory.AddAsync(inventory);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(5);

        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.PlayerId.Should().Be(10);
        result.ItemId.Should().Be(2);
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
        var newInventory = new PlayerInventory { Id = 10, PlayerId = 5, ItemId = 7, Quantity = 3 };

        await _repository.AddAsync(newInventory);

        var dbRecord = await _context.PlayerInventory.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.PlayerId.Should().Be(5);
        dbRecord.ItemId.Should().Be(7);
        dbRecord.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesRecordAndSavesChanges()
    {
        var inventory = new PlayerInventory { Id = 1, PlayerId = 1, ItemId = 1, Quantity = 5 };
        await _context.PlayerInventory.AddAsync(inventory);
        await _context.SaveChangesAsync();

        _context.Entry(inventory).State = EntityState.Detached;

        var updatedInventory = new PlayerInventory { Id = 1, PlayerId = 1, ItemId = 1, Quantity = 15 };
        await _repository.UpdateAsync(updatedInventory);

        var dbRecord = await _context.PlayerInventory.FindAsync(1);
        dbRecord.Should().NotBeNull();
        dbRecord!.Quantity.Should().Be(15);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRecord_WhenIdExists()
    {
        var inventory = new PlayerInventory { Id = 1, PlayerId = 1, ItemId = 1, Quantity = 1 };
        await _context.PlayerInventory.AddAsync(inventory);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(1);

        var dbRecord = await _context.PlayerInventory.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenIdDoesNotExist()
    {
        var inventory = new PlayerInventory { Id = 1, PlayerId = 1, ItemId = 1, Quantity = 1 };
        await _context.PlayerInventory.AddAsync(inventory);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(999);

        var remainingCount = await _context.PlayerInventory.CountAsync();
        remainingCount.Should().Be(1);
    }
}