namespace Leds.Catalog.Domain.Npcs;

// Gradual fracture state (decision Q22a). Aggregated NPC state = max severity across wounds.
public enum WoundState
{
    Latent = 0,
    Tendu = 1,
    Rompu = 2
}