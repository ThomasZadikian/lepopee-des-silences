using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.EquipSkill;

public sealed class EquipSkillCommandHandler : IRequestHandler<EquipSkillCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EquipSkillCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(EquipSkillCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        profile.EquipSkill(new PlayerCharacterId(request.CharacterId), request.SkillKey, _timeProvider.GetUtcNow());

        await _repository.SaveAsync(profile, cancellationToken);

        return PlayerProfileDto.FromDomain(profile);
    }
}
