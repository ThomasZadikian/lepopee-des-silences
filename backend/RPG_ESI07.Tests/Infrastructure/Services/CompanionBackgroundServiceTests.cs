using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Services;

namespace RPG_ESI07.Tests.Infrastructure.Services;

public class CompanionBackgroundServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICompanionRepository> _companionRepoMock;
    private readonly ServiceProvider _serviceProvider;
    private readonly CompanionBackgroundService _service;

    public CompanionBackgroundServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _companionRepoMock = new Mock<ICompanionRepository>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddSingleton(_context);
        services.AddSingleton(_companionRepoMock.Object);
        _serviceProvider = services.BuildServiceProvider();

        var loggerMock = new Mock<ILogger<CompanionBackgroundService>>();
        _service = new CompanionBackgroundService(_serviceProvider, loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task TickAsync_UpdatesState_WhenCalled()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow.AddHours(-1),
            VictoriesToday = 0,
            DefeatesToday = 0
        };
        _companionRepoMock.Setup(r => r.GetOrCreateAsync()).ReturnsAsync(state);
        _companionRepoMock.Setup(r => r.UpdateStateAsync(It.IsAny<CompanionState>())).Returns(Task.CompletedTask);

        await InvokeTickAsync();

        _companionRepoMock.Verify(r => r.GetOrCreateAsync(), Times.Once);
        _companionRepoMock.Verify(r => r.UpdateStateAsync(It.IsAny<CompanionState>()), Times.Once);
    }

    [Fact]
    public async Task TickAsync_SetsValidState()
    {
        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow.AddHours(-1),
            VictoriesToday = 0,
            DefeatesToday = 0
        };
        var updatedState = (CompanionState?)null;

        _companionRepoMock.Setup(r => r.GetOrCreateAsync()).ReturnsAsync(state);
        _companionRepoMock
            .Setup(r => r.UpdateStateAsync(It.IsAny<CompanionState>()))
            .Callback<CompanionState>(s => updatedState = s)
            .Returns(Task.CompletedTask);

        var validStates = new[]
        {
            Constants.CompanionStateRepos,
            Constants.CompanionStateJeu,
            Constants.CompanionStateManger,
            Constants.CompanionStateExcite,
            Constants.CompanionStateTriste,
            Constants.CompanionStateEndormi
        };

        await InvokeTickAsync();

        updatedState.Should().NotBeNull();
        validStates.Should().Contain(updatedState!.CurrentState);
        updatedState.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TickAsync_UpdatesVictoriesAndDefeatesFromCombatStats()
    {
        _context.CombatStats.AddRange(
            new CombatStats { PlayerId = 1, TotalCombats = 10, CombatsWon = 7, CombatsLost = 3 },
            new CombatStats { PlayerId = 2, TotalCombats = 5, CombatsWon = 3, CombatsLost = 2 }
        );
        await _context.SaveChangesAsync();

        var state = new CompanionState
        {
            CurrentState = Constants.CompanionStateRepos,
            LastUpdated = DateTime.UtcNow.AddHours(-1),
            VictoriesToday = 0,
            DefeatesToday = 0
        };
        _companionRepoMock.Setup(r => r.GetOrCreateAsync()).ReturnsAsync(state);
        _companionRepoMock.Setup(r => r.UpdateStateAsync(It.IsAny<CompanionState>())).Returns(Task.CompletedTask);

        await InvokeTickAsync();

        _companionRepoMock.Verify(r => r.UpdateStateAsync(It.Is<CompanionState>(s =>
            s.VictoriesToday == 10 && s.DefeatesToday == 5)), Times.Once);
    }

    [Fact]
    public async Task TickAsync_DoesNotThrow_WhenRepositoryThrows()
    {
        _companionRepoMock.Setup(r => r.GetOrCreateAsync()).ThrowsAsync(new Exception("DB error"));

        var act = async () => await InvokeTickAsync();
        await act.Should().NotThrowAsync();
    }

    private async Task InvokeTickAsync()
    {
        var method = typeof(CompanionBackgroundService).GetMethod("TickAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task?)method?.Invoke(_service, null);
        if (task != null) await task;
    }
}
