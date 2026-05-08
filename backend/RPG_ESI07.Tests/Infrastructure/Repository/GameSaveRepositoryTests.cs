using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure;

public class GameSaveRepositoryTests
{
    private AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private GameSave CreateGameSave(int playerId = 1) => new GameSave
    {
        PlayerId = playerId,
        CurrentZone = "Tutorial",
        PositionX = 10f,
        PositionY = 20f,
        QuestFlags = "{}",
        SavedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_ShouldPersistGameSave()
    {
        using var context = CreateContext(nameof(AddAsync_ShouldPersistGameSave));
        var repo = new GameSaveRepository(context);
        var save = CreateGameSave();

        await repo.AddAsync(save);

        var result = await context.GameSaves.FirstOrDefaultAsync();
        result.Should().NotBeNull();
        result!.CurrentZone.Should().Be("Tutorial");
        result.PositionX.Should().Be(10f);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectSave()
    {
        using var context = CreateContext(nameof(GetByIdAsync_ShouldReturnCorrectSave));
        var repo = new GameSaveRepository(context);
        var save = CreateGameSave();
        await repo.AddAsync(save);

        var result = await repo.GetByIdAsync(save.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(save.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        using var context = CreateContext(nameof(GetByIdAsync_WithInvalidId_ShouldReturnNull));
        var repo = new GameSaveRepository(context);

        var result = await repo.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllSaves()
    {
        using var context = CreateContext(nameof(GetAllAsync_ShouldReturnAllSaves));
        var repo = new GameSaveRepository(context);
        await repo.AddAsync(CreateGameSave(1));
        await repo.AddAsync(CreateGameSave(2));
        await repo.AddAsync(CreateGameSave(3));

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        using var context = CreateContext(nameof(GetAllAsync_WhenEmpty_ShouldReturnEmptyList));
        var repo = new GameSaveRepository(context);

        var result = await repo.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingSave()
    {
        using var context = CreateContext(nameof(UpdateAsync_ShouldModifyExistingSave));
        var repo = new GameSaveRepository(context);
        var save = CreateGameSave();
        await repo.AddAsync(save);

        save.CurrentZone = "BossFinal";
        save.PositionX = 99f;
        await repo.UpdateAsync(save);

        var updated = await repo.GetByIdAsync(save.Id);
        updated!.CurrentZone.Should().Be("BossFinal");
        updated.PositionX.Should().Be(99f);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSave()
    {
        using var context = CreateContext(nameof(DeleteAsync_ShouldRemoveSave));
        var repo = new GameSaveRepository(context);
        var save = CreateGameSave();
        await repo.AddAsync(save);

        await repo.DeleteAsync(save.Id);

        var result = await repo.GetByIdAsync(save.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldNotThrow()
    {
        using var context = CreateContext(nameof(DeleteAsync_WithInvalidId_ShouldNotThrow));
        var repo = new GameSaveRepository(context);

        var act = async () => await repo.DeleteAsync(999);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSavesOrderedById()
    {
        using var context = CreateContext(nameof(GetAllAsync_ShouldReturnSavesOrderedById));
        var repo = new GameSaveRepository(context);
        await repo.AddAsync(CreateGameSave(3));
        await repo.AddAsync(CreateGameSave(1));
        await repo.AddAsync(CreateGameSave(2));

        var result = await repo.GetAllAsync();

        result.Should().BeInAscendingOrder(s => s.Id);
    }
}