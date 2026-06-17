import { computed } from 'vue';

import type { CombatLogEntryDto, CombatRuntimeDto } from '../types/combatContracts';
import type { CombatantContribution, CombatMetricsState, CombatSideMetrics } from './useCombatMetrics';

function parseLogAmount(message: string): number {
  const matches = message.match(/\d+/g);
  if (!matches?.length) return 0;
  return Number(matches[matches.length - 1]) || 0;
}

function isGuardAbsorb(entry: CombatLogEntryDto): boolean {
  return entry.type === 'DamageApplied' && entry.message.toLowerCase().includes('guard absorbs');
}

function emptyContribution(id: string, side: string): CombatantContribution {
  return {
    id, side,
    damageDealt: 0, damageTaken: 0, guardAbsorbed: 0,
    healingReceived: 0, healingDone: 0, guardGained: 0, turnsTaken: 0,
    damageToGuard: 0,
  };
}

function emptySide(): CombatSideMetrics {
  return {
    damageDealt: 0, damageTaken: 0, healingDone: 0, healingReceived: 0,
    guardAbsorbed: 0, guardGained: 0, netVitalityLoss: 0,
  };
}

export function buildMetricsFromLogs(
  entries: CombatLogEntryDto[],
  combat: CombatRuntimeDto | null,
): CombatMetricsState {
  const contributions: Record<string, CombatantContribution> = {};

  if (combat) {
    for (const c of [...combat.allies, ...combat.enemies]) {
      contributions[c.id] = emptyContribution(c.id, c.side);
    }
  }

  for (const entry of entries) {
    const amount = parseLogAmount(entry.message);

    if (entry.type === 'SkillUsed' || entry.type === 'ItemUsed') {
      if (entry.actorId && contributions[entry.actorId]) {
        contributions[entry.actorId].turnsTaken++;
      }
    } else if (entry.type === 'DamageApplied') {
      const guardAbsorb = isGuardAbsorb(entry);
      if (entry.actorId && contributions[entry.actorId]) {
        contributions[entry.actorId].damageDealt += amount;
        if (guardAbsorb) contributions[entry.actorId].damageToGuard += amount;
      }
      for (const targetId of entry.targetIds) {
        if (!contributions[targetId]) continue;
        if (guardAbsorb) {
          contributions[targetId].guardAbsorbed += amount;
        } else {
          contributions[targetId].damageTaken += amount;
        }
      }
    } else if (entry.type === 'HealApplied') {
      if (entry.actorId && contributions[entry.actorId]) {
        contributions[entry.actorId].healingDone += amount;
      }
      for (const targetId of entry.targetIds) {
        if (contributions[targetId]) contributions[targetId].healingReceived += amount;
      }
    } else if (entry.type === 'GuardGained') {
      for (const targetId of entry.targetIds) {
        if (contributions[targetId]) contributions[targetId].guardGained += amount;
      }
    }
  }

  return {
    combatId: combat?.id ?? null,
    allies: emptySide(),
    enemies: emptySide(),
    contributions,
    floatEvents: [],
  };
}

export function useCombatLogMetrics(
  getEntries: () => CombatLogEntryDto[],
  getCombat: () => CombatRuntimeDto | null,
) {
  const state = computed<CombatMetricsState>(() =>
    buildMetricsFromLogs(getEntries(), getCombat()),
  );
  return { state };
}
