/** Mirrors the backend's CombatTime.TicksPerTurn. */
export const TICKS_PER_TURN = 2500;

/** Ticks remaining, expressed as whole tactical turns (rounded up) for display. */
export function ticksToTurns(ticks: number): number {
  return Math.max(0, Math.ceil(ticks / TICKS_PER_TURN));
}
