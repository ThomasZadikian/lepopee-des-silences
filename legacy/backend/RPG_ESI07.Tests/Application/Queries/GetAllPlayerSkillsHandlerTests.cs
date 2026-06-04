using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.PlayerSkills;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllPlayerSkillsHandlerTests
{
    private readonly Mock<IPlayerSkillRepository> _mockRepo;
    private readonly GetAllPlayerSkillsHandler _handler;

    public GetAllPlayerSkillsHandlerTests()
    {
        _mockRepo = new Mock<IPlayerSkillRepository>(MockBehavior.Strict);
        _handler = new GetAllPlayerSkillsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<PlayerSkill> { new() { Id = 1 }, new() { Id = 2 } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllPlayerSkillsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PlayerSkill>());

        var result = await _handler.Handle(new GetAllPlayerSkillsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}