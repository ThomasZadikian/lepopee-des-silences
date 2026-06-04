using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.GameSaves;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllGameSavesHandlerTests
{
    private readonly Mock<IGameSaveRepository> _mockRepo;
    private readonly GetAllGameSavesHandler _handler;

    public GetAllGameSavesHandlerTests()
    {
        _mockRepo = new Mock<IGameSaveRepository>(MockBehavior.Strict);
        _handler = new GetAllGameSavesHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<GameSave> { new() { Id = 1 }, new() { Id = 2 } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllGameSavesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<GameSave>());

        var result = await _handler.Handle(new GetAllGameSavesQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}