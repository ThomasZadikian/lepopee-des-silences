using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.NpcInteractions;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;


namespace RPG_ESI07.Tests.Application.Commands.NpcInteractions;

public class CreateNpcInteractionHandlerTests
{
    private readonly Mock<INpcInteractionRepository> _mockRepo;
    private readonly CreateNpcInteractionHandler _handler;

    public CreateNpcInteractionHandlerTests()
    {
        _mockRepo = new Mock<INpcInteractionRepository>(MockBehavior.Strict);
        _handler = new CreateNpcInteractionHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_CreatesInteraction_AndReturnsResponse()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<NpcInteraction>())).Returns(Task.CompletedTask);

        var command = new CreateNpcInteractionCommand(NpcId: 1, PlayerId: 1, EventType: "dialogue");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        _mockRepo.Verify(r => r.AddAsync(It.Is<NpcInteraction>(i =>
            i.NpcId == 1 &&
            i.PlayerId == 1 &&
            i.EventType == "dialogue"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_SetsInteractedAt_ToUtcNow()
    {
        NpcInteraction? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<NpcInteraction>()))
                 .Callback<NpcInteraction>(i => captured = i)
                 .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateNpcInteractionCommand(1, 1, "trade"), CancellationToken.None);

        captured!.InteractedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class DeleteNpcInteractionHandlerTests
{
    private readonly Mock<INpcInteractionRepository> _mockRepo;
    private readonly DeleteNpcInteractionHandler _handler;

    public DeleteNpcInteractionHandlerTests()
    {
        _mockRepo = new Mock<INpcInteractionRepository>(MockBehavior.Strict);
        _handler = new DeleteNpcInteractionHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_DeletesInteraction_AndReturnsSuccess()
    {
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new DeleteNpcInteractionCommand(1), CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}