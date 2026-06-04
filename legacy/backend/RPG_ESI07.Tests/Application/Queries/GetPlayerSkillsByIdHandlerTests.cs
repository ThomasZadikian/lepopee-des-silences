using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.PlayerSkills;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetPlayerSkillByIdHandlerTests
{
    private readonly Mock<IPlayerSkillRepository> _mockRepo;
    private readonly GetPlayerSkillByIdHandler _handler;

    public GetPlayerSkillByIdHandlerTests()
    {
        _mockRepo = new Mock<IPlayerSkillRepository>(MockBehavior.Strict);
        _handler = new GetPlayerSkillByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsOwner()
    {
        var entity = new PlayerSkill { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetPlayerSkillByIdQuery(1, 10, false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsAdmin()
    {
        var entity = new PlayerSkill { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetPlayerSkillByIdQuery(1, 99, true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenIdDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((PlayerSkill?)null);
        var query = new GetPlayerSkillByIdQuery(999, 1, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("PlayerSkill not found");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAdmin()
    {
        var entity = new PlayerSkill { Id = 1, PlayerId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetPlayerSkillByIdQuery(1, 5, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Access forbidden");
    }
}