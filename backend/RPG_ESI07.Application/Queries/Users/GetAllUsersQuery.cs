using MediatR;

namespace RPG_ESI07.Application.Queries.Users;

public record GetAllUsersQuery : IRequest<GetAllUsersResponse>;

public record GetAllUsersResponse(List<UserSummaryDto> Items);

public record UserSummaryDto(
    int Id,
    string Username,
    string Role,
    bool MfaEnabled,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);