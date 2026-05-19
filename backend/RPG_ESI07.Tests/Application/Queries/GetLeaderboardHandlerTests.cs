using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RPG_ESI07.API.Controllers;
using RPG_ESI07.Application.Queries.Leaderboard;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class GetLeaderboardHandlerTests
{
    private readonly Mock<ILeaderboardRepository> _mockRepo;
    private readonly GetLeaderboardHandler _handler;

    public GetLeaderboardHandlerTests()
    {
        _mockRepo = new Mock<ILeaderboardRepository>(MockBehavior.Strict);
        _handler = new GetLeaderboardHandler(_mockRepo.Object);
    }

    private static PlayerProfile BuildProfile(string name, int level, int wins, long damage, int playtime) =>
        new()
        {
            CharacterName = name,
            Level = level,
            CombatStats = new CombatStats
            {
                CombatsWon = wins,
                CombatsLost = 0,
                TotalCombats = wins,
                TotalDamageDealt = damage,
                TotalPlaytimeMinutes = playtime
            }
        };

    [Fact]
    public async Task Handle_ReturnsRankedEntries_SortedByLevelByDefault()
    {
        var profiles = new List<PlayerProfile>
        {
            BuildProfile("Elara",  8,  35, 18500, 280),
            BuildProfile("Kael",   25, 298, 145000, 2880),
            BuildProfile("Zyx",    3,  5,  950,   45)
        };
        _mockRepo.Setup(r => r.GetLeaderboardAsync("level", 0, 50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((profiles, 3));

        var result = await _handler.Handle(new GetLeaderboardQuery("level", 1), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Items[0].Rank.Should().Be(1);
        result.Items[1].Rank.Should().Be(2);
        result.Items[2].Rank.Should().Be(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectDto_WithAllFields()
    {
        var profiles = new List<PlayerProfile>
        {
            BuildProfile("Kael", 25, 298, 145000, 2880)
        };
        _mockRepo.Setup(r => r.GetLeaderboardAsync("level", 0, 50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((profiles, 1));

        var result = await _handler.Handle(new GetLeaderboardQuery("level", 1), CancellationToken.None);

        var entry = result.Items.First();
        entry.Rank.Should().Be(1);
        entry.CharacterName.Should().Be("Kael");
        entry.Level.Should().Be(25);
        entry.CombatsWon.Should().Be(298);
        entry.TotalDamageDealt.Should().Be(145000);
        entry.TotalPlaytimeMinutes.Should().Be(2880);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoProfiles()
    {
        _mockRepo.Setup(r => r.GetLeaderboardAsync("level", 0, 50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<PlayerProfile>(), 0));

        var result = await _handler.Handle(new GetLeaderboardQuery("level", 1), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_FallsBackToLevel_WhenSortByIsInvalid()
    {
        _mockRepo.Setup(r => r.GetLeaderboardAsync("level", 0, 50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<PlayerProfile>(), 0));

        var result = await _handler.Handle(new GetLeaderboardQuery("invalidfield", 1), CancellationToken.None);

        result.Should().NotBeNull();
        _mockRepo.Verify(r => r.GetLeaderboardAsync("level", 0, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Page2_CorrectSkipAndRank()
    {
        var profiles = new List<PlayerProfile>
        {
            BuildProfile("Joueur51", 10, 20, 5000, 100)
        };
        _mockRepo.Setup(r => r.GetLeaderboardAsync("level", 50, 50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((profiles, 51));

        var result = await _handler.Handle(new GetLeaderboardQuery("level", 2), CancellationToken.None);

        result.Items.First().Rank.Should().Be(51);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task Handle_LimitsTo100Items_EvenIfMoreExist()
    {
        _mockRepo.Setup(r => r.GetLeaderboardAsync("level", 0, 50, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((new List<PlayerProfile>(), 500));

        var result = await _handler.Handle(new GetLeaderboardQuery("level", 1), CancellationToken.None);

        result.TotalCount.Should().Be(100);
        result.TotalPages.Should().Be(2);
    }
}