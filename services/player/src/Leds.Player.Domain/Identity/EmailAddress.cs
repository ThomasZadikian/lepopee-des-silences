using System.Net.Mail;
using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public readonly record struct EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email address is required.");

        var normalized = value.Trim().ToLowerInvariant();

        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.Ordinal)
                || !normalized.Contains('@', StringComparison.Ordinal)
                || normalized.EndsWith('@'))
            {
                throw new DomainException("Email address is invalid.");
            }
        }
        catch (FormatException)
        {
            throw new DomainException("Email address is invalid.");
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;
}
