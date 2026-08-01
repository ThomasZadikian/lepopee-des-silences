using Leds.Player.Application.Abstractions;
using Leds.Player.Application.Common.Exceptions;
using Leds.Player.Domain.Players;
using MediatR;

namespace Leds.Player.Application.Players.SpendHimLitCurrency;

public sealed class SpendHimLitCurrencyCommandHandler : IRequestHandler<SpendHimLitCurrencyCommand, SpendHimLitCurrencyResult>
{
    private readonly IPlayerProfileRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SpendHimLitCurrencyCommandHandler(IPlayerProfileRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<SpendHimLitCurrencyResult> Handle(SpendHimLitCurrencyCommand request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(new PlayerId(request.PlayerId), cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerId);

        var succeeded = profile.TrySpendHimLitCurrency(_timeProvider.GetUtcNow(), request.Amount);

        if (succeeded)
            await _repository.SaveAsync(profile, cancellationToken);

        return new SpendHimLitCurrencyResult(succeeded, PlayerProfileDto.FromDomain(profile));
    }
}
