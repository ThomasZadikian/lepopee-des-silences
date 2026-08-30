using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Identity;

public static class MinimumAgePolicy
{
    public const int MinimumAge = 16;

    public static bool EnsureEligible(bool confirmsAtLeastSixteen)
    {
        if (!confirmsAtLeastSixteen)
            throw new DomainException($"Players must be at least {MinimumAge} years old.");

        return true;
    }
}
