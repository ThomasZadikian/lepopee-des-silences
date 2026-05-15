using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Infrastructure.Data;
using RPG_ESI07.Infrastructure.Repositories;
using RPG_ESI07.Infrastructure.Repository;

namespace RPG_ESI07.Tests.Infrastructure.Repository;

public class UserRepositoryGetWithAllDataTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryGetWithAllDataTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<User> SeedUserWithProfile()
    {
        var user = new User
        {
            Username     = "testuser",
            Email        = new byte[] { 1, 2, 3 },
            PasswordHash = "hash",
            Role         = Constants.RolePlayer,
            CreatedAt    = DateTime.UtcNow,
        };

        var item  = new Item  { Name = "Sword", Description = "A sword"};
        var skill = new Skill { Name = "Fire",  Description = "Fire spell" };
        var enemy = new Enemy { Name = "Goblin", Description = "A goblin", MaxHP = 50, Strength = 5, ExperienceReward = 10, GoldReward = 5, };

        _context.Items.Add(item);
        _context.Skills.Add(skill);
        _context.Enemies.Add(enemy);
        await _context.SaveChangesAsync();

        var profile = new PlayerProfile
        {
            CharacterName = "Hero",
            Level         = 5,
            User          = user,
            GameSaves     = new List<GameSave>
            {
                new() { CurrentZone = "Forest", }
            },
            Inventory = new List<PlayerInventory>
            {
                new() { Item = item, Quantity = 1 }
            },
            Skills = new List<PlayerSkill>
            {
                new() { Skill = skill}
            },
            BestiaryUnlocks = new List<BestiaryUnlock>
            {
                new() { Enemy = enemy }
            },
            CombatStats = new CombatStats
            {
                TotalCombats = 10, CombatsWon = 8, CombatsLost = 2,
                TotalDamageDealt = 5000, TotalDamageTaken = 2000,
                TotalPlaytimeMinutes = 120
            },
        };

        user.PlayerProfile = profile;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWithAllDataAsync_UserWithFullProfile_ReturnsAllRelations()
    {
        // Arrange
        var seeded = await SeedUserWithProfile();

        // Act
        var result = await _repository.GetWithAllDataAsync(seeded.Id);

        // Assert
        result.Should().NotBeNull();
        result!.PlayerProfile.Should().NotBeNull();
        result.PlayerProfile!.GameSaves.Should().HaveCount(1);
        result.PlayerProfile.Inventory.Should().HaveCount(1);
        result.PlayerProfile.Skills.Should().HaveCount(1);
        result.PlayerProfile.BestiaryUnlocks.Should().HaveCount(1);
        result.PlayerProfile.CombatStats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWithAllDataAsync_UserWithFullProfile_LoadsItemInInventory()
    {
        // Arrange
        var seeded = await SeedUserWithProfile();

        // Act
        var result = await _repository.GetWithAllDataAsync(seeded.Id);

        // Assert — l'Item doit être chargé via ThenInclude
        result!.PlayerProfile!.Inventory.First().Item.Should().NotBeNull();
        result.PlayerProfile.Inventory.First().Item!.Name.Should().Be("Sword");
    }

    [Fact]
    public async Task GetWithAllDataAsync_UserWithFullProfile_LoadsSkillInPlayerSkills()
    {
        // Arrange
        var seeded = await SeedUserWithProfile();

        // Act
        var result = await _repository.GetWithAllDataAsync(seeded.Id);

        // Assert — le Skill doit être chargé via ThenInclude
        result!.PlayerProfile!.Skills.First().Skill.Should().NotBeNull();
        result.PlayerProfile.Skills.First().Skill!.Name.Should().Be("Fire");
    }

    [Fact]
    public async Task GetWithAllDataAsync_UserWithoutProfile_ReturnsUserWithNullProfile()
    {
        // Arrange — user sans profil
        var user = new User
        {
            Username     = "noprofile",
            Email        = new byte[] { 9, 9 },
            PasswordHash = "hash",
            Role         = Constants.RolePlayer,
            CreatedAt    = DateTime.UtcNow,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithAllDataAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.PlayerProfile.Should().BeNull();
    }

    [Fact]
    public async Task GetWithAllDataAsync_NonExistingUserId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWithAllDataAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithAllDataAsync_ReturnsCorrectUser_WhenMultipleUsersExist()
    {
        // Arrange — deux users en base
        var user1 = new User { Username = "user1", Email = new byte[] { 1 }, PasswordHash = "h", Role = Constants.RolePlayer, CreatedAt = DateTime.UtcNow };
        var user2 = new User { Username = "user2", Email = new byte[] { 2 }, PasswordHash = "h", Role = Constants.RolePlayer, CreatedAt = DateTime.UtcNow };
        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithAllDataAsync(user1.Id);

        // Assert — on doit récupérer user1, pas user2
        result.Should().NotBeNull();
        result!.Username.Should().Be("user1");
    }
}
