import { ref } from 'vue';

import type { CombatRuntimeDto } from '../types/combatContracts';

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

export type CombatFloatEventType = 'damage' | 'heal' | 'guard';

export interface CombatantContribution {
  id: string;
  side: string;
  damageDealt: number;
  damageTaken: number;
  guardAbsorbed: number;
  healingReceived: number;
  healingDone: number;
  guardGained: number;
  turnsTaken: number;
}

export interface CombatFloatEvent {
  id: string;
  combatantId: string;
  type: CombatFloatEventType;
  amount: number;
}

export interface CombatMetricsState {
  combatId: string | null;
  allies: CombatSideMetrics;
  enemies: CombatSideMetrics;
  contributions: Record<string, CombatantContribution>;
  floatEvents: CombatFloatEvent[];
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

function createContribution(snapshot: CombatantSnapshot): CombatantContribution {
  return {
    id: snapshot.id,
    side: snapshot.side,
    damageDealt: 0,
    damageTaken: 0,
    guardAbsorbed: 0,
    healingReceived: 0,
    healingDone: 0,
    guardGained: 0,
    turnsTaken: 0,
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
): {
  allies: CombatSideMetrics;
  enemies: CombatSideMetrics;
  contributions: CombatantContribution[];
  floatEvents: CombatFloatEvent[];
} {
  const allies = emptyMetrics();
  const enemies = emptyMetrics();
  const contributions: CombatantContribution[] = [];
  const floatEvents: CombatFloatEvent[] = [];

  const allNext = [...next.allies, ...next.enemies];

  for (const nextC of allNext) {
    const prevC = prev.get(nextC.id);
    if (!prevC) continue;

    const target = nextC.side === 'Player' ? allies : enemies;
    const contribution = createContribution(prevC);

    const vitDelta = prevC.currentVitality - nextC.currentVitality;
    if (vitDelta > 0) {
      target.damageTaken += vitDelta;
      contribution.damageTaken += vitDelta;
      floatEvents.push({
        id: `${nextC.id}-damage-${Date.now()}-${floatEvents.length}`,
        combatantId: nextC.id,
        type: 'damage',
        amount: vitDelta,
      });
    } else if (vitDelta < 0) {
      target.healingReceived += -vitDelta;
      contribution.healingReceived += -vitDelta;
      floatEvents.push({
        id: `${nextC.id}-heal-${Date.now()}-${floatEvents.length}`,
        combatantId: nextC.id,
        type: 'heal',
        amount: -vitDelta,
      });
    }

    const guardDelta = prevC.currentGuard - nextC.guard;
    if (guardDelta > 0) {
      target.guardAbsorbed += guardDelta;
      contribution.guardAbsorbed += guardDelta;
      floatEvents.push({
        id: `${nextC.id}-block-${Date.now()}-${floatEvents.length}`,
        combatantId: nextC.id,
        type: 'guard',
        amount: guardDelta,
      });
    } else if (guardDelta < 0) {
      target.guardGained += -guardDelta;
      contribution.guardGained += -guardDelta;
      floatEvents.push({
        id: `${nextC.id}-guard-${Date.now()}-${floatEvents.length}`,
        combatantId: nextC.id,
        type: 'guard',
        amount: -guardDelta,
      });
    }

    target.netVitalityLoss = Math.max(0, target.damageTaken - target.healingReceived);
    if (contribution.damageTaken > 0 || contribution.healingReceived > 0 || contribution.guardGained > 0 || contribution.guardAbsorbed > 0) {
      contributions.push(contribution);
    }
  }

  return { allies, enemies, contributions, floatEvents };
}

export function useCombatMetrics() {
  const state = ref<CombatMetricsState>({
    combatId: null,
    allies: emptyMetrics(),
    enemies: emptyMetrics(),
    contributions: {},
    floatEvents: [],
  });

  const previousSnapshot = ref<Map<string, CombatantSnapshot>>(new Map());
  const previousActorId = ref<string | null>(null);

  function snapshotBeforeAction(combat: CombatRuntimeDto | null) {
    previousSnapshot.value = snapshotCombatants(combat);
    previousActorId.value = combat?.activeCombatantId ?? null;
  }

  function processAfterAction(combat: CombatRuntimeDto | null) {
    if (!combat) return;

    if (state.value.combatId !== combat.id) {
      state.value.combatId = combat.id;
      state.value.allies = emptyMetrics();
      state.value.enemies = emptyMetrics();
      state.value.contributions = {};
      state.value.floatEvents = [];
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

    for (const contribution of deltas.contributions) {
      const current = state.value.contributions[contribution.id] ?? {
        id: contribution.id,
        side: contribution.side,
        damageDealt: 0,
        damageTaken: 0,
        guardAbsorbed: 0,
        healingReceived: 0,
        healingDone: 0,
        guardGained: 0,
        turnsTaken: 0,
      };

      state.value.contributions[contribution.id] = {
        ...current,
        damageDealt: current.damageDealt + contribution.damageDealt,
        damageTaken: current.damageTaken + contribution.damageTaken,
        guardAbsorbed: current.guardAbsorbed + contribution.guardAbsorbed,
        healingReceived: current.healingReceived + contribution.healingReceived,
        healingDone: current.healingDone + contribution.healingDone,
        guardGained: current.guardGained + contribution.guardGained,
        turnsTaken: current.turnsTaken + contribution.turnsTaken,
      };
    }

    if (previousActorId.value) {
      const actor = [...combat.allies, ...combat.enemies].find((c) => c.id === previousActorId.value);
      if (actor) {
        const current = state.value.contributions[actor.id] ?? {
          id: actor.id,
          side: actor.side,
          damageDealt: 0,
          damageTaken: 0,
          guardAbsorbed: 0,
          healingReceived: 0,
          healingDone: 0,
          guardGained: 0,
          turnsTaken: 0,
        };

        const actorDamageDealt = actor.side === 'Player' ? deltas.enemies.damageTaken : deltas.allies.damageTaken;
        const actorHealingDone = actor.side === 'Player' ? deltas.allies.healingReceived : deltas.enemies.healingReceived;

        state.value.contributions[actor.id] = {
          ...current,
          damageDealt: current.damageDealt + actorDamageDealt,
          healingDone: current.healingDone + actorHealingDone,
          turnsTaken: current.turnsTaken + 1,
        };
      }
    }

    state.value.floatEvents = deltas.floatEvents;

    previousSnapshot.value = snapshotCombatants(combat);
  }

  function reset() {
    state.value.combatId = null;
    state.value.allies = emptyMetrics();
    state.value.enemies = emptyMetrics();
    state.value.contributions = {};
    state.value.floatEvents = [];
    previousSnapshot.value = new Map();
    previousActorId.value = null;
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
