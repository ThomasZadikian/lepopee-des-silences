<script setup lang="ts">
import type { CombatLogEntryDto, LogEntryType } from '../types/combatContracts';

defineProps<{
  entries: CombatLogEntryDto[];
}>();

function entryClass(type: LogEntryType): string {
  const map: Record<LogEntryType, string> = {
    SkillUsed:         'cas__entry--action',
    ItemUsed:          'cas__entry--action',
    DamageApplied:     'cas__entry--damage',
    GuardGained:       'cas__entry--guard',
    HealApplied:       'cas__entry--heal',
    TargetDefeated:    'cas__entry--defeated',
    TurnAdvanced:      'cas__entry--dim',
    EnemyTurnResolved: 'cas__entry--dim',
    CombatCompleted:   'cas__entry--completed',
    CombatFailed:      'cas__entry--failed',
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
      <span
        v-if="entry.type === 'TargetDefeated'"
        class="cas__badge es-chip es-chip--blood"
      >KO</span>
      <span
        v-else-if="entry.type === 'SkillUsed' || entry.type === 'ItemUsed'"
        class="cas__arrow"
        aria-hidden="true"
      >⟶</span>
      <span class="cas__msg">{{ entry.message }}</span>
    </div>
  </div>
</template>

<style scoped>
.cas {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin: 0 var(--space-4) var(--space-1);
  padding: var(--space-2) var(--space-3);
  background: oklch(0.16 0.035 272 / 0.72);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
  animation: cas-in 180ms ease-out;
}

.cas__entry {
  display: flex;
  align-items: baseline;
  gap: var(--space-2);
  font-family: var(--font-mono);
  font-size: 0.72rem;
  color: var(--ink-3);
  animation: cas-entry-in 200ms ease-out;
}

.cas__arrow {
  color: var(--gold);
  font-size: 0.82rem;
  flex-shrink: 0;
  line-height: 1;
}

.cas__badge {
  font-size: 0.6rem;
  padding: 0 5px;
  flex-shrink: 0;
  align-self: center;
}

.cas__msg {
  flex: 1;
  min-width: 0;
  line-height: 1.4;
  word-break: break-word;
}

.cas__entry--action .cas__msg  { color: var(--gold); font-weight: 500; }
.cas__entry--damage .cas__msg  { color: var(--blood); }
.cas__entry--heal .cas__msg    { color: var(--sap); }
.cas__entry--guard .cas__msg   { color: var(--frost); }
.cas__entry--defeated .cas__msg { color: var(--blood); }
.cas__entry--dim .cas__msg     { color: var(--ink-5); font-size: 0.68rem; }
.cas__entry--completed .cas__msg { color: var(--gold); font-weight: 600; }
.cas__entry--failed .cas__msg  { color: var(--blood); font-weight: 600; }

@keyframes cas-in {
  from { opacity: 0; transform: translateY(3px); }
  to   { opacity: 1; transform: translateY(0); }
}

@keyframes cas-entry-in {
  from { opacity: 0; }
  to   { opacity: 1; }
}

@media (prefers-reduced-motion: reduce) {
  .cas, .cas__entry { animation: none; }
}
</style>
