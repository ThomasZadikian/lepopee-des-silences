namespace Leds.Player.Application.Abstractions;

public interface ICompromisedPasswordChecker
{
    Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken);
}

internal sealed class AcceptAllPasswordChecker : ICompromisedPasswordChecker
{
    public Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken) => Task.FromResult(false);
}
