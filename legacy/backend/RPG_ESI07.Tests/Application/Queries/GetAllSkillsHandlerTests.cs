using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Skills;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllSkillsHandlerTests
{
    private readonly Mock<ISkillRepository> _mockRepo;
    private readonly GetAllSkillsHandler _handler;

    public GetAllSkillsHandlerTests()
    {
        _mockRepo = new Mock<ISkillRepository>(MockBehavior.Strict);
        _handler = new GetAllSkillsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<Skill> { new() { Id = 1 }, new() { Id = 2 } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllSkillsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Skill>());

        var result = await _handler.Handle(new GetAllSkillsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}