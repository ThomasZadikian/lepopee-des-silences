using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.BestiaryUnlocks;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllBestiaryUnlocksHandlerTests
{
    private readonly Mock<IBestiaryUnlockRepository> _mockRepo;
    private readonly GetAllBestiaryUnlocksHandler _handler;

    public GetAllBestiaryUnlocksHandlerTests()
    {
        _mockRepo = new Mock<IBestiaryUnlockRepository>();
        _handler = new GetAllBestiaryUnlocksHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<BestiaryUnlock>
        {
            new() { Id = 1, PlayerId = 1, EnemyId = 1, UnlockedAt = DateTime.UtcNow },
            new() { Id = 2, PlayerId = 1, EnemyId = 2, UnlockedAt = DateTime.UtcNow },
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllBestiaryUnlocksQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BestiaryUnlock>());

        var result = await _handler.Handle(new GetAllBestiaryUnlocksQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FiltersByUserId_WhenSpecified()
    {
        var items = new List<BestiaryUnlock>
        {
            new() { Id = 1, PlayerId = 1, UnlockedAt = DateTime.UtcNow },
            new() { Id = 2, PlayerId = 2, UnlockedAt = DateTime.UtcNow },
        };
        _mockRepo.Setup(r => r.GetByPlayerIdAsync(1)).ReturnsAsync(new List<BestiaryUnlock> { items[0] });

        var result = await _handler.Handle(new GetAllBestiaryUnlocksQuery(UserId: 1), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CallsRepositoryOnce()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<BestiaryUnlock>());

        await _handler.Handle(new GetAllBestiaryUnlocksQuery(), CancellationToken.None);

        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
