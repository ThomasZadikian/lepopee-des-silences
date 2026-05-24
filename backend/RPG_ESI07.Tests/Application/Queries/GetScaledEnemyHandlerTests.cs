using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Enemies;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetScaledEnemyHandlerTests
{
    private readonly Mock<IEnemyRepository> _mockRepo;
    private readonly GetScaledEnemyHandler _handler;

    public GetScaledEnemyHandlerTests()
    {
        _mockRepo = new Mock<IEnemyRepository>(MockBehavior.Strict);
        _handler = new GetScaledEnemyHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenEnemyNotFound()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Enemy?)null);

        var result = await _handler.Handle(
            new GetScaledEnemyQuery(999, 1, 0),
            CancellationToken.None);

        result.Enemy.Should().BeNull();
    }

    [Fact]
    public async Task Handle_BasicEnemy_Level5_NoEquipment_CorrectScaling()
    {
        // Rat Géant — puissance joueur = 50, ratio = 1.0
        // multiplicateur = 1 + 7 / (1 + e^0) = 1 + 3.5 = 4.5
        var enemy = new Enemy
        {
            Id = 1,
            Name = "Rat Géant",
            Type = Constants.EnemyTypeBasic,
            MaxHP = 30,
            Strength = 6,
            Intelligence = 2,
            Speed = 14,
            PhysicalResistance = 1.0f,
            MagicalResistance = 1.0f,
            ExperienceReward = 10,
            GoldReward = 3
        };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(enemy);

        var result = await _handler.Handle(
            new GetScaledEnemyQuery(1, 5, 0),
            CancellationToken.None);

        result.Enemy.Should().NotBeNull();
        result.Enemy!.Multiplier.Should().BeApproximately(4.5f, 0.01f);
        result.Enemy.ScaledMaxHP.Should().Be(135);
        result.Enemy.ScaledStrength.Should().Be(27);
    }

    [Fact]
    public async Task Handle_BasicEnemy_HighLevel_PlafonneMult()
    {
        // Joueur niveau 99, full stuff +500 → puissance = 1490
        // ratio = 1490/50 = 29.8 → multiplicateur proche du plafond 8.0
        var enemy = new Enemy
        {
            Id = 2,
            Name = "Gobelin",
            Type = Constants.EnemyTypeBasic,
            MaxHP = 45,
            Strength = 8,
            Intelligence = 4,
            Speed = 10,
            PhysicalResistance = 1.0f,
            MagicalResistance = 1.2f,
            ExperienceReward = 15,
            GoldReward = 8
        };
        _mockRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(enemy);

        var result = await _handler.Handle(
            new GetScaledEnemyQuery(2,99, 500),
            CancellationToken.None);

        // Le multiplicateur doit plafonner proche de 8.0 — jamais dépasser
        result.Enemy!.Multiplier.Should().BeLessThanOrEqualTo(8.0f);
        result.Enemy.Multiplier.Should().BeGreaterThan(7.5f);
    }

    [Fact]
    public async Task Handle_BossEnemy_Level10_Equipment50_CorrectScaling()
    {
        // Dragon de l'Aube — puissance joueur = 150, ratio = 0.375
        // multiplicateur ≈ 6.35
        var enemy = new Enemy
        {
            Id = 3,
            Name = "Dragon de l'Aube",
            Type = Constants.EnemyTypeBoss,
            MaxHP = 900,
            Strength = 40,
            Intelligence = 30,
            Speed = 20,
            PhysicalResistance = 0.7f,
            MagicalResistance = 0.7f,
            ExperienceReward = 1000,
            GoldReward = 500
        };
        _mockRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(enemy);

        var result = await _handler.Handle(
            new GetScaledEnemyQuery(3, 10, 50),
            CancellationToken.None);

        result.Enemy.Should().NotBeNull();
        result.Enemy!.Multiplier.Should().BeApproximately(6.35f, 0.05f);
        result.Enemy.ScaledMaxHP.Should().BeInRange(5690, 5740);
        result.Enemy.ScaledStrength.Should().BeInRange(252, 256);
    }

    [Fact]
    public async Task Handle_MinibossEnemy_Level20_Equipment100_CorrectScaling()
    {
        // Mage Corrompu — puissance joueur = 300, ratio = 2.0
        // multiplicateur = 1 + 11 / (1 + e^(-2.0 * (2-1))) = 1 + 11/1.135 ≈ 10.69
        var enemy = new Enemy
        {
            Id = 4,
            Name = "Mage Corrompu",
            Type = Constants.EnemyTypeMiniboss,
            MaxHP = 180,
            Strength = 10,
            Intelligence = 30,
            Speed = 8,
            PhysicalResistance = 1.6f,
            MagicalResistance = 0.5f,
            ExperienceReward = 120,
            GoldReward = 65
        };
        _mockRepo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(enemy);

        var result = await _handler.Handle(
            new GetScaledEnemyQuery(4, 20, 100),
            CancellationToken.None);

        result.Enemy.Should().NotBeNull();
        result.Enemy!.Multiplier.Should().BeApproximately(10.69f, 0.05f);
        result.Enemy.ScaledMaxHP.Should().BeInRange(1922, 1926);
    }

    [Fact]
    public async Task Handle_PreservesNonScaledFields()
    {
        var enemy = new Enemy
        {
            Id = 5,
            Name = "Troll",
            Type = Constants.EnemyTypeBasic,
            MaxHP = 120,
            Strength = 18,
            Intelligence = 3,
            Speed = 5,
            PhysicalResistance = 0.8f,
            MagicalResistance = 1.2f,
            ExperienceReward = 45,
            GoldReward = 22,
            TransitionMatrix = "{}",
            CombatScripts = "{}",
            InitialState = Constants.EnemyStateRepos
        };
        _mockRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(enemy);

        var result = await _handler.Handle(
            new GetScaledEnemyQuery(5, 10, 0),
            CancellationToken.None);

        result.Enemy!.PhysicalResistance.Should().Be(0.8f);
        result.Enemy.MagicalResistance.Should().Be(1.2f);
        result.Enemy.TransitionMatrix.Should().Be("{}");
        result.Enemy.InitialState.Should().Be(Constants.EnemyStateRepos);
    }
}