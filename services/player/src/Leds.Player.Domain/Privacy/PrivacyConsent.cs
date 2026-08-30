using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Privacy;

public sealed class PrivacyConsent
{
    private PrivacyConsent(string purposeKey, string policyVersion, DateTimeOffset grantedAtUtc)
    {
        PurposeKey = purposeKey;
        PolicyVersion = policyVersion;
        GrantedAtUtc = grantedAtUtc;
    }

    public string PurposeKey { get; }
    public string PolicyVersion { get; }
    public DateTimeOffset GrantedAtUtc { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool IsGranted => !RevokedAtUtc.HasValue;

    public static PrivacyConsent Grant(string purposeKey, string policyVersion, DateTimeOffset grantedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(purposeKey))
            throw new DomainException("Consent purpose is required.");
        if (string.IsNullOrWhiteSpace(policyVersion))
            throw new DomainException("Privacy policy version is required.");
        if (purposeKey.StartsWith("necessary.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Necessary processing cannot be represented as optional consent.");

        return new PrivacyConsent(purposeKey.Trim(), policyVersion.Trim(), grantedAtUtc);
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc.HasValue)
            return;
        if (revokedAtUtc < GrantedAtUtc)
            throw new DomainException("Consent cannot be revoked before it was granted.");

        RevokedAtUtc = revokedAtUtc;
    }
}
