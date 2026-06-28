namespace Leds.GameEngine.Domain.Npcs;

// Gradual fracture state. Ordered by severity so comparisons (>=) mean "more ruptured".
public enum WoundState
{
    Latent = 0,
    Tendu = 1,
    Rompu = 2
}