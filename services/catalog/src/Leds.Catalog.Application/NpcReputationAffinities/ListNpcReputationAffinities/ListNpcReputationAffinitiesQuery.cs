using Leds.Catalog.Application.Abstractions.Messaging;

namespace Leds.Catalog.Application.NpcReputationAffinities.ListNpcReputationAffinities;

public sealed record ListNpcReputationAffinitiesQuery() : IQuery<ListNpcReputationAffinitiesResponse>;
