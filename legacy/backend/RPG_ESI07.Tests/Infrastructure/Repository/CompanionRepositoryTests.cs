using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class CompanionRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CompanionRepository _repository;

    public CompanionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new CompanionRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetCurrentStateAsync_ReturnsNull_WhenNoStateExists()
    {
        var result = await _repository.GetCurrentStateAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentStateAsync_ReturnsState_WhenExists()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow,
            LastCombatAt = DateTime.UtcNow,
            VictoriesToday = 5,
            DefeatesToday = 2
        };
        _context.CompanionStates.Add(state);
        await _context.SaveChangesAsync();

        var result = await _repository.GetCurrentStateAsync();

        result.Should().NotBeNull();
        result!.CurrentState.Should().Be(Constants.CompanionStateRepos);
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesState_WhenNoneExists()
    {
        var result = await _repository.GetOrCreateAsync();

        result.Should().NotBeNull();
        result.CurrentState.Should().Be(Constants.CompanionStateRepos);

        var count = await _context.CompanionStates.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsExisting_WhenStateExists()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateExcite,
            LastUpdated = DateTime.UtcNow,
            LastCombatAt = DateTime.UtcNow,
            VictoriesToday = 15,
            DefeatesToday = 0
        };
        _context.CompanionStates.Add(state);
        await _context.SaveChangesAsync();

        var result = await _repository.GetOrCreateAsync();

        result.CurrentState.Should().Be(Constants.CompanionStateExcite);
        var count = await _context.CompanionStates.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateStateAsync_SavesChanges()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow,
            LastCombatAt = DateTime.UtcNow,
            VictoriesToday = 0,
            DefeatesToday = 0
        };
        _context.CompanionStates.Add(state);
        await _context.SaveChangesAsync();

        state.CurrentState = Constants.CompanionStateExcite;
        state.VictoriesToday = 20;
        await _repository.UpdateStateAsync(state);

        var updated = await _context.CompanionStates.FirstOrDefaultAsync();
        updated!.CurrentState.Should().Be(Constants.CompanionStateExcite);
        updated.VictoriesToday.Should().Be(20);
    }
}