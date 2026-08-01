using MediatR;

namespace Leds.Player.Application.Players.SpendHimLitCurrency;

public sealed record SpendHimLitCurrencyCommand(Guid PlayerId, int Amount) : IRequest<SpendHimLitCurrencyResult>;

/// <summary>
/// Succeeded is false on insufficient funds (not an exception) — mirrors
/// SpendCurrencyResult.
/// </summary>
public sealed record SpendHimLitCurrencyResult(bool Succeeded, PlayerProfileDto Profile);
