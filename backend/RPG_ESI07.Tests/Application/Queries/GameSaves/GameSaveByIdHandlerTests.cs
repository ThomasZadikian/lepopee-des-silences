using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.GameSaves;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GameSaveByIdHandlerTests
{
    private readonly Mock<IGameSaveRepository> _mockRepo;
    private readonly GetSaveByIdHandler _handler;

    public GameSaveByIdHandlerTests()
    {
        _mockRepo = new Mock<IGameSaveRepository>();
        _handler = new GetSaveByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingId_ReturnsGameSave()
    {
        var save = new GameSave { Id = 1, PlayerId = 5, CurrentZone = "Forêt", SavedAt = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(save);

        var result = await _handler.Handle(new GetSaveByIdQuery(1, 5, false), CancellationToken.None);

        result.GameSave.Should().BeEquivalentTo(save);
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsInvalidOperationException()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((GameSave?)null);

        var act = async () => await _handler.Handle(new GetSaveByIdQuery(99, 1, false), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_NonAdminAccessingOtherPlayerSave_ThrowsUnauthorized()
    {
        var save = new GameSave { Id = 1, PlayerId = 10, CurrentZone = "Donjon", SavedAt = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(save);

        var act = async () => await _handler.Handle(new GetSaveByIdQuery(1, 5, false), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_AdminCanAccessAnyPlayerSave()
    {
        var save = new GameSave { Id = 1, PlayerId = 10, CurrentZone = "Donjon", SavedAt = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(save);

        var result = await _handler.Handle(new GetSaveByIdQuery(1, 99, true), CancellationToken.None);

        result.GameSave.Should().BeEquivalentTo(save);
    }

    [Fact]
    public async Task Handle_OwnerCanAccessOwnSave()
    {
        var save = new GameSave { Id = 1, PlayerId = 5, CurrentZone = "Plaine", SavedAt = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(save);

        var result = await _handler.Handle(new GetSaveByIdQuery(1, 5, false), CancellationToken.None);

        result.GameSave.Should().BeEquivalentTo(save);
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectId()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((GameSave?)null);

        try { await _handler.Handle(new GetSaveByIdQuery(7, 1, false), CancellationToken.None); }
        catch (InvalidOperationException) { }

        _mockRepo.Verify(r => r.GetByIdAsync(7), Times.Once);
    }
}
