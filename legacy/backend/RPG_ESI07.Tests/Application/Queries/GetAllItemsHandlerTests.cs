using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Items;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllItemsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var mockRepo = new Mock<IItemRepository>();
        var items = new List<Item> { new() { Id = 1, Name = "Sword" }, new() { Id = 2, Name = "Shield" } };
        mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        var handler = new GetAllItemsHandler(mockRepo.Object);

        var result = await handler.Handle(new GetAllItemsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Sword");
    }
}