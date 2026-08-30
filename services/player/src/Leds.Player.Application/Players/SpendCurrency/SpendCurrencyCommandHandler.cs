using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.SpendCurrency;

public sealed class SpendCurrencyCommandHandler : IRequestHandler<SpendCurrencyCommand, SpendCurrencyResult>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SpendCurrencyCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<SpendCurrencyResult> Handle(SpendCurrencyCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        var succeeded = profile.TrySpendCurrency(_timeProvider.GetUtcNow(), request.Amount);

        if (succeeded)
            await _repository.SaveAsync(profile, cancellationToken);

        return new SpendCurrencyResult(succeeded, PlayerProfileDto.FromDomain(profile));
    }
}
