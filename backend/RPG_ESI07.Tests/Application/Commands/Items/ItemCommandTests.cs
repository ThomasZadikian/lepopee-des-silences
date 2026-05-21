using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Items;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.Items;

public class CreateItemHandlerTests
{
    private readonly Mock<IItemRepository> _mockRepo;
    private readonly CreateItemHandler _handler;

    public CreateItemHandlerTests()
    {
        _mockRepo = new Mock<IItemRepository>();
        _handler = new CreateItemHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = new CreateItemCommand("Épée", Constants.ItemTypeWeapon, 100, null, "Une épée puissante", 15, null);
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Item>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Item created successfully");
    }
}

public class UpdateItemHandlerTests
{
    private readonly Mock<IItemRepository> _mockRepo;
    private readonly UpdateItemHandler _handler;

    public UpdateItemHandlerTests()
    {
        _mockRepo = new Mock<IItemRepository>();
        _handler = new UpdateItemHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingEntity_UpdatesFields()
    {
        // Arrange
        var entity = new Item { Id = 1, Name = "Old", Type = Constants.ItemTypeWeapon, Price = 50 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Item>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new UpdateItemCommand(1, "New", Constants.ItemTypeArmor, 200, null, "Armure renforcée", 20, null), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        entity.Name.Should().Be("New");
        entity.Type.Should().Be(Constants.ItemTypeArmor);
        entity.Price.Should().Be(200);
    }

    [Fact]
    public async Task Handle_NonExistingEntity_ReturnsFailure()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Item?)null);

        // Act
        var result = await _handler.Handle(new UpdateItemCommand(99, "X", "X", 0, null, null, null, null), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
    }
}

public class DeleteItemHandlerTests
{
    private readonly Mock<IItemRepository> _mockRepo;
    private readonly DeleteItemHandler _handler;

    public DeleteItemHandlerTests()
    {
        _mockRepo = new Mock<IItemRepository>();
        _handler = new DeleteItemHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidId_CallsDeleteOnce()
    {
        // Arrange
        _mockRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new DeleteItemCommand(1), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}