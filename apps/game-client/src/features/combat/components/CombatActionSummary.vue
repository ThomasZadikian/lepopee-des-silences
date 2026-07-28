<script setup lang="ts">
import type { CombatLogEntryDto, LogEntryType } from '../types/combatContracts';

defineProps<{
  entries: CombatLogEntryDto[];
}>();

function entryClass(type: LogEntryType): string {
  const map: Partial<Record<LogEntryType, string>> = {
    ActionAccepted:   'cas__entry--action',
    SkillUsed:       'cas__entry--action',
    ItemUsed:        'cas__entry--action',
    DamageApplied:   'cas__entry--damage',
    GuardGained:     'cas__entry--guard',
    HealApplied:     'cas__entry--heal',
    TargetDefeated:  'cas__entry--defeated',
    TurnAdvanced:    'cas__entry--dim',
    EnemyTurnResolved: 'cas__entry--dim',
    CombatCompleted: 'cas__entry--completed',
    CombatFailed:    'cas__entry--failed',
    AttackMissed:    'cas__entry--missed',
    CriticalHit:     'cas__entry--critical',
    WeaknessHit:     'cas__entry--weakness',
    ResistedHit:    'cas__entry--resisted',
    ImmuneHit:       'cas__entry--immune',
    StatusApplied:  'cas__entry--status',
  };
  return map[type] ?? '';
}
</script>

<template>
  <div v-if="entries.length" class="cas">
    <div
      v-for="(entry, i) in entries"
      :key="i"
      class="cas__entry"
      :class="entryClass(entry.type)"
    >
      <span class="cas__text">{{ entry.message }}</span>
    </div>
  </div>
</template>
