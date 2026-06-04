using FluentAssertions;
using Moq;
using RPG_ESI07.Application.Queries.Leaderboard;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Tests.Application.Queries;

public class SearchLeaderboardHandlerTests
{
    private readonly Mock<ILeaderboardRepository> _mockRepo;
    private readonly SearchLeaderboardHandler _handler;

    public SearchLeaderboardHandlerTests()
    {
        _mockRepo = new Mock<ILeaderboardRepository>(MockBehavior.Strict);
        _handler = new SearchLeaderboardHandler(_mockRepo.Object);
    }

    private static PlayerProfile BuildProfile(string name, int level) =>
        new() { CharacterName = name, Level = level };

    [Fact]
    public async Task Handle_ReturnsResults_WhenMatchesExist()
    {
        var profiles = new List<PlayerProfile>
        {
            BuildProfile("Kaelindra", 25),
            BuildProfile("Kaeloth",   18),
        };
        _mockRepo.Setup(r => r.SearchByCharacterNameAsync("Kael", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(profiles);

        var result = await _handler.Handle(new SearchLeaderboardQuery("Kael"), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].CharacterName.Should().Be("Kaelindra");
        result.Items[0].Rank.Should().Be(1);
        result.Items[1].CharacterName.Should().Be("Kaeloth");
        result.Items[1].Rank.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoMatchesExist()
    {
        _mockRepo.Setup(r => r.SearchByCharacterNameAsync("xyz", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<PlayerProfile>());

        var result = await _handler.Handle(new SearchLeaderboardQuery("xyz"), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_TrimsSearchTerm_BeforeQuerying()
    {
        _mockRepo.Setup(r => r.SearchByCharacterNameAsync("Kael", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<PlayerProfile> { BuildProfile("Kaelindra", 25) });

        var result = await _handler.Handle(new SearchLeaderboardQuery("  Kael  "), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        _mockRepo.Verify(r => r.SearchByCharacterNameAsync("Kael", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectDto_WithLevelAndRank()
    {
        var profiles = new List<PlayerProfile> { BuildProfile("Elara", 30) };
        _mockRepo.Setup(r => r.SearchByCharacterNameAsync("Elara", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(profiles);

        var result = await _handler.Handle(new SearchLeaderboardQuery("Elara"), CancellationToken.None);

        var dto = result.Items.First();
        dto.Rank.Should().Be(1);
        dto.CharacterName.Should().Be("Elara");
        dto.Level.Should().Be(30);
    }
}