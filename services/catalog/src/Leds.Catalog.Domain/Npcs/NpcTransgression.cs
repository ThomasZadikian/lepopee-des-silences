namespace Leds.Catalog.Domain.Npcs;

// L1: a transgression arms a wound when TriggerFlag is present on the relationship
// (e.g. set by an earlier choice in the SAME encounter). L2 extends this with
// historical/echo predicates ("dirty shoes two rooms ago").
public sealed record NpcTransgression(
    string WoundKey,
    string TriggerFlag,
    int RelationshipPenalty = 0);