
using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Commands.Npcs;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Commands.NpcInteractions; 

public class CreateNpcHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly CreateNpcHandler _handler;

    public CreateNpcHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new CreateNpcHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_CreatesNpc_AndReturnsResponse()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Npc>())).Returns(Task.CompletedTask);

        var command = new CreateNpcCommand(
            Name: "Aldric", Type: Constants.NpcTypeNeutral,
            Description: "Un marchand", Zone: "Dev Island",
            SpawnX: 10f, SpawnY: 20f, InfluenceRadius: 5f,
            TransitionMatrix: null, MapStates: null,
            InitialState: Constants.NpcStateSerein,
            Dialogues: null, IsMerchant: false,
            MerchantInventory: null, Quests: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        _mockRepo.Verify(r => r.AddAsync(It.Is<Npc>(n =>
            n.Name == "Aldric" &&
            n.Zone == "Dev Island"
        )), Times.Once);
    }
}

public class UpdateNpcHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly UpdateNpcHandler _handler;

    public UpdateNpcHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new UpdateNpcHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_UpdatesNpc_AndReturnsSuccess()
    {
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Npc>())).Returns(Task.CompletedTask);

        var command = new UpdateNpcCommand(
            Id: 1,
            Name: "Aldric Modifié", Type: Constants.NpcTypeNeutral,
            Description: "Description mise à jour", Zone: "Dev Island",
            SpawnX: 15f, SpawnY: 25f, InfluenceRadius: 5f,
            TransitionMatrix: null, MapStates: null,
            InitialState: Constants.NpcStateSerein,
            Dialogues: null, IsMerchant: true,
            MerchantInventory: null, Quests: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.UpdateAsync(It.Is<Npc>(n =>
            n.Id == 1 && n.IsMerchant == true
        )), Times.Once);
    }
}

public class DeleteNpcHandlerTests
{
    private readonly Mock<INpcRepository> _mockRepo;
    private readonly DeleteNpcHandler _handler;

    public DeleteNpcHandlerTests()
    {
        _mockRepo = new Mock<INpcRepository>(MockBehavior.Strict);
        _handler = new DeleteNpcHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_DeletesNpc_AndReturnsSuccess()
    {
        _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(new DeleteNpcCommand(1), CancellationToken.None);

        result.Success.Should().BeTrue();
        _mockRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}
