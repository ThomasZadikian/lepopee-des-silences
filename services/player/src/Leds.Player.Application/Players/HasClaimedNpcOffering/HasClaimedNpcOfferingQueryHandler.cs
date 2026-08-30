using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.HasClaimedNpcOffering;

public sealed class HasClaimedNpcOfferingQueryHandler : IRequestHandler<HasClaimedNpcOfferingQuery, bool>
{
    private readonly IPlayerProfileRepository _repository;

    public HasClaimedNpcOfferingQueryHandler(IPlayerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(HasClaimedNpcOfferingQuery request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        return profile.HasPermanentUnlock($"{request.NpcKey}:{request.OfferingKey}");
    }
}
