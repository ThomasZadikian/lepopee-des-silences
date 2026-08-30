namespace Leds.GameEngine.Application.Combats.EnemyTurns.Bossing;

/// <summary>
/// The action a scripted boss chooses to play on its turn: a skill key from the
/// boss's own skill set plus the target combatant ids it applies to.
/// <para>
/// The <see cref="EnemyCombatTurnResolver"/> resolves <see cref="SkillKey"/>
/// against the boss's <see cref="Domain.Combats.Combatant.Skills"/> (case-insensitive)
/// and then runs the same validate → effect → advance pipeline as a generic
/// enemy turn. If the key does not match a skill the boss owns, the resolver
/// falls back to the generic AI for that turn, so a malformed script degrades
/// gracefully instead of stalling the fight.
/// </para>
/// </summary>
/// <param name="SkillKey">A skill key owned by the boss combatant.</param>
/// <param name="TargetIds">The combatant ids the skill targets.</param>
public sealed record BossActionDecision(string SkillKey, IReadOnlyCollection<Guid> TargetIds);
