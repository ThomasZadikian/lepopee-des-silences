using Leds.Catalog.Domain.RewardCursePools;

namespace Leds.Catalog.Application.RewardCursePools.Dtos;

public sealed record RewardCursePoolDto(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Status,
    IReadOnlyCollection<RewardCurseEntryDto> Entries)
{
    public static RewardCursePoolDto FromDomain(IRewardCursePool pool) => new(
        pool.Id.Value,
        pool.Key.Value,
        pool.Name.Value,
        pool.Description.Value,
        pool.Version.Value,
        pool.Status.ToString(),
        pool.Entries.Select(RewardCurseEntryDto.FromDomain).ToArray());
}