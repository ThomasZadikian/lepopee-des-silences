using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.Application.RewardCursePools.Dtos;

public sealed record RewardCurseEntryDto(
    string Kind,
    string ResultKind,
    string? TargetKey,
    int Amount,
    IReadOnlyCollection<RewardCurseAvailabilityDto> Availability)
{
    public static RewardCurseEntryDto FromDomain(RewardCurseEntry e) => new(
        e.Kind.ToString(),
        e.ResultKind,
        e.TargetKey,
        e.Amount,
        (e.Availability ?? Array.Empty<RewardCurseAvailability>())
            .Select(RewardCurseAvailabilityDto.FromDomain).ToArray());
}