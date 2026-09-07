using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Application.Players;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Accounts;

public sealed class GetAccountOverviewQueryHandler
    : IRequestHandler<GetAccountOverviewQuery, AccountOverviewResponse>
{
    private readonly IAccountStore _accountStore;
    private readonly IPlayerProfileRepository _profiles;

    public GetAccountOverviewQueryHandler(
        IAccountStore accountStore,
        IPlayerProfileRepository profiles)
    {
        _accountStore = accountStore;
        _profiles = profiles;
    }

    public async Task<AccountOverviewResponse> Handle(
        GetAccountOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var identity = await _accountStore.FindIdentityByAccountIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account identity", request.AccountId);
        var profile = await _profiles.GetByIdAsync(new PlayerId(request.AccountId), cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        var characters = profile.Roster.Characters
            .Where(character => !character.IsArchived && character.ArchetypeKey is not null)
            .Select(PlayerCharacterDto.FromDomain)
            .ToArray();

        return new AccountOverviewResponse(
            request.AccountId,
            profile.DisplayName,
            identity.Email.Value,
            identity.Role.ToString(),
            identity.IsEmailVerified,
            identity.IsMfaConfigured,
            characters,
            MainStoryProgressDto.FromDomain(profile.MainStoryProgress));
    }
}

public sealed class ListAccountSessionsQueryHandler
    : IRequestHandler<ListAccountSessionsQuery, IReadOnlyCollection<AccountSessionResponse>>
{
    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;

    public ListAccountSessionsQueryHandler(IAccountStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyCollection<AccountSessionResponse>> Handle(
        ListAccountSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var sessions = await _store.ListSessionsAsync(request.AccountId, cancellationToken);
        return sessions
            .Select(session => new AccountSessionResponse(
                session.SessionId,
                session.CreatedAtUtc,
                session.ExpiresAtUtc,
                session.RotatedAtUtc,
                session.RevokedAtUtc,
                session.SessionId == request.CurrentSessionId,
                !session.IsRevoked && !session.IsExpired(now)))
            .ToArray();
    }
}

public sealed class RevokeAccountSessionCommandHandler : IRequestHandler<RevokeAccountSessionCommand>
{
    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;

    public RevokeAccountSessionCommandHandler(IAccountStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task Handle(RevokeAccountSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _store.FindSessionAsync(request.SessionId, cancellationToken);
        if (session is null || session.AccountId != request.AccountId)
            return;

        if (!session.IsRevoked)
        {
            session.Revoke(_timeProvider.GetUtcNow());
            await _store.SaveSessionAsync(session, cancellationToken);
        }

        await _store.ReleaseGameLeaseAsync(request.AccountId, request.SessionId, cancellationToken);
    }
}

public sealed class CreateAccountCharacterCommandHandler
    : IRequestHandler<CreateAccountCharacterCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _profiles;
    private readonly TimeProvider _timeProvider;
    private readonly IArchetypeDefinitionGateway _archetypes;

    public CreateAccountCharacterCommandHandler(
        IPlayerProfileRepository profiles,
        TimeProvider timeProvider,
        IArchetypeDefinitionGateway archetypes)
    {
        _profiles = profiles;
        _timeProvider = timeProvider;
        _archetypes = archetypes;
    }

    public async Task<PlayerProfileDto> Handle(
        CreateAccountCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByIdAsync(new PlayerId(request.AccountId), cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        var archetype = await _archetypes.GetByKeyAsync(request.ArchetypeKey, cancellationToken)
            ?? throw new NotFoundException("Archetype", request.ArchetypeKey);
        profile.CreatePlayableCharacter(request.DisplayName, archetype, _timeProvider.GetUtcNow());
        await _profiles.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}

public sealed class ArchiveAccountCharacterCommandHandler
    : IRequestHandler<ArchiveAccountCharacterCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _profiles;
    private readonly TimeProvider _timeProvider;

    public ArchiveAccountCharacterCommandHandler(
        IPlayerProfileRepository profiles,
        TimeProvider timeProvider)
    {
        _profiles = profiles;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(
        ArchiveAccountCharacterCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByIdAsync(new PlayerId(request.AccountId), cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);
        var character = profile.Roster.GetRequired(new PlayerCharacterId(request.CharacterId));
        if (character.ArchetypeKey is null)
            throw new ConflictException("Only player-created characters can be archived from the account.");

        var now = _timeProvider.GetUtcNow();
        character.Archive(now);
        profile.Touch(now);
        await _profiles.SaveAsync(profile, cancellationToken);
        return PlayerProfileDto.FromDomain(profile);
    }
}

public sealed class ClaimGameSessionCommandHandler
    : IRequestHandler<ClaimGameSessionCommand, GameSessionLeaseResponse>
{
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;

    public ClaimGameSessionCommandHandler(IAccountStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task<GameSessionLeaseResponse> Handle(
        ClaimGameSessionCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        await EnsureActiveOwnedSessionAsync(_store, request.AccountId, request.SessionId, now, cancellationToken);

        var claim = await _store.ClaimGameLeaseAsync(
            request.AccountId,
            request.SessionId,
            now,
            LeaseDuration,
            request.ConfirmTransfer,
            cancellationToken);

        var status = "unavailable";
        if (claim.TransferRequired)
            status = "transfer-required";
        else if (claim.Lease.OwnerSessionId == request.SessionId)
            status = "active";

        return new GameSessionLeaseResponse(
            status,
            claim.Lease.OwnerSessionId,
            claim.Lease.ExpiresAtUtc);
    }

    internal static async Task EnsureActiveOwnedSessionAsync(
        IAccountStore store,
        Guid accountId,
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var session = await store.FindSessionAsync(sessionId, cancellationToken);
        if (session is null
            || session.AccountId != accountId
            || session.IsRevoked
            || session.IsExpired(now))
        {
            throw new UnauthorizedException("The authenticated session is no longer active.");
        }
    }
}

public sealed class HeartbeatGameSessionCommandHandler
    : IRequestHandler<HeartbeatGameSessionCommand, GameSessionLeaseResponse>
{
    private readonly IAccountStore _store;
    private readonly TimeProvider _timeProvider;

    public HeartbeatGameSessionCommandHandler(IAccountStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public async Task<GameSessionLeaseResponse> Handle(
        HeartbeatGameSessionCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        await ClaimGameSessionCommandHandler.EnsureActiveOwnedSessionAsync(
            _store,
            request.AccountId,
            request.SessionId,
            now,
            cancellationToken);

        var renewed = await _store.HeartbeatGameLeaseAsync(
            request.AccountId,
            request.SessionId,
            now,
            ClaimGameSessionCommandHandler.LeaseDuration,
            cancellationToken);
        if (!renewed)
            throw new ConflictException("The active game-session lease is no longer owned by this session.");

        var lease = await _store.GetGameLeaseAsync(request.AccountId, cancellationToken)
            ?? throw new ConflictException("The active game-session lease no longer exists.");
        return new GameSessionLeaseResponse("active", lease.OwnerSessionId, lease.ExpiresAtUtc);
    }
}

public sealed class ReleaseGameSessionCommandHandler : IRequestHandler<ReleaseGameSessionCommand>
{
    private readonly IAccountStore _store;

    public ReleaseGameSessionCommandHandler(IAccountStore store)
    {
        _store = store;
    }

    public Task Handle(ReleaseGameSessionCommand request, CancellationToken cancellationToken) =>
        _store.ReleaseGameLeaseAsync(request.AccountId, request.SessionId, cancellationToken);
}
