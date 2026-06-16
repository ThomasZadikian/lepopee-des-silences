using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Catalog;
using Leds.GameEngine.Application.Catalog.Contracts;
using Leds.GameEngine.Application.Catalog.Ports;
using Leds.GameEngine.Application.Combats;
using Leds.GameEngine.Application.Combats.EncounterDrafts;
using Leds.GameEngine.Application.Combats.Ports;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.Events.Contracts;
using Leds.GameEngine.Application.Events.Dtos;
using Leds.GameEngine.Application.Events.Ports;
using Leds.GameEngine.Application.Events.ResolveNodeEvent;
using Leds.GameEngine.Application.Rewards.Ports;
using Leds.GameEngine.Application.Rewards.RewardOfferFactory;
using Leds.GameEngine.Application.Runs.ResolveCurrentEvent;
using Leds.GameEngine.Domain.Combats;
using Leds.GameEngine.Domain.Nodes;
using Leds.GameEngine.Domain.Rooms;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Leds.SharedBuildingBlocks.Results;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.ResolveCurrentEvent;

public sealed class ResolveCurrentEventCommandHandlerTests
{
    private static Mock<INodeEventResolverDispatcher> CreateDispatcherMock(
        NodeEventResolutionKind resolutionKind = NodeEventResolutionKind.NarrativeFragmentRevealed)
    {
        var dispatcher = new Mock<INodeEventResolverDispatcher>();

        dispatcher
            .Setup(service => service.Resolve(It.IsAny<NodeEventResolutionContext>()))
            .Returns(NodeEventResolutionResult.Create(
                resolutionKind,
                "Test outcome",
                "Test outcome description",
                narrativeFragments: new[]
                {
                    new NarrativeFragmentDto(
                        "Elise",
                        "Test narrative fragment.")
                }));

        return dispatcher;
    }

    private static Mock<IEventContentResolver> CreateContentResolverMock()
    {
        return new Mock<IEventContentResolver>();
    }

    private static Mock<ICatalogContentGateway> CreateCatalogGatewayMock()
    {
        return new Mock<ICatalogContentGateway>();
    }

    private static Mock<ICombatInstanceFactory> CreateCombatFactoryMock()
    {
        return new Mock<ICombatInstanceFactory>();
    }

    private static Mock<ICombatInstanceRepository> CreateCombatRepositoryMock()
    {
        return new Mock<ICombatInstanceRepository>();
    }

    private static Mock<ICombatEncounterDraftGenerator> CreateEncounterDraftGeneratorMock()
    {
        return new Mock<ICombatEncounterDraftGenerator>();
    }

    private static ResolveCurrentEventCommandHandler CreateHandler(
        Mock<IRunRepository> repository,
        Mock<INodeEventResolverDispatcher> dispatcher,
        Mock<ICombatFactory>? combatFactory = null)
    {
        return new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object,
            CreateContentResolverMock().Object,
            CreateCatalogGatewayMock().Object,
            CreateCombatFactoryMock().Object,
            CreateCombatRepositoryMock().Object,
            CreateEncounterDraftGeneratorMock().Object,
            combatFactory?.Object ?? new Mock<ICombatFactory>().Object,
            new Mock<IRewardOfferRepository>().Object,
            new RewardOfferFactory(new Mock<Leds.GameEngine.Application.Combats.ICombatRiskProfileResolver>().Object));
    }

    [Fact]
    public async Task Handle_ShouldResolveCurrentEvent_AndKeepRunActive_WhenRoomBossIsNotResolved()
    {
        var run = TestGameEngineFactory.CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();

        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock();

        var handler = CreateHandler(repository, dispatcher);

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved.ToString());

        var resolvedNode = response.Run.CurrentRoom.Nodes
            .Single(node => node.Id == selectedNode.Id.Value);

        resolvedNode.State.Should().Be(NodeState.Resolved.ToString());

        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var dispatcher = CreateDispatcherMock();

        var handler = CreateHandler(repository, dispatcher);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenNoNodeWasSelected()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock();

        var handler = CreateHandler(repository, dispatcher);

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("No node has been selected for the current room depth.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldFail_WhenSelectedNodeAlreadyResolved()
    {
        // The node is already Resolved — no Selected node exists at current depth.
        var runWithNode = TestGameEngineFactory.CreateRunWithResolvedCurrentEvent(NodeEventType.Item);
        var run = runWithNode.Run;

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = CreateHandler(repository, CreateDispatcherMock());

        var act = () => handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<Leds.GameEngine.Domain.Common.DomainException>()
            .WithMessage("No node has been selected for the current room depth.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldNotModifyRoomMapTopology()
    {
        var run = TestGameEngineFactory.CreateRun();
        var nodeCountBefore = run.CurrentRoom.Nodes.Count;
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = CreateHandler(repository, CreateDispatcherMock());

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.Run.CurrentRoom.Nodes.Should().HaveCount(nodeCountBefore,
            because: "ResolveCurrentEvent must not add or remove nodes from the RoomMap.");
    }

    [Fact]
    public async Task ResolveCurrentEvent_ShouldKeepRunProgressionConsistent()
    {
        var run = TestGameEngineFactory.CreateRun();
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = CreateHandler(repository, CreateDispatcherMock());

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        // Run remains Active (boss not yet reached).
        response.Run.Status.Should().Be(RunStatus.Active.ToString());

        // Room transitioned to NodeResolved — ready for progress.
        response.Run.CurrentRoom.State.Should().Be(RoomState.NodeResolved.ToString());

        // CurrentNodeDepth has not advanced (ProgressRun is a separate command).
        response.Run.CurrentRoom.CurrentNodeDepth.Should().Be(0);

        // The target node is now Resolved.
        response.Run.CurrentRoom.Nodes
            .Single(n => n.Id == selectedNode.Id.Value)
            .State.Should().Be(NodeState.Resolved.ToString());

        // Repository was updated exactly once.
        repository.Verify(
            repo => repo.UpdateAsync(run, CancellationToken.None),
            Times.Once);

        // Outcome carries the correct node metadata.
        response.Outcome.NodeId.Should().Be(selectedNode.Id.Value);
        response.Outcome.Title.Should().NotBeNullOrWhiteSpace();
        response.Outcome.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_ShouldIncludeEncounterDraft_ForCombatEvent()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Combat);
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock(NodeEventResolutionKind.CombatStarted);

        var contentResolver = new Mock<IEventContentResolver>();
        contentResolver
            .Setup(r => r.ResolveAsync(It.IsAny<EventContentResolutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ResolvedNodeEventContent>.Success(
                new ResolvedCombatEventContent(
                    EventTemplateKey: "event-combat-shadow-v1",
                    EventTemplateVersion: "1.0.0",
                    Tags: new[] { "combat" },
                    EnemyTemplateKey: "enemy-shadow-v1",
                    EnemyTemplateVersion: "1.0.0",
                    RiskLevel: selectedNode.RiskLevel)));

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetEventTemplateByKeyAsync("event-combat-shadow-v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EventTemplateSnapshot>.Success(new EventTemplateSnapshot(
                "event-combat-shadow-v1", "Test Event", "", "1.0.0", "Active", "Combat",
                "CombatStarted", It.IsAny<int>(), It.IsAny<int>(), false, new[] { "combat" })));
        catalogGateway
            .Setup(g => g.GetEnemyTemplateByKeyAsync("enemy-shadow-v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EnemyTemplateSnapshot>.Success(new EnemyTemplateSnapshot(
                "enemy-shadow-v1", "Shadow", "", "1.0.0", "Active", 30, 8, 4, 6, "Shadow",
                new[] { "skill-shadow-strike-v1" })));

        var draftEnemy = new CatalogEnemyDefinition(
            "enemy.threshold.doubt-fragment", "Fragment de Doute", "", "Fragile",
            new[] { "Threshold" }, 1, 1, 2, new[] { "fragile" }, new[] { "skill.basic.strike" });

        catalogGateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draftEnemy });

        var draftGenerator = new Mock<ICombatEncounterDraftGenerator>();
        var expectedDraft = new CombatEncounterDraft(
            run.Id.Value, run.CurrentRoom.Id.Value, selectedNode.Id.Value,
            "Threshold", 0, selectedNode.RiskLevel, "Combat",
            new[] { new CombatEncounterDraftEnemy(
                "enemy.threshold.doubt-fragment", "Fragment de Doute", "", "Fragile",
                1, 1, 2, new[] { "fragile" }, new[] { "skill.basic.strike" }, Skills: []) },
            new[] { new CombatEncounterDraftAlly("player.self", "Le Joueur", "Protagonist", new[] { "player" }) });

        draftGenerator
            .Setup(g => g.GenerateAsync(It.IsAny<CombatEncounterDraftContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDraft);

        var combatFactory = new Mock<ICombatInstanceFactory>();
        var combatants = new CombatantSnapshot[]
        {
            CombatantSnapshot.Create("player.self", "Player", CombatantSide.Player, 100, 10, 5, 7),
            CombatantSnapshot.Create("enemy-shadow-v1", "Shadow", CombatantSide.Enemy, 30, 8, 4, 6)
        };
        combatFactory
            .Setup(f => f.CreateFromEnemyTemplate(It.IsAny<EnemyTemplateSnapshot>()))
            .Returns(CombatInstance.Create(combatants));

        var runtimeCombat = new CombatFactory().CreateFromDraft(expectedDraft);
        var runtimeFactoryMock = new Mock<ICombatFactory>();
        runtimeFactoryMock
            .Setup(f => f.CreateFromDraft(
                It.IsAny<CombatEncounterDraft>(),
                It.IsAny<PlayerRuntimeState?>(),
                It.IsAny<IReadOnlyCollection<RunModifier>?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(runtimeCombat);

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object,
            contentResolver.Object,
            catalogGateway.Object,
            combatFactory.Object,
            new Mock<ICombatInstanceRepository>().Object,
            draftGenerator.Object,
            runtimeFactoryMock.Object,
            new Mock<IRewardOfferRepository>().Object,
            new RewardOfferFactory(new Mock<Leds.GameEngine.Application.Combats.ICombatRiskProfileResolver>().Object));

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.EncounterDraft.Should().NotBeNull();
        response.EncounterDraft!.EncounterType.Should().Be("Combat");
        response.EncounterDraft.Enemies.Should().NotBeEmpty();
        response.EncounterDraft.Allies.Should().NotBeEmpty();
        response.EncounterDraft.Enemies.Should().Contain(e =>
            e.EnemyKey == "enemy.threshold.doubt-fragment");
    }

    [Fact]
    public async Task Handle_ShouldIncludePersistedCombat_ForCombatEvent()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Combat);
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var dispatcher = CreateDispatcherMock(NodeEventResolutionKind.CombatStarted);

        var contentResolver = new Mock<IEventContentResolver>();
        contentResolver
            .Setup(r => r.ResolveAsync(It.IsAny<EventContentResolutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ResolvedNodeEventContent>.Success(
                new ResolvedCombatEventContent(
                    EventTemplateKey: "event-combat-shadow-v1",
                    EventTemplateVersion: "1.0.0",
                    Tags: new[] { "combat" },
                    EnemyTemplateKey: "enemy-shadow-v1",
                    EnemyTemplateVersion: "1.0.0",
                    RiskLevel: selectedNode.RiskLevel)));

        var catalogGateway = new Mock<ICatalogContentGateway>();
        catalogGateway
            .Setup(g => g.GetEventTemplateByKeyAsync("event-combat-shadow-v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EventTemplateSnapshot>.Success(new EventTemplateSnapshot(
                "event-combat-shadow-v1", "Test Event", "", "1.0.0", "Active", "Combat",
                "CombatStarted", It.IsAny<int>(), It.IsAny<int>(), false, new[] { "combat" })));
        catalogGateway
            .Setup(g => g.GetEnemyTemplateByKeyAsync("enemy-shadow-v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EnemyTemplateSnapshot>.Success(new EnemyTemplateSnapshot(
                "enemy-shadow-v1", "Shadow", "", "1.0.0", "Active", 30, 8, 4, 6, "Shadow",
                new[] { "skill-shadow-strike-v1" })));

        var draftEnemy = new CatalogEnemyDefinition(
            "enemy.threshold.doubt-fragment", "Fragment de Doute", "", "Fragile",
            new[] { "Threshold" }, 1, 1, 2, new[] { "fragile" }, new[] { "skill.basic.strike" });

        catalogGateway
            .Setup(g => g.ListCompatibleEnemyDefinitionsAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draftEnemy });

        var draftGenerator = new Mock<ICombatEncounterDraftGenerator>();
        var expectedDraft = new CombatEncounterDraft(
            run.Id.Value, run.CurrentRoom.Id.Value, selectedNode.Id.Value,
            "Threshold", 0, selectedNode.RiskLevel, "Combat",
            new[] { new CombatEncounterDraftEnemy(
                "enemy.threshold.doubt-fragment", "Fragment de Doute", "", "Fragile",
                1, 1, 2, new[] { "fragile" }, new[] { "skill.basic.strike" }, Skills: []) },
            new[] { new CombatEncounterDraftAlly("player.self", "Le Joueur", "Protagonist", new[] { "player" }) });

        draftGenerator
            .Setup(g => g.GenerateAsync(It.IsAny<CombatEncounterDraftContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDraft);

        var combatFactory = new Mock<ICombatInstanceFactory>();
        var combatants = new CombatantSnapshot[]
        {
            CombatantSnapshot.Create("player.self", "Player", CombatantSide.Player, 100, 10, 5, 7),
            CombatantSnapshot.Create("enemy-shadow-v1", "Shadow", CombatantSide.Enemy, 30, 8, 4, 6)
        };
        combatFactory
            .Setup(f => f.CreateFromEnemyTemplate(It.IsAny<EnemyTemplateSnapshot>()))
            .Returns(CombatInstance.Create(combatants));

        var runtimeCombat = new CombatFactory().CreateFromDraft(expectedDraft);
        var runtimeFactoryMock = new Mock<ICombatFactory>();
        runtimeFactoryMock
            .Setup(f => f.CreateFromDraft(
                It.IsAny<CombatEncounterDraft>(),
                It.IsAny<PlayerRuntimeState?>(),
                It.IsAny<IReadOnlyCollection<RunModifier>?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(runtimeCombat);

        var handler = new ResolveCurrentEventCommandHandler(
            repository.Object,
            dispatcher.Object,
            contentResolver.Object,
            catalogGateway.Object,
            combatFactory.Object,
            new Mock<ICombatInstanceRepository>().Object,
            draftGenerator.Object,
            runtimeFactoryMock.Object,
            new Mock<IRewardOfferRepository>().Object,
            new RewardOfferFactory(new Mock<Leds.GameEngine.Application.Combats.ICombatRiskProfileResolver>().Object));

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.EncounterDraft.Should().NotBeNull();
        response.EncounterDraft!.EncounterType.Should().Be("Combat");
        response.EncounterDraft.Enemies.Should().Contain(e =>
            e.EnemyKey == "enemy.threshold.doubt-fragment");
    }

    [Fact]
    public async Task Handle_ShouldNotIncludeEncounterDraft_ForNonCombatEvent()
    {
        var run = TestGameEngineFactory.CreateRun(NodeEventType.Item);
        var selectedNode = run.CurrentRoom.AvailableNodes.First();
        run.ChooseNode(selectedNode.Id);

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var handler = CreateHandler(repository, CreateDispatcherMock());

        var response = await handler.Handle(
            new ResolveCurrentEventCommand(run.Id.Value),
            CancellationToken.None);

        response.EncounterDraft.Should().BeNull();
    }
}