using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.UserConsents;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetAllUserConsentsHandlerTests
{
    private readonly Mock<IUserConsentRepository> _mockRepo;
    private readonly GetAllUserConsentsHandler _handler;

    public GetAllUserConsentsHandlerTests()
    {
        _mockRepo = new Mock<IUserConsentRepository>(MockBehavior.Strict);
        _handler = new GetAllUserConsentsHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAllItems()
    {
        var items = new List<UserConsent> { new() { Id = 1 }, new() { Id = 2 } };
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

        var result = await _handler.Handle(new GetAllUserConsentsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsEmptyResponse()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserConsent>());

        var result = await _handler.Handle(new GetAllUserConsentsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }
}