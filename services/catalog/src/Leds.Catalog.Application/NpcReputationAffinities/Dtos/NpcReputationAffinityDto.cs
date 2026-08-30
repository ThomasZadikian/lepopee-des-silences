namespace Leds.Catalog.Application.NpcReputationAffinities.Dtos;

public sealed record NpcReputationAffinityDto(Guid Id, string NpcKeyFrom, string NpcKeyTo, decimal Weight);
