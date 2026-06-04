using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class LeaderboardRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly LeaderboardRepository _repository;

    public LeaderboardRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new LeaderboardRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetLeaderboardAsync_DefaultSort_OrdersByLevelDescending()
    {
        await SeedProfilesAsync();
        var (items, total) = await _repository.GetLeaderboardAsync("level", 0, 10, CancellationToken.None);

        items.Should().HaveCount(3);
        items[0].Level.Should().Be(30);
        items[1].Level.Should().Be(20);
        items[2].Level.Should().Be(10);
        total.Should().Be(3);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SortByCharacterName_OrdersAlphabetically()
    {
        await SeedProfilesAsync();
        var (items, _) = await _repository.GetLeaderboardAsync("charactername", 0, 10, CancellationToken.None);

        items[0].CharacterName.Should().Be("Alice");
        items[1].CharacterName.Should().Be("Bob");
        items[2].CharacterName.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetLeaderboardAsync_SortByCombatsWon_OrdersByDescending()
    {
        await SeedProfilesAsync();
        var (items, _) = await _repository.GetLeaderboardAsync("combatswon", 0, 10, CancellationToken.None);

        items[0].CombatStats!.CombatsWon.Should().Be(30);
        items[1].CombatStats!.CombatsWon.Should().Be(20);
        items[2].CombatStats!.CombatsWon.Should().Be(10);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SortByTotalDamageDealt_OrdersByDescending()
    {
        await SeedProfilesAsync();
        var (items, _) = await _repository.GetLeaderboardAsync("totaldamagedealt", 0, 10, CancellationToken.None);

        items[0].CombatStats!.TotalDamageDealt.Should().Be(9000);
        items[1].CombatStats!.TotalDamageDealt.Should().Be(5000);
        items[2].CombatStats!.TotalDamageDealt.Should().Be(1000);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SortByPlaytime_OrdersByDescending()
    {
        await SeedProfilesAsync();
        var (items, _) = await _repository.GetLeaderboardAsync("totalplaytimeminutes", 0, 10, CancellationToken.None);

        items[0].CombatStats!.TotalPlaytimeMinutes.Should().Be(300);
        items[1].CombatStats!.TotalPlaytimeMinutes.Should().Be(200);
        items[2].CombatStats!.TotalPlaytimeMinutes.Should().Be(100);
    }

    [Fact]
    public async Task GetLeaderboardAsync_UnknownSort_FallsBackToLevel()
    {
        await SeedProfilesAsync();
        var (items, _) = await _repository.GetLeaderboardAsync("invalid_sort", 0, 10, CancellationToken.None);

        items[0].Level.Should().Be(30);
    }

    [Fact]
    public async Task GetLeaderboardAsync_Pagination_CorrectSkipAndTake()
    {
        for (int i = 1; i <= 5; i++)
        {
            var p = new PlayerProfile
            {
                UserId = i,
                CharacterName = $"P{i}",
                Level = i * 10,
                CurrentHP = 100,
                MaxHP = 100,
                CurrentMP = 50,
                MaxMP = 50,
                Strength = 10,
                Intelligence = 10,
                Speed = 10,
                Experience = 0,
                Gold = 0,
                UpdatedAt = DateTime.UtcNow,
                CurrentZone = "zone",
                ScalingFactor = 1.0f,
                CombatStats = new CombatStats
                {
                    PlayerId = i,
                    TotalCombats = 0,
                    CombatsWon = 0,
                    CombatsLost = 0,
                    TotalDamageDealt = 0,
                    TotalDamageTaken = 0,
                    TotalPlaytimeMinutes = 0
                }
            };
            _context.PlayerProfiles.Add(p);
        }
        await _context.SaveChangesAsync();

        var (page1, total) = await _repository.GetLeaderboardAsync("level", 0, 2, CancellationToken.None);
        var (page2, _) = await _repository.GetLeaderboardAsync("level", 2, 2, CancellationToken.None);

        total.Should().Be(5);
        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
        page1[0].Level.Should().Be(50);
        page2[0].Level.Should().Be(30);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ExcludesProfilesWithoutCombatStats()
    {
        var withStats = new PlayerProfile
        {
            UserId = 1, CharacterName = "WithStats", Level = 10,
            CurrentHP = 100, MaxHP = 100, CurrentMP = 50, MaxMP = 50,
            Strength = 10, Intelligence = 10, Speed = 10,
            Experience = 0, Gold = 0, UpdatedAt = DateTime.UtcNow,
            CurrentZone = "zone", ScalingFactor = 1.0f,
            CombatStats = new CombatStats { PlayerId = 1 }
        };
        var withoutStats = new PlayerProfile
        {
            UserId = 2, CharacterName = "NoStats", Level = 20,
            CurrentHP = 100, MaxHP = 100, CurrentMP = 50, MaxMP = 50,
            Strength = 10, Intelligence = 10, Speed = 10,
            Experience = 0, Gold = 0, UpdatedAt = DateTime.UtcNow,
            CurrentZone = "zone", ScalingFactor = 1.0f
        };
        _context.PlayerProfiles.AddRange(withStats, withoutStats);
        await _context.SaveChangesAsync();

        var (items, total) = await _repository.GetLeaderboardAsync("level", 0, 10, CancellationToken.None);

        items.Should().HaveCount(1);
        items[0].CharacterName.Should().Be("WithStats");
        total.Should().Be(1);
    }

    private async Task SeedProfilesAsync()
    {
        var profiles = new List<PlayerProfile>
        {
            new()
            {
                UserId = 1, CharacterName = "Alice", Level = 20,
                CurrentHP = 100, MaxHP = 100, CurrentMP = 50, MaxMP = 50,
                Strength = 10, Intelligence = 10, Speed = 10,
                Experience = 0, Gold = 0, UpdatedAt = DateTime.UtcNow,
                CurrentZone = "zone", ScalingFactor = 1.0f,
                CombatStats = new CombatStats
                {
                    PlayerId = 1, TotalCombats = 50, CombatsWon = 20, CombatsLost = 30,
                    TotalDamageDealt = 5000, TotalDamageTaken = 3000, TotalPlaytimeMinutes = 200
                }
            },
            new()
            {
                UserId = 2, CharacterName = "Bob", Level = 10,
                CurrentHP = 100, MaxHP = 100, CurrentMP = 50, MaxMP = 50,
                Strength = 10, Intelligence = 10, Speed = 10,
                Experience = 0, Gold = 0, UpdatedAt = DateTime.UtcNow,
                CurrentZone = "zone", ScalingFactor = 1.0f,
                CombatStats = new CombatStats
                {
                    PlayerId = 2, TotalCombats = 30, CombatsWon = 10, CombatsLost = 20,
                    TotalDamageDealt = 1000, TotalDamageTaken = 2000, TotalPlaytimeMinutes = 100
                }
            },
            new()
            {
                UserId = 3, CharacterName = "Charlie", Level = 30,
                CurrentHP = 100, MaxHP = 100, CurrentMP = 50, MaxMP = 50,
                Strength = 10, Intelligence = 10, Speed = 10,
                Experience = 0, Gold = 0, UpdatedAt = DateTime.UtcNow,
                CurrentZone = "zone", ScalingFactor = 1.0f,
                CombatStats = new CombatStats
                {
                    PlayerId = 3, TotalCombats = 80, CombatsWon = 30, CombatsLost = 50,
                    TotalDamageDealt = 9000, TotalDamageTaken = 5000, TotalPlaytimeMinutes = 300
                }
            },
        };
        _context.PlayerProfiles.AddRange(profiles);
        await _context.SaveChangesAsync();
    }
}
