using Leds.Player.Application.Abstractions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.CreatePlayerProfile;

public sealed class CreatePlayerProfileCommandHandler
    : IRequestHandler<CreatePlayerProfileCommand, CreatePlayerProfileResponse>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreatePlayerProfileCommandHandler(
        IPlayerProfileRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<CreatePlayerProfileResponse> Handle(
        CreatePlayerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = PlayerProfile.Create(request.DisplayName, _timeProvider.GetUtcNow());

        await _repository.SaveAsync(profile, cancellationToken);

        return new CreatePlayerProfileResponse(PlayerProfileDto.FromDomain(profile));
    }
}
