using FluentAssertions;
using Leds.GameEngine.Application.Abstractions;
using Leds.GameEngine.Application.Common.Exceptions;
using Leds.GameEngine.Application.PalaceLaws.Ports;
using Leds.GameEngine.Application.Runs.GetRunById;
using Leds.GameEngine.Application.Runs.PalaceIndicators;
using Leds.GameEngine.Domain.PalaceLaws;
using Leds.GameEngine.Domain.Runs;
using Leds.GameEngine.UnitTests.Common.Factories;
using Moq;

namespace Leds.GameEngine.UnitTests.Runs.GetRunById;

public sealed class GetRunByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnRun_WhenRunExists()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository();
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.Id.Should().Be(run.Id.Value);
        response.Run.PlayerId.Should().Be(run.PlayerId);
        response.Run.Status.Should().Be(RunStatus.Active.ToString());
        var allNodes = response.Run.CurrentRoom.Nodes.ToArray();

        allNodes.Should().HaveCount(response.Run.CurrentRoom.TotalNodeCount);
        allNodes.Should().HaveCount(6);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .HaveCount(2);

        response.Run.CurrentRoom.AvailableNodes
            .Should()
            .OnlyContain(node => node.State == "Available" && node.Row == 0);

        allNodes
            .Where(node => node.Row > 0)
            .Should()
            .OnlyContain(node => node.State == "Planned");

        allNodes
            .Should()
            .ContainSingle(node => node.IsBoss);
    }

    [Fact]
    public async Task Handle_ShouldReturnPartySnapshot_WhenRunHasPlayerSnapshot()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository();
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.Party.Should().NotBeNull();
        response.Run.Party!.Members.Should().HaveCount(1);

        var member = response.Run.Party.Members.First();
        member.DisplayName.Should().Be("Le Porteur");
        member.DefinitionKey.Should().Be("character.player.self");
        member.MaxVitality.Should().Be(100);
        member.CurrentVitality.Should().BeGreaterThan(0);
        member.IsActive.Should().BeTrue();
        member.IsDefeated.Should().BeFalse();
        member.Skills.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_PartySnapshot_ShouldNotDependOnActiveCombat()
    {
        var run = TestGameEngineFactory.CreateRunWithPlayerSnapshot();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository();
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.ActiveCombatId.Should().BeNull();
        response.Run.Party.Should().NotBeNull("party snapshot must be available outside of combat");
    }

    [Fact]
    public async Task Handle_ShouldReturnNullParty_WhenRunHasNoPlayerSnapshot()
    {
        var run = TestGameEngineFactory.CreateRun();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository();
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.Party.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenRunDoesNotExist()
    {
        var runId = Guid.NewGuid();

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(new RunId(runId), CancellationToken.None))
            .ReturnsAsync((Run?)null);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository();
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var act = () => handler.Handle(
            new GetRunByIdQuery(runId),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"Run with id '{runId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnPublicPalaceIndicators_WhenIndicatorsExist()
    {
        var run = TestGameEngineFactory.CreateRun();
        var indicator = PalaceIndicator.Create(
            run.Id.Value,
            "palace.whispers",
            "Murmures du Palais",
            "Le Palais observe la traversée.",
            "high",
            sourceDecisionId: Guid.NewGuid());

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository([indicator]);
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.PalaceIndicators.Should().ContainSingle();
        var publicIndicator = response.Run.PalaceIndicators!.Single();
        publicIndicator.Key.Should().Be("palace.whispers");
        publicIndicator.Label.Should().Be("Murmures du Palais");
        publicIndicator.Description.Should().Be("Le Palais observe la traversée.");
        publicIndicator.Level.Should().Be("high");
        publicIndicator.Source.Should().Be("run");
    }

    [Fact]
    public async Task Handle_ShouldReturnProjectedPublicPalaceIndicators_FromRunState()
    {
        var run = TestGameEngineFactory.CreateRun();
        run.ActivatePalaceLaw(PalaceLaw.Create(
            "law-silence-v1",
            "Loi du Silence",
            "v1",
            [PalaceLawDomain.Narrative]));

        var repository = new Mock<IRunRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(run.Id, CancellationToken.None))
            .ReturnsAsync(run);

        var palaceIndicatorRepository = CreatePalaceIndicatorRepository();
        var handler = CreateHandler(repository.Object, palaceIndicatorRepository.Object);

        var response = await handler.Handle(
            new GetRunByIdQuery(run.Id.Value),
            CancellationToken.None);

        response.Run.PalaceIndicators.Should().ContainSingle(indicator =>
            indicator.Key == "law:law-silence-v1" &&
            indicator.Category == "law" &&
            indicator.Source == "law");
    }

    private static GetRunByIdQueryHandler CreateHandler(
        IRunRepository runRepository,
        IPalaceIndicatorRepository palaceIndicatorRepository)
    {
        return new GetRunByIdQueryHandler(
            runRepository,
            palaceIndicatorRepository,
            new PalacePublicIndicatorProjectionService());
    }

    private static Mock<IPalaceIndicatorRepository> CreatePalaceIndicatorRepository(
        IReadOnlyCollection<PalaceIndicator>? indicators = null)
    {
        var repository = new Mock<IPalaceIndicatorRepository>();
        repository
            .Setup(repo => repo.GetByRunIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(indicators ?? []);

        return repository;
    }
}
