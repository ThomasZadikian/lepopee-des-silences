using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.UserConsents;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetUserConsentByIdHandlerTests
{
    private readonly Mock<IUserConsentRepository> _mockRepo;
    private readonly GetUserConsentByIdHandler _handler;

    public GetUserConsentByIdHandlerTests()
    {
        _mockRepo = new Mock<IUserConsentRepository>(MockBehavior.Strict);
        _handler = new GetUserConsentByIdHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsOwner()
    {
        var entity = new UserConsent { Id = 1, UserId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetUserConsentByIdQuery(1, 10, false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsEntity_WhenUserIsAdmin()
    {
        var entity = new UserConsent { Id = 1, UserId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetUserConsentByIdQuery(1, 99, true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenIdDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((UserConsent?)null);
        var query = new GetUserConsentByIdQuery(999, 1, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("UserConsent not found");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorizedAccessException_WhenUserIsNotOwnerOrAdmin()
    {
        var entity = new UserConsent { Id = 1, UserId = 10 };
        _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        var query = new GetUserConsentByIdQuery(1, 5, false);

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Access forbidden");
    }
}