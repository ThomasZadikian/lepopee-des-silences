using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.PlayerProfiles;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetPlayerProfileByUserIdHandlerTests
{
    private readonly Mock<IPlayerProfileRepository> _profileRepo;
    private readonly Mock<IUserRepository>          _userRepo;
    private readonly GetPlayerProfileByUserIdHandler _handler;

    public GetPlayerProfileByUserIdHandlerTests()
    {
        _profileRepo = new Mock<IPlayerProfileRepository>();
        _userRepo    = new Mock<IUserRepository>();
        _handler     = new GetPlayerProfileByUserIdHandler(
            _profileRepo.Object,
            _userRepo.Object);
    }

    private User BuildUserWithProfile(int userId = 1) => new()
    {
        Id       = userId,
        Username = "testuser",
        Role     = Constants.RolePlayer,
        PlayerProfile = new PlayerProfile
        {
            Id           = 10,
            UserId       = userId,
            CharacterName = "Aragorn",
            Level        = 5,
            CurrentHP    = 80,
            MaxHP        = 100,
            CurrentMP    = 30,
            MaxMP        = 50,
            Strength     = 15,
            Intelligence = 12,
            Speed        = 11,
            Experience   = 1250,
            Gold         = 300,
            UpdatedAt    = DateTime.UtcNow,
            GameSaves       = new List<GameSave>    { new(), new() },
            Inventory       = new List<PlayerInventory> { new() },
            Skills          = new List<PlayerSkill> { new(), new(), new() },
            BestiaryUnlocks = new List<BestiaryUnlock> { new() },
            CombatStats = new CombatStats
            {
                TotalCombats         = 20,
                CombatsWon           = 15,
                CombatsLost          = 5,
                TotalDamageDealt     = 8000,
                TotalDamageTaken     = 3000,
                TotalPlaytimeMinutes = 120,
            }
        }
    };

    [Fact]
    public async Task Handle_UserWithProfile_ReturnsCorrectData()
    {
        // Arrange
        var user = BuildUserWithProfile(1);
        _userRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(1), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.CharacterName.Should().Be("Aragorn");
        result.Level.Should().Be(5);
        result.CurrentHP.Should().Be(80);
        result.MaxHP.Should().Be(100);
        result.Experience.Should().Be(1250);
        result.Gold.Should().Be(300);
    }

    [Fact]
    public async Task Handle_UserWithProfile_ReturnsCorrectCombatStats()
    {
        // Arrange
        var user = BuildUserWithProfile(1);
        _userRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(1), CancellationToken.None);

        // Assert
        result.TotalCombats.Should().Be(20);
        result.CombatsWon.Should().Be(15);
        result.CombatsLost.Should().Be(5);
        result.TotalDamageDealt.Should().Be(8000);
        result.TotalPlaytimeMinutes.Should().Be(120);
    }

    [Fact]
    public async Task Handle_UserWithProfile_ReturnsCorrectCounts()
    {
        // Arrange
        var user = BuildUserWithProfile(1);
        _userRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(1), CancellationToken.None);

        // Assert
        result.SavesCount.Should().Be(2);
        result.InventoryCount.Should().Be(1);
        result.SkillsCount.Should().Be(3);
        result.BestiaryCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UserWithoutCombatStats_ReturnsNullStats()
    {
        // Arrange
        var user = BuildUserWithProfile(1);
        user.PlayerProfile!.CombatStats = null;
        _userRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(1), CancellationToken.None);

        // Assert — pas encore joué, stats nulles
        result.TotalCombats.Should().BeNull();
        result.CombatsWon.Should().BeNull();
        result.TotalDamageDealt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserWithEmptyCollections_ReturnsZeroCounts()
    {
        // Arrange
        var user = BuildUserWithProfile(1);
        user.PlayerProfile!.GameSaves       = new List<GameSave>();
        user.PlayerProfile!.Inventory       = new List<PlayerInventory>();
        user.PlayerProfile!.Skills          = new List<PlayerSkill>();
        user.PlayerProfile!.BestiaryUnlocks = new List<BestiaryUnlock>();
        _userRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(1), CancellationToken.None);

        // Assert
        result.SavesCount.Should().Be(0);
        result.InventoryCount.Should().Be(0);
        result.SkillsCount.Should().Be(0);
        result.BestiaryCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userRepo.Setup(r => r.GetWithAllDataAsync(99)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(99), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Utilisateur*");
    }

    [Fact]
    public async Task Handle_UserWithoutProfile_ThrowsKeyNotFoundException()
    {
        // Arrange
        var user = new User { Id = 1, Username = "nochar", PlayerProfile = null };
        _userRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var act = async () => await _handler.Handle(
            new GetPlayerProfileByUserIdQuery(1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*personnage*");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        _userRepo.Setup(r => r.GetWithAllDataAsync(42)).ReturnsAsync((User?)null);

        // Act
        try { await _handler.Handle(new GetPlayerProfileByUserIdQuery(42), CancellationToken.None); }
        catch (KeyNotFoundException) { }

        // Assert
        _userRepo.Verify(r => r.GetWithAllDataAsync(42), Times.Once);
    }
}
