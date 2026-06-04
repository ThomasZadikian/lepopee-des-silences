using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.RGPD;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries.RGPD;
public class GetUserDataHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly GetUserDataHandler _handler;

    public GetUserDataHandlerTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _handler = new GetUserDataHandler(_mockRepo.Object);
    }

    private static User BuildUser(int id = 1, PlayerProfile? profile = null)
    {
        var user = new User
        {
            Id           = id,
            Username     = "testuser",
            CreatedAt    = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt  = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginIP  = Constants.SeedIpAddress,
            PlayerProfile = profile,
        };
        return user;
    }

    // ── Cas nominal ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserExistsWithProfile_ReturnsAllData()
    {
        // Arrange
        var profile = new PlayerProfile
        {
            GameSaves       = new List<GameSave> { new() { Id = 1 } },
            Inventory       = new List<PlayerInventory> { new() { Id = 1 } },
            Skills          = new List<PlayerSkill> { new() { Id = 1 } },
            BestiaryUnlocks = new List<BestiaryUnlock> { new() { Id = 1 } },
            CombatStats     = new CombatStats { Id = 1 },
        };
        var user = BuildUser(1, profile);

        _mockRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new GetUserDataQuery(1), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Username.Should().Be("testuser");
        result.GameSaves.Should().HaveCount(1);
        result.Inventory.Should().HaveCount(1);
        result.Skills.Should().HaveCount(1);
        result.BestiaryUnlocks.Should().HaveCount(1);
        result.CombatStats.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_UserExistsWithoutProfile_ReturnsEmptyLists()
    {
        // Arrange — joueur sans profil créé (PlayerProfile null)
        var user = BuildUser(1, profile: null);
        _mockRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new GetUserDataQuery(1), CancellationToken.None);

        // Assert — les listes doivent être vides, pas null
        result.GameSaves.Should().NotBeNull().And.BeEmpty();
        result.Inventory.Should().NotBeNull().And.BeEmpty();
        result.Skills.Should().NotBeNull().And.BeEmpty();
        result.BestiaryUnlocks.Should().NotBeNull().And.BeEmpty();
        result.CombatStats.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserExists_DoesNotExposePasswordHash()
    {
        // Arrange
        var user = BuildUser(1);
        user.PasswordHash = "argon2id$secret$hash";
        _mockRepo.Setup(r => r.GetWithAllDataAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(new GetUserDataQuery(1), CancellationToken.None);

        // Assert — la Response ne doit pas contenir le PasswordHash
        var props = result.GetType().GetProperties().Select(p => p.Name);
        props.Should().NotContain("PasswordHash");
        props.Should().NotContain("MfaSecret");
    }

    // ── Cas d'erreur ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetWithAllDataAsync(99)).ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(
            new GetUserDataQuery(99), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*introuvable*");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetWithAllDataAsync(42)).ReturnsAsync((User?)null);

        // Act — on s'attend à une exception mais on veut vérifier l'appel
        try { await _handler.Handle(new GetUserDataQuery(42), CancellationToken.None); }
        catch (KeyNotFoundException) { }

        // Assert — le repository doit avoir été appelé avec 42, pas un autre ID
        _mockRepo.Verify(r => r.GetWithAllDataAsync(42), Times.Once);
    }
}
