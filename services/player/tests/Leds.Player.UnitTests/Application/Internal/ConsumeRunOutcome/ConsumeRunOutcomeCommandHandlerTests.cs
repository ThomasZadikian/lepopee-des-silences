using FluentAssertions;
using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Internal.ConsumeRunOutcome;
using Leds.Player.Domain.Players;
using Moq;

namespace Leds.Player.UnitTests.Application.Internal.ConsumeRunOutcome;

public sealed class ConsumeRunOutcomeCommandHandlerTests
{
    private readonly Mock<IPlayerProfileRepository> _profileRepository;
    private readonly Mock<IProcessedIntegrationEventRepository> _processedEvents;
    private readonly Mock<TimeProvider> _timeProvider;
    private readonly ConsumeRunOutcomeCommandHandler _handler;

    public ConsumeRunOutcomeCommandHandlerTests()
    {
        _profileRepository = new Mock<IPlayerProfileRepository>();
        _processedEvents = new Mock<IProcessedIntegrationEventRepository>();
        _timeProvider = new Mock<TimeProvider>();
        _timeProvider.Setup(tp => tp.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        _handler = new ConsumeRunOutcomeCommandHandler(
            _profileRepository.Object,
            _processedEvents.Object,
            _timeProvider.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnIdempotentResponse_WhenEventAlreadyProcessed()
    {
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            "{}");

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.Reason.Should().Be("Event already processed (idempotent).");
        _profileRepository.Verify(pr => pr.SaveAsync(It.IsAny<PlayerProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidPayload_WhenPayloadJsonIsNull()
    {
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            "null");

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.Reason.Should().Be("Invalid payload.");
    }

    [Fact]
    public async Task Handle_ShouldThrowJsonException_WhenPayloadJsonIsEmpty()
    {
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            "");

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowJsonException_WhenPayloadJsonIsInvalidJson()
    {
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            "{invalid json}");

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnPlayerNotFound_WhenProfileDoesNotExist()
    {
        var playerId = Guid.NewGuid();
        var payload = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.IsAny<PlayerId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerProfile?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ShouldIncrementRunsCompleted_WhenEventTypeIsRunCompleted()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var payload = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId.Value}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.Is<PlayerId>(id => id.Value == playerId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.Reason.Should().BeNull();
        profile.Progression.TotalRunsCompleted.Should().Be(1);
        _profileRepository.Verify(pr => pr.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncrementRunsFailed_WhenEventTypeIsRunFailed()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var payload = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId.Value}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunFailedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.Is<PlayerId>(id => id.Value == playerId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.Reason.Should().BeNull();
        profile.Progression.TotalRunsFailed.Should().Be(1);
        _profileRepository.Verify(pr => pr.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncrementRunsAbandoned_WhenEventTypeIsRunAbandoned()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var payload = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId.Value}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunAbandonedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.Is<PlayerId>(id => id.Value == playerId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeTrue();
        result.Reason.Should().BeNull();
        profile.Progression.TotalRunsAbandoned.Should().Be(1);
        _profileRepository.Verify(pr => pr.SaveAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnknownEventType_WhenEventTypeIsNotRecognized()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var payload = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId.Value}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "UnknownEventType",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.Is<PlayerId>(id => id.Value == playerId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Processed.Should().BeFalse();
        result.Reason.Should().Contain("Unknown event type");
        _profileRepository.Verify(pr => pr.SaveAsync(It.IsAny<PlayerProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldMarkEventAsProcessed_WhenSuccessfullyProcessed()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var eventId = Guid.NewGuid();
        var payload = $"{{\"EventId\":\"{eventId}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId.Value}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            eventId,
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.Is<PlayerId>(id => id.Value == playerId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await _handler.Handle(command, CancellationToken.None);

        _processedEvents.Verify(pe => pe.MarkProcessedAsync(
            eventId,
            "RunCompletedIntegrationEvent",
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldTouchProfile_WhenSuccessfullyProcessed()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var now = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var payload = $"{{\"EventId\":\"{Guid.NewGuid()}\",\"OccurredAtUtc\":\"2024-01-01T00:00:00Z\",\"RunId\":\"{Guid.NewGuid()}\",\"PlayerId\":\"{playerId.Value}\",\"Seed\":\"seed123\",\"FinalDepth\":5,\"GeneratorVersion\":\"v1.0.0\"}}";
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            payload);

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _profileRepository.Setup(pr => pr.GetByIdAsync(It.Is<PlayerId>(id => id.Value == playerId.Value), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _timeProvider.Setup(tp => tp.GetUtcNow()).Returns(now);

        await _handler.Handle(command, CancellationToken.None);

        profile.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task Handle_ShouldNotModifyProgression_WhenEventIsIdempotent()
    {
        var playerId = PlayerId.New();
        var profile = PlayerProfile.Create("Test Player", DateTimeOffset.UtcNow);
        var command = new ConsumeRunOutcomeCommand(
            Guid.NewGuid(),
            "RunCompletedIntegrationEvent",
            "1.0",
            DateTime.UtcNow,
            "{}");

        _processedEvents.Setup(pe => pe.HasProcessedAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var initialCompleted = profile.Progression.TotalRunsCompleted;
        await _handler.Handle(command, CancellationToken.None);

        profile.Progression.TotalRunsCompleted.Should().Be(initialCompleted);
    }
}
