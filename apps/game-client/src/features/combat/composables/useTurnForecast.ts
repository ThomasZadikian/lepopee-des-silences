import { computed, type ComputedRef } from 'vue';

import type { CombatantRuntimeDto, CombatSide } from '../types/combatContracts';

const READY = 50_000;
const DEFAULT_SLOTS = 8;

export type ForecastEntry = {
  combatantId: string;
  displayName: string;
  side: CombatSide;
  /** 0 = next to act, 1 = the one after that, etc. */
  slot: number;
};

/**
 * Simulates the ATB race forward using each combatant's current gauge and
 * fill rate to predict the order combatants become ready over the next
 * `slots` turns. Purely derived from live combatant data, so it re-adapts
 * automatically whenever speed changes (buffs/debuffs) shift someone's
 * atbFillPerTick.
 */
export function useTurnForecast(
  combatants: () => CombatantRuntimeDto[],
  slots = DEFAULT_SLOTS,
): ComputedRef<ForecastEntry[]> {
  return computed(() => buildTurnForecast(combatants(), slots));
}

export function buildTurnForecast(combatants: CombatantRuntimeDto[], slots = DEFAULT_SLOTS): ForecastEntry[] {
  const actors = combatants
    .filter((c) => c.status !== 'Defeated')
    .map((c) => ({
      id: c.id,
      name: c.displayName,
      side: c.side,
      gauge: c.atbGauge ?? 0,
      rate: Math.max(1, c.atbFillPerTick ?? 10),
    }));

  if (actors.length === 0) return [];

  const forecast: ForecastEntry[] = [];

  for (let slot = 0; slot < slots; slot++) {
    let winner = actors[0];
    let minTicks = Infinity;
    for (const actor of actors) {
      const ticks = Math.max(0, READY - actor.gauge) / actor.rate;
      if (ticks < minTicks) {
        minTicks = ticks;
        winner = actor;
      }
    }

    for (const actor of actors) actor.gauge += minTicks * actor.rate;

    forecast.push({ combatantId: winner.id, displayName: winner.name, side: winner.side, slot });
    winner.gauge = 0;
  }

  return forecast;
}
