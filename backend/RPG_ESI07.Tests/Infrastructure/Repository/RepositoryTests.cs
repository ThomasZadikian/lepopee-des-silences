using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repositories;

namespace RPG_ESI07.Tests.Infrastructure.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<Item> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // Utilisation de l'entité Item comme type générique T pour tester le comportement de base
        _repository = new Repository<Item>(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenIdExists()
    {
        var item = new Item { Id = 1, Name = "Sword", Type = "Weapon" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Sword");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var items = new List<Item>
        {
            new Item { Id = 1, Name = "Sword", Type = "Weapon" },
            new Item { Id = 2, Name = "Shield", Type = "Armor" }
        };
        await _context.Items.AddRangeAsync(items);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Id == 1);
        result.Should().Contain(x => x.Id == 2);
    }

    [Fact]
    public async Task AddAsync_AddsEntityAndSavesChanges()
    {
        var newItem = new Item { Id = 10, Name = "Bow", Type = "Weapon" };

        var returnedItem = await _repository.AddAsync(newItem);

        returnedItem.Should().NotBeNull();
        returnedItem.Id.Should().Be(10);

        var dbRecord = await _context.Items.FirstOrDefaultAsync(x => x.Id == 10);
        dbRecord.Should().NotBeNull();
        dbRecord!.Name.Should().Be("Bow");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesEntityAndSavesChanges()
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
    public async Task DeleteAsync_RemovesEntityAndSavesChanges()
    {
        var item = new Item { Id = 1, Name = "ToDelete", Type = "Junk" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        // Le repository générique demande l'entité entière, pas juste l'ID
        await _repository.DeleteAsync(item);

        var dbRecord = await _context.Items.FindAsync(1);
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenIdExists()
    {
        var item = new Item { Id = 5, Name = "Existing", Type = "Misc" };
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();

        var result = await _repository.ExistsAsync(5);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenIdDoesNotExist()
    {
        var result = await _repository.ExistsAsync(999);

        result.Should().BeFalse();
    }
}