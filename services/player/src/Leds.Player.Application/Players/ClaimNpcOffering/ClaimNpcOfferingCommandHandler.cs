using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.ClaimNpcOffering;

public sealed class ClaimNpcOfferingCommandHandler : IRequestHandler<ClaimNpcOfferingCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ClaimNpcOfferingCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(ClaimNpcOfferingCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        profile.GrantPermanentUnlock(
            $"{request.NpcKey}:{request.OfferingKey}", "npc-offering", request.SourceRunId, _timeProvider.GetUtcNow());

        await _repository.SaveAsync(profile, cancellationToken);

        return PlayerProfileDto.FromDomain(profile);
    }
}
