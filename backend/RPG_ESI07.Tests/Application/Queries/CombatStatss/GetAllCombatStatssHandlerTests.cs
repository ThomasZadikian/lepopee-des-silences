using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.CombatStatss;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllCombatStatssHandlerTests
{
    private readonly Mock<ICombatStatsRepository> _mockRepo;
    private readonly GetAllCombatStatssHandler _handler;

    public GetAllCombatStatssHandlerTests()
    {
        _mockRepo = new Mock<ICombatStatsRepository>();
        _handler = new GetAllCombatStatssHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<CombatStats>
        {
            new() { PlayerId = 1, TotalCombats = 10, CombatsWon = 5, CombatsLost = 5 },
            new() { PlayerId = 2, TotalCombats = 20, CombatsWon = 15, CombatsLost = 5 },
        };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllCombatStatssQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats>());

        var result = await _handler.Handle(new GetAllCombatStatssQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CallsRepositoryOnce()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<CombatStats>());

        await _handler.Handle(new GetAllCombatStatssQuery(), CancellationToken.None);

        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
