using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.UnequipItem;

public sealed class UnequipItemCommandHandler : IRequestHandler<UnequipItemCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UnequipItemCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(UnequipItemCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        profile.UnequipItem(new PlayerCharacterId(request.CharacterId), request.ItemKey, _timeProvider.GetUtcNow());

        await _repository.SaveAsync(profile, cancellationToken);

        return PlayerProfileDto.FromDomain(profile);
    }
}
