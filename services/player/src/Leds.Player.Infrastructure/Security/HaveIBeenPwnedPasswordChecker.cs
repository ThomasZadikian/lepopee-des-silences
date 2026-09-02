using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Leds.Player.Application.Abstractions;

namespace Leds.Player.Infrastructure.Security;

public sealed class HaveIBeenPwnedPasswordChecker : ICompromisedPasswordChecker
{
    private readonly HttpClient _httpClient;

    public HaveIBeenPwnedPasswordChecker(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

#pragma warning disable S4790 // The HIBP range API contract requires the SHA-1 prefix; the password never leaves this process.
        var digest = SHA1.HashData(Encoding.UTF8.GetBytes(password));
#pragma warning restore S4790
        var fullHash = Convert.ToHexString(digest);
        var prefix = fullHash[..5];
        var suffix = fullHash[5..];

        using var request = new HttpRequestMessage(HttpMethod.Get, $"range/{prefix}");
        request.Headers.TryAddWithoutValidation("Add-Padding", "true");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        foreach (var rawLine in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = rawLine.IndexOf(':');
            if (separator <= 0)
                continue;

            if (!rawLine.AsSpan(0, separator).Equals(suffix.AsSpan(), StringComparison.OrdinalIgnoreCase))
                continue;

            var countText = rawLine[(separator + 1)..].Trim();
            return int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                && count > 0;
        }

        return false;
    }
}
