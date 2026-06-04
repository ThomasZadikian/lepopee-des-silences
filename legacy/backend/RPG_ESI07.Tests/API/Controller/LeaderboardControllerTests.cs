using MediatR;
using Microsoft.AspNetCore.Mvc;
using RPG_ESI07.Application.Queries.Leaderboard;
using FluentAssertions;
using Moq;
using RPG_ESI07.API.Controllers;

namespace RPG_ESI07.Tests.API.Controllers;

public class LeaderboardControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _controller = new LeaderboardController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Get_ReturnsOk_WithLeaderboardData()
    {
        var expected = new GetLeaderboardResponse(
            new List<LeaderboardEntryDto>
            {
                new(1, "Kael", 25, 298, 145000, 2880)
            },
            TotalCount: 1,
            Page: 1,
            PageSize: 50,
            TotalPages: 1);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLeaderboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Get("level", 1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Get_DefaultParameters_SortByLevel_Page1()
    {
        var expected = new GetLeaderboardResponse(
            new List<LeaderboardEntryDto>(), 0, 1, 50, 0);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetLeaderboardQuery>(q => q.SortBy == "level" && q.Page == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Get();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_CallsMediatorOnce()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetLeaderboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetLeaderboardResponse(new List<LeaderboardEntryDto>(), 0, 1, 50, 0));

        await _controller.Get("combatswon", 1);

        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetLeaderboardQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task Search_ReturnsOk_WithResults()
    {
        var expected = new SearchLeaderboardResponse(
            new List<LeaderboardSearchEntryDto>
            {
            new(1, "Kael", 25)
            });

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<SearchLeaderboardQuery>(q => q.Term == "Kael"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Search("Kael");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_WhenTermTooShort()
    {
        var result = await _controller.Search("K");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_WhenTermIsEmpty()
    {
        var result = await _controller.Search("");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_WhenTermIsNull()
    {
        var result = await _controller.Search(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}