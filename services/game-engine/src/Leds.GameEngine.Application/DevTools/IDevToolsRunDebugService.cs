namespace Leds.GameEngine.Application.DevTools;

public interface IDevToolsRunDebugService
{
    Task<DevToolsRunDebugResult> AdvanceRoomAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> AdvanceRoomsAsync(Guid runId, int count, CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> ForceCurrentRoomPalaceStateAsync(
        Guid runId,
        string state,
        CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> ForceCurrentRoomClimateAsync(
        Guid runId,
        string climate,
        CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> ActivatePalaceLawAsync(
        Guid runId,
        string lawKey,
        CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> ClearPalaceLawsAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> ActivateCurseAsync(
        Guid runId,
        string curseKey,
        CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> ClearCursesAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<DevToolsCombatDebugResult> KillAllCurrentCombatEnemiesAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<DevToolsCombatDebugResult> KillCurrentCombatEnemyAsync(
        Guid runId,
        Guid combatantId,
        CancellationToken cancellationToken = default);

    Task<DevToolsCombatDebugResult> SetCurrentCombatantVitalsAsync(
        Guid runId,
        Guid combatantId,
        int vitality,
        int guard,
        CancellationToken cancellationToken = default);

    /// <summary>Recruits one of the authored, recruitable companions (Thomas, Mané, Mina, Elise, John)
    /// straight onto the current run's roster, using its real catalog-authored combat kit — for
    /// testing without grinding NPC reputation. <paramref name="companionNpcKey"/> must be an NPC key
    /// that has a Companion-kind offering with a CompanionKit (e.g. "npc.thomas").</summary>
    Task<DevToolsRunDebugResult> AddDebugAllyAsync(Guid runId, string companionNpcKey, CancellationToken cancellationToken = default);

    Task<DevToolsRunDebugResult> RemoveDebugAllyAsync(Guid runId, CancellationToken cancellationToken = default);

    Task<DevToolsCombatDebugResult> ApplyCombatantStatusAsync(
        Guid runId,
        Guid combatantId,
        string statusKey,
        int stacks,
        int durationTicks,
        CancellationToken cancellationToken = default);
}
