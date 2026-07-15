using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.RecruitCompanion;

public sealed class RecruitCompanionCommandHandler : IRequestHandler<RecruitCompanionCommand, PlayerProfileDto>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecruitCompanionCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PlayerProfileDto> Handle(RecruitCompanionCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        var statBlock = PlayerCharacterStatBlock.Create(
            request.MaxVitality,
            request.AttackPower,
            request.Defense,
            request.StartingGuard,
            request.Speed,
            request.Initiative,
            request.Recovery,
            request.Focus,
            request.Mana,
            request.Charge,
            request.MagicAttack,
            request.MagicDefense);

        profile.RecruitCompanion(
            request.CompanionDefinitionKey, request.DisplayName, statBlock, request.SkillKeys, _timeProvider.GetUtcNow());

        await _repository.SaveAsync(profile, cancellationToken);

        return PlayerProfileDto.FromDomain(profile);
    }
}
