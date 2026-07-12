/** Mirrors the backend's AtbConstants.TicksPerTurn — see the game-engine service. */
export const TICKS_PER_TURN = 2500;

/** Ticks remaining, expressed as whole "tours" (rounded up) for display. */
export function ticksToTurns(ticks: number): number {
  return Math.max(0, Math.ceil(ticks / TICKS_PER_TURN));
}
