using MediatR;

namespace Leds.Player.Application.Players.SpendCurrency;

public sealed record SpendCurrencyCommand(Guid PlayerId, int Amount) : IRequest<SpendCurrencyResult>;

/// <summary>
/// Succeeded is false on insufficient funds (not an exception) — callers like "Loi de
/// l'Impôt du Seuil" need to branch on insolvency, not catch a DomainException.
/// </summary>
public sealed record SpendCurrencyResult(bool Succeeded, PlayerProfileDto Profile);
