using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.PlayerProfiles;
using RPG_ESI07.Application.Commands.CombatStatss;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.PlayerProfiles;

// ── CreatePlayerProfileHandler ────────────────────────────────────────────────
public class CreatePlayerProfileHandlerTests
{
    private readonly Mock<IPlayerProfileRepository> _mockRepo;
    private readonly CreatePlayerProfileHandler _handler;

    public CreatePlayerProfileHandlerTests()
    {
        _mockRepo = new Mock<IPlayerProfileRepository>();
        _handler  = new CreatePlayerProfileHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProfileWithDefaultStats()
    {
        // Arrange
        PlayerProfile? captured = null;
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<PlayerProfile>()))
            .Callback<PlayerProfile>(p => captured = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new CreatePlayerProfileCommand(UserId: 1, CharacterName: "Aragorn"),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Contain("succès");
        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(1);
        captured.CharacterName.Should().Be("Aragorn");
        captured.Level.Should().Be(1);
        captured.CurrentHP.Should().Be(100);
        captured.MaxHP.Should().Be(100);
        captured.CurrentMP.Should().Be(50);
        captured.MaxMP.Should().Be(50);
        captured.Strength.Should().Be(10);
        captured.Intelligence.Should().Be(10);
        captured.Speed.Should().Be(10);
        captured.Experience.Should().Be(0);
        captured.Gold.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAsync()
    {
        // Arrange
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<PlayerProfile>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(
            new CreatePlayerProfileCommand(1, "Hero"),
            CancellationToken.None);

        // Assert
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<PlayerProfile>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProfileId()
    {
        // Arrange
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<PlayerProfile>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new CreatePlayerProfileCommand(1, "Hero"),
            CancellationToken.None);

        // Assert
        result.Id.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_DifferentUsers_CreatesSeparateProfiles()
    {
        // Arrange
        var profiles = new List<PlayerProfile>();
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<PlayerProfile>()))
            .Callback<PlayerProfile>(p => profiles.Add(p))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new CreatePlayerProfileCommand(1, "Player1"), CancellationToken.None);
        await _handler.Handle(new CreatePlayerProfileCommand(2, "Player2"), CancellationToken.None);

        // Assert
        profiles.Should().HaveCount(2);
        profiles[0].UserId.Should().Be(1);
        profiles[1].UserId.Should().Be(2);
    }
}

// ── SyncGameSaveHandler ───────────────────────────────────────────────────────
public class SyncGameSaveHandlerTests
{
    private readonly Mock<IPlayerProfileRepository>  _profileRepo;
    private readonly Mock<IGameSaveRepository>       _gameSaveRepo;
    private readonly Mock<ICombatStatsRepository>    _combatRepo;
    private readonly SyncGameSaveHandler             _handler;

    public SyncGameSaveHandlerTests()
    {
        _profileRepo  = new Mock<IPlayerProfileRepository>();
        _gameSaveRepo = new Mock<IGameSaveRepository>();
        _combatRepo   = new Mock<ICombatStatsRepository>();
        _handler      = new SyncGameSaveHandler(
            _profileRepo.Object,
            _gameSaveRepo.Object,
            _combatRepo.Object);
    }

    private SyncGameSaveCommand BuildCommand(int userId = 1) => new(
        RequestingUserId:      userId,
        CurrentZone:           "Forest",
        PositionX:             10f,
        PositionY:             20f,
        QuestFlags:            "{\"q1\": true}",
        Level:                 5,
        CurrentHP:             80,
        MaxHP:                 100,
        CurrentMP:             30,
        MaxMP:                 50,
        Strength:              15,
        Intelligence:          12,
        Speed:                 11,
        Experience:            1250,
        Gold:                  300,
        TotalCombats:          20,
        CombatsWon:            15,
        CombatsLost:           5,
        TotalDamageDealt:      8000,
        TotalDamageTaken:      3000,
        TotalPlaytimeMinutes:  120
    );

    private PlayerProfile BuildProfile(int userId = 1) => new()
    {
        Id     = 1,
        UserId = userId,
        Level  = 1,
    };

    [Fact]
    public async Task Handle_ValidSync_UpdatesPlayerProfile()
    {
        // Arrange
        var profile = BuildProfile();
        _profileRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerProfile> { profile });
        _profileRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerProfile>())).Returns(Task.CompletedTask);
        _gameSaveRepo.Setup(r => r.AddAsync(It.IsAny<GameSave>())).Returns(Task.CompletedTask);
        _combatRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats>());
        _combatRepo.Setup(r => r.AddAsync(It.IsAny<CombatStats>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        profile.Level.Should().Be(5);
        profile.CurrentHP.Should().Be(80);
        profile.Experience.Should().Be(1250);
        profile.Gold.Should().Be(300);
    }

    [Fact]
    public async Task Handle_ValidSync_CreatesNewGameSave()
    {
        // Arrange
        var profile = BuildProfile();
        GameSave? capturedSave = null;
        _profileRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerProfile> { profile });
        _profileRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerProfile>())).Returns(Task.CompletedTask);
        _gameSaveRepo
            .Setup(r => r.AddAsync(It.IsAny<GameSave>()))
            .Callback<GameSave>(s => capturedSave = s)
            .Returns(Task.CompletedTask);
        _combatRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats>());
        _combatRepo.Setup(r => r.AddAsync(It.IsAny<CombatStats>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        capturedSave.Should().NotBeNull();
        capturedSave!.CurrentZone.Should().Be("Forest");
        capturedSave.PositionX.Should().Be(10f);
        capturedSave.PositionY.Should().Be(20f);
        capturedSave.PlayerId.Should().Be(profile.Id);
    }

    [Fact]
    public async Task Handle_FirstSync_CreatesCombatStats()
    {
        // Arrange
        var profile = BuildProfile();
        _profileRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerProfile> { profile });
        _profileRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerProfile>())).Returns(Task.CompletedTask);
        _gameSaveRepo.Setup(r => r.AddAsync(It.IsAny<GameSave>())).Returns(Task.CompletedTask);
        _combatRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats>());
        _combatRepo.Setup(r => r.AddAsync(It.IsAny<CombatStats>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert — première sync → AddAsync appelé, pas UpdateAsync
        _combatRepo.Verify(r => r.AddAsync(It.IsAny<CombatStats>()), Times.Once);
        _combatRepo.Verify(r => r.UpdateAsync(It.IsAny<CombatStats>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SubsequentSync_UpdatesCombatStats()
    {
        // Arrange
        var profile = BuildProfile();
        var existingStats = new CombatStats { Id = 1, PlayerId = profile.Id, TotalCombats = 5 };
        _profileRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerProfile> { profile });
        _profileRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerProfile>())).Returns(Task.CompletedTask);
        _gameSaveRepo.Setup(r => r.AddAsync(It.IsAny<GameSave>())).Returns(Task.CompletedTask);
        _combatRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats> { existingStats });
        _combatRepo.Setup(r => r.UpdateAsync(It.IsAny<CombatStats>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert — sync suivante → UpdateAsync appelé, pas AddAsync
        _combatRepo.Verify(r => r.UpdateAsync(It.IsAny<CombatStats>()), Times.Once);
        _combatRepo.Verify(r => r.AddAsync(It.IsAny<CombatStats>()), Times.Never);
        existingStats.TotalCombats.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange — aucun profil correspondant à l'userId
        _profileRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerProfile>());

        // Act
        var act = async () => await _handler.Handle(BuildCommand(userId: 99), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*personnage*");
    }

    [Fact]
    public async Task Handle_ValidSync_ReturnsSuccessMessage()
    {
        // Arrange
        var profile = BuildProfile();
        _profileRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerProfile> { profile });
        _profileRepo.Setup(r => r.UpdateAsync(It.IsAny<PlayerProfile>())).Returns(Task.CompletedTask);
        _gameSaveRepo.Setup(r => r.AddAsync(It.IsAny<GameSave>())).Returns(Task.CompletedTask);
        _combatRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats>());
        _combatRepo.Setup(r => r.AddAsync(It.IsAny<CombatStats>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("succès");
    }
}
