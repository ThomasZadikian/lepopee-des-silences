using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.PlayerInventorys;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllPlayerInventorysHandlerTests
{
    private readonly Mock<IPlayerInventoryRepository> _mockRepo;
    private readonly GetAllPlayerInventorysHandler _handler;

    public GetAllPlayerInventorysHandlerTests()
    {
        _mockRepo = new Mock<IPlayerInventoryRepository>(MockBehavior.Strict);
        _handler = new GetAllPlayerInventorysHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<PlayerInventory> { new() { Id = 1 }, new() { Id = 2 } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllPlayerInventorysQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerInventory>());

        var result = await _handler.Handle(new GetAllPlayerInventorysQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}