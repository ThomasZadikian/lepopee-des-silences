using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Skills;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Skills;

public class CreateSkillHandlerTests
{
    private readonly Mock<ISkillRepository> _mockRepo;
    private readonly CreateSkillHandler _handler;

    public CreateSkillHandlerTests()
    {
        _mockRepo = new Mock<ISkillRepository>();
        _handler = new CreateSkillHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new CreateSkillCommand("Fireball", Constants.EffectDamage, 20);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Skill>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Skill created successfully");
    }
}

public class UpdateSkillHandlerTests
{
    private readonly Mock<ISkillRepository> _mockRepo;
    private readonly UpdateSkillHandler _handler;

    public UpdateSkillHandlerTests()
    {
        _mockRepo = new Mock<ISkillRepository>();
        _handler = new UpdateSkillHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingEntity_UpdatesFields()
    {
        // Arrange
        var entity = new Skill { Id = 1, Name = "Old", EffectType = Constants.EffectDamage, MPCost = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Skill>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new UpdateSkillCommand(1, "Blizzard", Constants.EffectDamage, 25), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.Name.Should().Be("Blizzard");
        entity.MPCost.Should().Be(25);
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ReturnsFailure()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Skill?)null);

        // Act
        var result = await _handler.Handle(new UpdateSkillCommand(99, "X", "X", 0), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }
}

public class DeleteSkillHandlerTests
{
    private readonly Mock<ISkillRepository> _mockRepo;
    private readonly DeleteSkillHandler _handler;

    public DeleteSkillHandlerTests()
    {
        _mockRepo = new Mock<ISkillRepository>();
        _handler = new DeleteSkillHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidId_CallsDeleteOnce()
    {
        // Arrange
        _mockRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeleteSkillCommand(1), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}