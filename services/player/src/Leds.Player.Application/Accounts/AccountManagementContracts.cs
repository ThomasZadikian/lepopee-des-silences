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

public sealed record UpdateAccountProfileCommand(Guid AccountId, string DisplayName)
    : IRequest<AccountOverviewResponse>;

public sealed record ChangeAccountEmailCommand(Guid AccountId, string NewEmail)
    : IRequest<AccountEmailChangeResponse>;

public sealed record AccountEmailChangeResponse(string Email, bool VerificationRequired);

public sealed record ChangeAccountPasswordCommand(
    Guid AccountId,
    string CurrentPassword,
    string NewPassword) : IRequest;

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

public sealed record GetPrivacyStateQuery(Guid AccountId) : IRequest<AccountPrivacyResponse>;

public sealed record PrivacyConsentResponse(
    string PurposeKey,
    string PolicyVersion,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsGranted);

public sealed record AccountClosureResponse(
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset ExecuteAfterUtc,
    DateTimeOffset? CancelledAtUtc,
    bool IsCancelled);

public sealed record AccountPrivacyResponse(
    IReadOnlyCollection<PrivacyConsentResponse> Consents,
    AccountClosureResponse? Closure);

public sealed record SetPrivacyConsentCommand(
    Guid AccountId,
    string PurposeKey,
    string PolicyVersion,
    bool Granted) : IRequest<PrivacyConsentResponse>;

public sealed record RequestAccountClosureCommand(Guid AccountId) : IRequest<AccountClosureResponse>;
public sealed record CancelAccountClosureCommand(Guid AccountId) : IRequest<AccountClosureResponse>;

public sealed record GetAccountDataExportQuery(Guid AccountId) : IRequest<AccountDataExportResponse>;

public sealed record AccountExportIdentity(
    Guid AccountId,
    string DisplayName,
    string Email,
    string Role,
    DateTimeOffset AccountCreatedAtUtc,
    DateTimeOffset AccountUpdatedAtUtc,
    DateTimeOffset IdentityCreatedAtUtc,
    bool EmailVerified,
    bool MfaConfigured);

public sealed record AccountDataExportResponse(
    string Format,
    DateTimeOffset GeneratedAtUtc,
    AccountExportIdentity Identity,
    IReadOnlyCollection<PlayerCharacterDto> Characters,
    MainStoryProgressDto MainStory,
    IReadOnlyCollection<PrivacyConsentResponse> Consents,
    AccountClosureResponse? Closure,
    IReadOnlyCollection<AccountSessionResponse> Sessions);

/// <summary>Internal maintenance command. It anonymises every closure request whose grace period elapsed.</summary>
public sealed record ExecuteDueAccountClosuresCommand : IRequest<int>;
