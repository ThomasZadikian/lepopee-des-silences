using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Privacy;

public sealed class AccountClosureRequest
{
    private AccountClosureRequest(Guid accountId, DateTimeOffset requestedAtUtc, DateTimeOffset executeAfterUtc)
    {
        AccountId = accountId;
        RequestedAtUtc = requestedAtUtc;
        ExecuteAfterUtc = executeAfterUtc;
    }

    public Guid AccountId { get; }
    public DateTimeOffset RequestedAtUtc { get; }
    public DateTimeOffset ExecuteAfterUtc { get; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public bool IsCancelled => CancelledAtUtc.HasValue;

    public static AccountClosureRequest Request(Guid accountId, DateTimeOffset requestedAtUtc, TimeSpan gracePeriod)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("Account id is required for closure.");
        if (gracePeriod <= TimeSpan.Zero)
            throw new DomainException("Account closure grace period must be positive.");

        return new AccountClosureRequest(accountId, requestedAtUtc, requestedAtUtc.Add(gracePeriod));
    }

    public bool CanExecute(DateTimeOffset now) => !IsCancelled && now >= ExecuteAfterUtc;

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        if (IsCancelled)
            return;
        if (cancelledAtUtc >= ExecuteAfterUtc)
            throw new DomainException("Account closure can only be cancelled during the grace period.");

        CancelledAtUtc = cancelledAtUtc;
    }
}
