using Leds.Player.Application.Players;
using MediatR;

namespace Leds.Player.Application.Accounts;

public sealed record GetAccountOverviewQuery(Guid AccountId) : IRequest<AccountOverviewResponse>;

public sealed record AccountOverviewResponse(
    Guid AccountId,
    string DisplayName,
    string Email,
    string Role,
    bool EmailVerified,
    bool MfaConfigured,
    IReadOnlyCollection<PlayerCharacterDto> Characters,
    MainStoryProgressDto MainStory);

public sealed record ListAccountSessionsQuery(
    Guid AccountId,
    Guid CurrentSessionId) : IRequest<IReadOnlyCollection<AccountSessionResponse>>;

public sealed record AccountSessionResponse(
    Guid SessionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RotatedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsCurrent,
    bool IsActive);

public sealed record RevokeAccountSessionCommand(
    Guid AccountId,
    Guid SessionId) : IRequest;

public sealed record CreateAccountCharacterCommand(
    Guid AccountId,
    string DisplayName,
    string ArchetypeKey) : IRequest<PlayerProfileDto>;

public sealed record ArchiveAccountCharacterCommand(
    Guid AccountId,
    Guid CharacterId) : IRequest<PlayerProfileDto>;

public sealed record ClaimGameSessionCommand(
    Guid AccountId,
    Guid SessionId,
    bool ConfirmTransfer) : IRequest<GameSessionLeaseResponse>;

public sealed record HeartbeatGameSessionCommand(
    Guid AccountId,
    Guid SessionId) : IRequest<GameSessionLeaseResponse>;

public sealed record ReleaseGameSessionCommand(
    Guid AccountId,
    Guid SessionId) : IRequest;

public sealed record GameSessionLeaseResponse(
    string Status,
    Guid OwnerSessionId,
    DateTimeOffset ExpiresAtUtc);
