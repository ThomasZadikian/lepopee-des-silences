using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using RPG_ESI07.Infrastructure.Data;
using System.Text;

namespace RPG_ESI07.Tests.Infrastructure.Data;

public class DatabaseSeederTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _hasher;

    private static IConfiguration BuildTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:PlayerPassword"] = "TestPlayer2026!",
                ["Seed:AdminPassword"] = "TestAdmin2026!",
                ["Seed:TestPassword"] = "TestUser2026!",
            })
            .Build();
    }
    public DatabaseSeederTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        // Mock du hasher — retourne une valeur fixe pour les tests
        var mockHasher = new Mock<IPasswordHasher>();
        mockHasher.Setup(h => h.HashPassword(It.IsAny<string>()))
                  .Returns("hashed_password_test");
        _hasher = mockHasher.Object;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_PopulatesAllEntities()
    {
        await DatabaseSeeder.SeedAsync(_context, _hasher, BuildTestConfiguration());

        var userCount = await _context.Users.CountAsync();
        var profileCount = await _context.PlayerProfiles.CountAsync();
        var enemyCount = await _context.Enemies.CountAsync();
        var itemCount = await _context.Items.CountAsync();
        var skillCount = await _context.Skills.CountAsync();
        var combatStatsCount = await _context.CombatStats.CountAsync();

        userCount.Should().Be(3);
        profileCount.Should().Be(3);
        enemyCount.Should().Be(20);
        itemCount.Should().Be(30);
        skillCount.Should().Be(18);
        combatStatsCount.Should().Be(3);
    }

    [Fact]
    public async Task SeedAsync_ExistingDatabase_AbortsSeeding()
    {
        _context.Users.Add(new User
        {
            Username = "PreExistingUser",
            Email = Encoding.UTF8.GetBytes("test@test.com"),
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            Role = "Player"
        });
        await _context.SaveChangesAsync();

        await DatabaseSeeder.SeedAsync(_context, _hasher, BuildTestConfiguration());

        var userCount = await _context.Users.CountAsync();
        var profileCount = await _context.PlayerProfiles.CountAsync();

        userCount.Should().Be(1);
        profileCount.Should().Be(0);
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_HashesPasswordsViaHasher()
    {
        await DatabaseSeeder.SeedAsync(_context, _hasher, BuildTestConfiguration());

        var users = await _context.Users.ToListAsync();
        users.Should().AllSatisfy(u =>
            u.PasswordHash.Should().Be("hashed_password_test"));
    }
}