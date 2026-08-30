using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players;

public sealed class GetPlayerProfileByIdQueryHandler
    : IRequestHandler<GetPlayerProfileByIdQuery, PlayerProfileDto?>
{
    private readonly IPlayerProfileRepository _repository;

    public GetPlayerProfileByIdQueryHandler(IPlayerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlayerProfileDto?> Handle(
        GetPlayerProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(
            new PlayerId(request.PlayerId), cancellationToken);

        return profile is null ? null : PlayerProfileDto.FromDomain(profile);
    }
}
