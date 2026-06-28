using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.Application.RewardCursePools.Dtos;

public sealed record RewardCurseAvailabilityDto(
    string Kind,
    int Value)
{
    public static RewardCurseAvailabilityDto FromDomain(RewardCurseAvailability a) => new(
        a.Kind.ToString(),
        a.Value);
}