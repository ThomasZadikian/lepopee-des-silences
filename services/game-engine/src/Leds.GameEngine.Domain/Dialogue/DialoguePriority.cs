namespace Leds.GameEngine.Domain.Dialogue;

/// <summary>
/// Ordering when more than one thing wants to speak at once — SFD Système global de dialogues
/// §9.1: scénaristique > événement critique > réaction urgente > contextuelle > ambiance. Lower
/// value wins (see <see cref="DialoguePriorityResolver"/>).
/// </summary>
public enum DialoguePriority
{
    Scripted = 0,
    CriticalEvent = 1,
    UrgentReaction = 2,
    Contextual = 3,
    Ambient = 4,
}
