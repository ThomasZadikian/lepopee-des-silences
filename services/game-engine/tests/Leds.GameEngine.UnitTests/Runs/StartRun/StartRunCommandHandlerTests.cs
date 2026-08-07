using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.SharedBuildingBlocks.Time;
using Leds.SharedBuildingBlocks.Results;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.PalaceLaws;
using Leds.GameEngine.Application.Players;
using Leds.GameEngine.Application.Players.Ports;
using Leds.GameEngine.Application.Runs.StartRun;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.Domain.Combats.Typing;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.StartRun;

public sealed class StartRunCommandHandlerTests
{
    private static PlayerSkillMerger CreateSkillMerger(Mock<ICatalogContentGateway> gateway)
    {
        gateway
            .Setup(g => g.GetEmotionalAffinityMatrixAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogEmotionalAffinityMatrixSnapshot(
                Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create().Version,
                Leds.GameEngine.UnitTests.Common.TestEmotionalAffinityMatrix.Create().Rules
                    .Select(rule => new CatalogEmotionalAffinityRuleSnapshot(
                        rule.AttackingRegister.ToString(),
                        rule.DefendingRegister.ToString(),
                        rule.Effectiveness.ToString(),
                        rule.Multiplier))
                    .ToArray()));
        gateway
            .Setup(g => g.ListCharacterCombatDefinitionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CatalogCharacterCombatDefinition(
                    "character.player.self", "Protagonist", "adaptive", "memoire"),
                new CatalogCharacterCombatDefinition(
                    "character.mane", "Companion", "glass-cannon", "rupture")
            ]);
        gateway
            .Setup(g => g.GetSkillDefinitionByKeyAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => key switch
            {
                "skill.granted.shield" => new CatalogSkillDefinition(
                    key, "Bouclier accordé", "Un bouclier temporaire.",
                    "Defense", "Self", "Guard", 0, 0, 8, [],
                    EmotionalRegister: "Neutral"),
                "skill.mane.favorite-de-elise" => new CatalogSkillDefinition(
                    key, "Favorite de Elise", "Un soin instantané.",
                    "Buff", "Self", "Heal", 0, 0, 15, [],
                    BasePowerIsPercentOfMaxVitality: true,
                    EmotionalRegister: "Neutral"),
                _ => new CatalogSkillDefinition(
                    key,
                    key,
                    "Test skill",
                    key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Defense" : "Damage",
                    key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Self" : "SingleEnemy",
                    key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? "Guard" : "Damage",
                    0,
                    0,
                    key.Contains("guard", StringComparison.OrdinalIgnoreCase) ? 5 : 10,
                    [],
                    EmotionalRegister: "Neutral")
            });
        return new PlayerSkillMerger(gateway.Object);
    }

    private static void ConfigurePermanentBehavior(
        Mock<ICatalogContentGateway> gateway,
        string itemKey,
        string behaviorCode)
    {
        gateway
            .Setup(g => g.GetItemDefinitionByKeyAsync(itemKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                itemKey,
                "test-1.0.0",
                itemKey,
                "Test item",
                null,
                "Equipment",
                "Accessory",
                "Rare",
                "Passive",
                "PersistentMeta",
                "None",
                1,
                false,
                false,
                EquipmentEffects:
                [new CatalogItemEquipmentEffect(
                    "RuntimeBehavior", null, null, null, null, BehaviorCode: behaviorCode)])));
    }

    private static Mock<IPlayerProfileGateway> CreateProfileGateway(
        Guid playerId, params string[] permanentItemKeys)
    {
        var gateway = new Mock<IPlayerProfileGateway>();
        gateway
            .Setup(g => g.GetProfileAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerProfileView(
                playerId,
                "Test Player",
                [],
                new PlayerProgressionView(0, 0),
                permanentItemKeys
                    .Select(key => new PlayerPermanentItemView(key, null, DateTimeOffset.UtcNow))
                    .ToArray()));
        gateway
            .Setup(g => g.GetNpcReputationScoresAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NpcReputationScoreView>());
        return gateway;
    }

    [Fact]
    public async Task Handle_ShouldCreateRun_AndPersistIt()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-001");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-001", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills:
                    [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        var response = await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        response.Run.Id.Should().NotBeEmpty();
        response.Run.PlayerId.Should().Be(playerId);
        response.Run.Seed.Should().Be("seed-test-001");
        response.Run.GeneratorVersion.Should().Be("gen-0.1.0");
        response.Run.MarkovMatrixVersion.Should().Be("markov-0.1.0");
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        var allNodes = response.Run.CurrentRoom.Nodes.ToArray();

        allNodes.Should().HaveCount(6);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .HaveCount(5);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.State == "Available");

        allNodes
            .Should()
            .ContainSingle(node => node.IsBoss);

        repository.Verify(
            repo => repo.AddAsync(
                It.Is<Run>(run =>
                    run.PlayerId == playerId &&
                    run.Seed == "seed-test-001" &&
                    run.Status == RunStatus.Active),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSeedPlayerStateManaAndCharge_FromCharacterStats()
    {
        // Regression test: Run.StartNew used to hardcode PlayerState's starting
        // Mana/Charge at 0 regardless of the character's actual Mana/Charge stat —
        // silently discarding any stat points invested via SpendStatPointCommand.
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-mana");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-mana", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 30,
                        Charge: 4),
                    Skills:
                    [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.PlayerState!.Mana.Should().Be(30);
        capturedRun.PlayerState!.Charge.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ShouldApplyEquippedItemStatBonusesAndGrantedSkill()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-002");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-002", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills:
                    [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10)
                    ],
                    EquippedItemKeys: ["item.equipment.sac-a-dos"])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.equipment.sac-a-dos", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.equipment.sac-a-dos",
                "1.0",
                "Sac à dos renforcé",
                "Un sac à dos qui augmente la capacité du sac de run.",
                null,
                "Equipment",
                "Backpack",
                "Rare",
                "PermanentEquip",
                "Permanent",
                "None",
                1,
                false,
                false,
                IsPermanentEligible: true,
                EquipmentEffects:
                [
                    new CatalogItemEquipmentEffect("StatBonus", "RunItemCapacity", 2, null, null),
                    new CatalogItemEquipmentEffect("StatBonus", "AttackPower", 3, null, null),
                    new CatalogItemEquipmentEffect("GrantSkill", null, null, "skill.granted.shield", null)
                ])));
        catalogGateway
            .Setup(g => g.GetSkillDefinitionByKeyAsync("skill.granted.shield", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogSkillDefinition(
                "skill.granted.shield", "Bouclier accordé", "Un bouclier temporaire.",
                "Defense", "Self", "Guard", 0, 0, 8, [], "neutral"));

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        repository.Invocations.Should().ContainSingle(i => i.Method.Name == nameof(IRunRepository.AddAsync));
        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.Attack.Should().Be(15);
        capturedRun.RunItemCapacity.Should().Be(Run.DefaultRunItemCapacity + 2);
        capturedRun.PlayerState!.Skills.Should().Contain(s => s.Key == "skill.granted.shield");
        capturedRun.PlayerState!.Skills.Should().Contain(s => s.Key == "skill.basic.strike");
    }

    [Fact]
    public async Task Handle_ShouldSeedRunMagicAttackAndMagicDefense_FromSnapshotAndEquipmentBonuses()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-magic");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-magic", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0,
                        MagicAttack: 9,
                        MagicDefense: 4),
                    Skills:
                    [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10)
                    ],
                    EquippedItemKeys: ["item.equipment.monocle"])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.equipment.monocle", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.equipment.monocle",
                "1.0",
                "Monocle",
                "Un monocle qui augmente l'attaque magique.",
                null,
                "Equipment",
                "Accessory",
                "Rare",
                "PermanentEquip",
                "Permanent",
                "None",
                1,
                false,
                false,
                IsPermanentEligible: true,
                EquipmentEffects:
                [
                    new CatalogItemEquipmentEffect("StatBonus", "MagicAttack", 3, null, null)
                ])));

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.MagicAttack.Should().Be(12);
        capturedRun.MagicDefense.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ShouldResolveEquippedSkill_FromCatalog_NotFromSnapshotGuess()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-003");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-003", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    // The snapshot's own guess is wrong on purpose (Damage/10, no percent
                    // heal) — this is what player-service's real run-snapshot endpoint
                    // sends today when it only knows the equipped skill KEY, not its
                    // catalog-defined mechanics.
                    Skills:
                    [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.mane.favorite-de-elise",
                            DisplayName: "skill.mane.favorite-de-elise",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        catalogGateway
            .Setup(g => g.GetSkillDefinitionByKeyAsync("skill.mane.favorite-de-elise", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CatalogSkillDefinition(
                "skill.mane.favorite-de-elise", "Favorite de Elise", "Un soin instantané.",
                "Buff", "Self", "Heal", 0, 0, 15, [], "neutral",
                BasePowerIsPercentOfMaxVitality: true));

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        var resolvedSkill = capturedRun.PlayerState!.Skills.Single(s => s.Key == "skill.mane.favorite-de-elise");
        resolvedSkill.EffectType.Should().Be("Heal");
        resolvedSkill.BasePower.Should().Be(15);
        resolvedSkill.BasePowerIsPercentOfMaxVitality.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldApplyStatBonusPercent_FromEquippedItem()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-002");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-002", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ],
                    EquippedItemKeys: ["item.equipment.bague-du-courage"])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        catalogGateway
            .Setup(g => g.GetItemDefinitionByKeyAsync("item.equipment.bague-du-courage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CatalogItemDefinitionSnapshot>.Success(new CatalogItemDefinitionSnapshot(
                "item.equipment.bague-du-courage",
                "1.0",
                "Bague du courage",
                "Un anneau simple.",
                null,
                "Equipment",
                "Accessory",
                "Epic",
                "PermanentEquip",
                "Permanent",
                "None",
                1,
                false,
                false,
                IsPermanentEligible: true,
                EquipmentEffects:
                [
                    new CatalogItemEquipmentEffect("StatBonusPercent", "Speed", 10, null, null),
                    new CatalogItemEquipmentEffect("StatBonusPercent", "AttackPower", 10, null, null)
                ])));

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        // Speed: 10 base + round(10 * 10%) = 11. Attack: 12 base + round(12 * 10%) = 13.
        capturedRun.Speed.Should().Be(11);
        capturedRun.Attack.Should().Be(13);
    }

    [Fact]
    public async Task Handle_ShouldEnableJournal_WhenPlayerOwnsCarnetDeBord()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-journal");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-journal", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConfigurePermanentBehavior(catalogGateway, "canon.item.carnet-de-bord", "run-journal");
        var playerProfileGateway = CreateProfileGateway(playerId, "canon.item.carnet-de-bord");
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.JournalEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNotEnableJournal_WhenPlayerLacksCarnetDeBord()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-no-journal");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-no-journal", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.JournalEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldEnableLawDenial_WhenPlayerOwnsDeniPermanent()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-law-denial");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-law-denial", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConfigurePermanentBehavior(catalogGateway, "canon.item.deni-permanent", "deny-palace-law");
        var playerProfileGateway = CreateProfileGateway(playerId, "canon.item.deni-permanent");
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.LawDenialEnabled.Should().BeTrue();
        capturedRun.CanUseLawDenial.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldNotEnableLawDenial_WhenPlayerLacksDeniPermanent()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-no-law-denial");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-no-law-denial", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var playerProfileGateway = CreateProfileGateway(playerId);
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.LawDenialEnabled.Should().BeFalse();
        capturedRun.CanUseLawDenial.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldEnableCaliceInfini_WhenPlayerOwnsCaliceInfini()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-calice-infini");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-calice-infini", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConfigurePermanentBehavior(catalogGateway, "canon.item.calice-infini", "infinite-chalice");
        var playerProfileGateway = CreateProfileGateway(playerId, "canon.item.calice-infini");
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.CaliceInfiniEnabled.Should().BeTrue();
        capturedRun.CanUseCaliceInfini.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetReputationGainBonus_WhenPlayerOwnsPelucheDeMina()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-peluche-mina");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-peluche-mina", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConfigurePermanentBehavior(catalogGateway, "canon.item.peluche-mina", "reputation-gain-plus-ten");
        var playerProfileGateway = CreateProfileGateway(playerId, "canon.item.peluche-mina");
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.ReputationGainBonusPercent.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldEnableHimLitProtection_WhenPlayerOwnsProtectionDeHimLit()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);

        var initialRoom = TestGameEngineFactory.CreateThresholdRoom();

        var generator = new Mock<IRunGenerator>();
        generator.SetupGet(service => service.GeneratorVersion).Returns("gen-0.1.0");
        generator.SetupGet(service => service.MarkovMatrixVersion).Returns("markov-0.1.0");
        generator.Setup(service => service.GenerateSeed()).Returns("seed-test-protection-himlit");
        generator.Setup(service => service.GenerateInitialRoomAsync("seed-test-protection-himlit", CancellationToken.None)).ReturnsAsync(initialRoom);

        var repository = new Mock<IRunRepository>();

        var playerGateway = new Mock<IPlayerRunSnapshotGateway>();
        playerGateway
            .Setup(g => g.GetRunSnapshotAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRunSnapshot(
                playerId,
                "Test Player",
                [new PlayerRunSnapshotCharacter(
                    Guid.NewGuid(),
                    "character.player.self",
                    "Le Porteur",
                    Stats: new PlayerRunSnapshotCharacterStats(
                        MaxVitality: 100,
                        AttackPower: 12,
                        Defense: 6,
                        StartingGuard: 0,
                        Speed: 10,
                        Initiative: 10,
                        Focus: 0,
                        Mana: 0,
                        Charge: 0),
                    Skills: [
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.strike",
                            DisplayName: "Frappe",
                            SkillType: "Damage",
                            TargetingMode: "SingleEnemy",
                            EffectType: "Damage",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 10),
                        new PlayerRunSnapshotCharacterSkill(
                            SkillDefinitionKey: "skill.basic.guard",
                            DisplayName: "Garde",
                            SkillType: "Defense",
                            TargetingMode: "Self",
                            EffectType: "Guard",
                            ManaCost: 0,
                            ChargeCost: 0,
                            BasePower: 5)
                    ])]));

        var clock = new Mock<IClock>();
        clock.SetupGet(service => service.UtcNow).Returns(now);

        var catalogGateway = new Mock<ICatalogContentGateway>();
        var palaceLawPromulgator = new Mock<IAmbientPalaceLawPromulgator>();
        palaceLawPromulgator
            .Setup(p => p.PromulgateForRoomTransitionAsync(It.IsAny<Run>(), It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ConfigurePermanentBehavior(catalogGateway, "canon.item.protection-himlit", "himlit-protection");
        var playerProfileGateway = CreateProfileGateway(playerId, "canon.item.protection-himlit");
        var handler = new StartRunCommandHandler(
            generator.Object,
            repository.Object,
            playerGateway.Object,
            playerProfileGateway.Object,
            palaceLawPromulgator.Object,
            CreateSkillMerger(catalogGateway),
            new PlayerStatMerger(),
            clock.Object,
            catalogGateway.Object);

        await handler.Handle(
            new StartRunCommand(playerId),
            CancellationToken.None);

        var capturedRun = (Run)repository.Invocations
            .Single(i => i.Method.Name == nameof(IRunRepository.AddAsync)).Arguments[0];

        capturedRun.HimLitProtectionEnabled.Should().BeTrue();
    }

}
