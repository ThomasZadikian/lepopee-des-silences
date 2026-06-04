using FluentValidation;
using MediatR;

namespace RPG_ESI07.Application.Queries.Leaderboard;

public record SearchLeaderboardQuery(string Term) : IRequest<SearchLeaderboardResponse>;
public record SearchLeaderboardResponse(List<LeaderboardSearchEntryDto> Items);
public record LeaderboardSearchEntryDto(int Rank, string CharacterName, int Level);