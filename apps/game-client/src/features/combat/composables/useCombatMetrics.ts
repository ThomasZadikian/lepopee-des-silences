import { computed, ref, watch } from 'vue';

import type { CombatantRuntimeDto, CombatRuntimeDto } from '../types/combatContracts';

export type CombatMeterMetric =
  | 'damageDealt'
  | 'damageTaken'
  | 'healingDone'
  | 'healingReceived'
  | 'guardAbsorbed'
  | 'guardGained'
  | 'netVitalityLoss';

export interface CombatSideMetrics {
  damageDealt: number;
  damageTaken: number;
  healingDone: number;
  healingReceived: number;
  guardAbsorbed: number;
  guardGained: number;
  netVitalityLoss: number;
}

export interface CombatMetricsState {
  combatId: string | null;
  allies: CombatSideMetrics;
  enemies: CombatSideMetrics;
}

function emptyMetrics(): CombatSideMetrics {
  return {
    damageDealt: 0,
    damageTaken: 0,
    healingDone: 0,
    healingReceived: 0,
    guardAbsorbed: 0,
    guardGained: 0,
    netVitalityLoss: 0,
  };
}

type CombatantSnapshot = {
  id: string;
  side: string;
  currentVitality: number;
  maxVitality: number;
  currentGuard: number;
};

function snapshotCombatants(combat: CombatRuntimeDto | null): Map<string, CombatantSnapshot> {
  const map = new Map<string, CombatantSnapshot>();
  if (!combat) return map;

  for (const c of combat.allies) {
    map.set(c.id, {
      id: c.id,
      side: c.side,
      currentVitality: c.currentVitality,
      maxVitality: c.maxVitality,
      currentGuard: c.guard,
    });
  }

  for (const c of combat.enemies) {
    map.set(c.id, {
      id: c.id,
      side: c.side,
      currentVitality: c.currentVitality,
      maxVitality: c.maxVitality,
      currentGuard: c.guard,
    });
  }

  return map;
}

function computeDeltas(
  prev: Map<string, CombatantSnapshot>,
  next: CombatRuntimeDto,
): { allies: CombatSideMetrics; enemies: CombatSideMetrics } {
  const allies = emptyMetrics();
  const enemies = emptyMetrics();

  const allNext = [...next.allies, ...next.enemies];

  for (const nextC of allNext) {
    const prevC = prev.get(nextC.id);
    if (!prevC) continue;

    const target = nextC.side === 'Player' ? allies : enemies;

    const vitDelta = prevC.currentVitality - nextC.currentVitality;
    if (vitDelta > 0) {
      target.damageTaken += vitDelta;
    } else if (vitDelta < 0) {
      target.healingReceived += -vitDelta;
    }

    const guardDelta = prevC.currentGuard - nextC.currentGuard;
    if (guardDelta > 0) {
      target.guardAbsorbed += guardDelta;
    } else if (guardDelta < 0) {
      target.guardGained += -guardDelta;
    }

    target.netVitalityLoss = Math.max(0, target.damageTaken - target.healingReceived);
  }

  return { allies, enemies };
}

export function useCombatMetrics() {
  const state = ref<CombatMetricsState>({
    combatId: null,
    allies: emptyMetrics(),
    enemies: emptyMetrics(),
  });

  const previousSnapshot = ref<Map<string, CombatantSnapshot>>(new Map());

  function snapshotBeforeAction(combat: CombatRuntimeDto | null) {
    previousSnapshot.value = snapshotCombatants(combat);
  }

  function processAfterAction(combat: CombatRuntimeDto | null) {
    if (!combat) return;

    if (state.value.combatId !== combat.id) {
      state.value.combatId = combat.id;
      state.value.allies = emptyMetrics();
      state.value.enemies = emptyMetrics();
    }

    const deltas = computeDeltas(previousSnapshot.value, combat);

    state.value.allies.damageDealt += deltas.enemies.damageTaken;
    state.value.allies.damageTaken += deltas.allies.damageTaken;
    state.value.allies.healingDone += deltas.allies.healingReceived;
    state.value.allies.healingReceived += deltas.allies.healingReceived;
    state.value.allies.guardAbsorbed += deltas.allies.guardAbsorbed;
    state.value.allies.guardGained += deltas.allies.guardGained;
    state.value.allies.netVitalityLoss = Math.max(0, state.value.allies.damageTaken - state.value.allies.healingReceived);

    state.value.enemies.damageDealt += deltas.allies.damageTaken;
    state.value.enemies.damageTaken += deltas.enemies.damageTaken;
    state.value.enemies.healingDone += deltas.enemies.healingReceived;
    state.value.enemies.healingReceived += deltas.enemies.healingReceived;
    state.value.enemies.guardAbsorbed += deltas.enemies.guardAbsorbed;
    state.value.enemies.guardGained += deltas.enemies.guardGained;
    state.value.enemies.netVitalityLoss = Math.max(0, state.value.enemies.damageTaken - state.value.enemies.healingReceived);

    previousSnapshot.value = snapshotCombatants(combat);
  }

  function reset() {
    state.value.combatId = null;
    state.value.allies = emptyMetrics();
    state.value.enemies = emptyMetrics();
    previousSnapshot.value = new Map();
  }

  return {
    state,
    snapshotBeforeAction,
    processAfterAction,
    reset,
  };
}

export const metricLabels: Record<CombatMeterMetric, string> = {
  damageDealt: 'Violence rendue',
  damageTaken: 'Vitalité perdue',
  healingDone: 'Soin prodigué',
  healingReceived: 'Soin reçu',
  guardAbsorbed: 'Garde encaissée',
  guardGained: 'Garde gagnée',
  netVitalityLoss: 'Perte nette',
};
