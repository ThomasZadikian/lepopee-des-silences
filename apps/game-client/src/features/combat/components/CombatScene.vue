<script setup lang="ts">
import { computed, nextTick, onMounted, watch } from 'vue';

import { useRunStore } from '../../runs/stores/runStore';
import { useCombatStore } from '../stores/useCombatStore';
import { useCombatMetrics } from '../composables/useCombatMetrics';
import CombatantCard from './CombatantCard.vue';
import CombatLogPanel from './CombatLogPanel.vue';
import CombatMetersPanel from './CombatMetersPanel.vue';
import CombatOutcomePanel from './CombatOutcomePanel.vue';
import SkillBar from './SkillBar.vue';

const props = defineProps<{
  runId: string;
  combatId: string;
}>();

const emit = defineEmits<{
  combatCompleted: [];
  combatFailed: [];
  leaveRun: [];
}>();

const combatStore = useCombatStore();
const runStore = useRunStore();
const { state: metricsState, snapshotBeforeAction, processAfterAction, reset: resetMetrics } = useCombatMetrics();

const activeCombatant = computed(() => combatStore.currentActor);
const isPlayerTurn = computed(() => combatStore.isPlayerTurn);
const hasSelectedSkill = computed(() => Boolean(combatStore.selectedSkill));

function canSelect(combatantId: string): boolean {
  if (combatStore.isLoading) return false;
  if (combatStore.selectedItem?.targetingType === 'SingleAlly') {
    return combatStore.itemValidTargets.some((t) => t.id === combatantId);
  }
  if (!combatStore.selectedSkill || !isPlayerTurn.value) return false;
  return combatStore.validTargets.some((t) => t.id === combatantId);
}

function isInvalidTarget(combatantId: string): boolean {
  if (combatStore.selectedItem) return false;
  if (!hasSelectedSkill.value || combatStore.isLoading || combatStore.isSelectedTarget(combatantId)) return false;
  return !combatStore.validTargets.some((t) => t.id === combatantId);
}

function isVisuallyActive(combatantId: string): boolean {
  if (combatStore.thinkingCombatantId) return combatStore.thinkingCombatantId === combatantId;
  return combatStore.isCurrentActor(combatantId);
}

function handleSelect(combatantId: string) {
  if (combatStore.selectedItem?.targetingType === 'SingleAlly') {
    const isValid = combatStore.itemValidTargets.some((t) => t.id === combatantId);
    if (isValid) combatStore.selectedTargetIds = [combatantId];
    return;
  }
  const validIds = combatStore.validTargets.map((t) => t.id);
  if (validIds.includes(combatantId)) combatStore.selectTarget(combatantId);
}

async function handleSubmit() {
  snapshotBeforeAction(combatStore.combat);
  await combatStore.submitAction(props.runId);
  processAfterAction(combatStore.combat);
}

async function handleSubmitItem() {
  snapshotBeforeAction(combatStore.combat);
  await combatStore.submitItemAction(props.runId);
  processAfterAction(combatStore.combat);
}
function handleClearSelection() { combatStore.clearSelection(); combatStore.clearItemSelection(); }
function handleSelectItem(itemId: string) { combatStore.selectItem(itemId); }
function handleContinue() {
  resetMetrics();
  combatStore.clearCombat();
  emit('combatCompleted');
}

function getEnemyJitter(index: number): string {
  const offsets = [
    'translate(4px, -6px) rotate(-0.5deg)',
    'translate(-3px, 8px) rotate(0.3deg)',
    'translate(6px, 2px) rotate(-0.8deg)',
    'translate(-5px, -4px) rotate(0.6deg)',
    'translate(2px, 10px) rotate(-0.2deg)',
    'translate(-7px, -3px) rotate(0.4deg)',
  ];
  return offsets[index % offsets.length];
}

watch(() => combatStore.terminalEvent, (event) => {
  if (event?.kind === 'defeat') emit('combatFailed');
});

watch(() => combatStore.canSubmit, async (isReady) => {
  if (isReady && !combatStore.isResolvingAction) { await nextTick(); await handleSubmit(); }
});

watch(() => combatStore.canSubmitItem, async (isReady) => {
  const item = combatStore.selectedItem;
  if (isReady && item?.targetingType === 'Self' && !combatStore.isResolvingAction) { await nextTick(); await handleSubmitItem(); }
});

watch(() => combatStore.selectedTargetIds, async (ids) => {
  const item = combatStore.selectedItem;
  if (item && item.targetingType === 'SingleAlly' && ids.length > 0 && !combatStore.isResolvingAction) { await nextTick(); await handleSubmitItem(); }
});

onMounted(() => {
  resetMetrics();
  if (combatStore.combat?.id === props.combatId) return;
  if (runStore.combatRuntime?.id && runStore.combatRuntime.status === 'Active') {
    combatStore.initCombat(runStore.combatRuntime);
  } else {
    combatStore.loadCurrentCombat(props.runId);
  }
});

watch(() => props.combatId, (newId) => {
  if (!newId) return;
  if (combatStore.combat?.id === newId) return;
  combatStore.clearCombat();
  resetMetrics();
  if (runStore.combatRuntime?.id === newId) combatStore.initCombat(runStore.combatRuntime);
  else combatStore.loadCurrentCombat(props.runId);
});
</script>

<template>
  <section class="combat-scene">

    <!-- Loading / absent -->
    <template v-if="!combatStore.combat">
      <div class="combat-scene__placeholder">
        <p class="es-kicker">Confrontation</p>
        <h3 class="es-h2" v-if="combatStore.isLoading">Le seuil s'ouvre…</h3>
        <h3 class="es-h2" v-else>Confrontation indisponible</h3>
      </div>
    </template>

    <!-- Combat actif -->
    <template v-else>
      <!-- Top: turn indicator + meters -->
      <header class="combat-scene__header">
        <span class="es-kicker">Confrontation</span>
        <span class="es-chip es-chip--blood">Tour {{ combatStore.combat?.turnNumber ?? '?' }}</span>
      </header>

      <CombatMetersPanel :metrics="metricsState" />

      <!-- Main: face-à-face -->
      <div class="combat-scene__arena">
        <!-- Les Voix (allies) -->
        <div class="combat-scene__side combat-scene__side--voix">
          <p class="combat-scene__side-title">Les Voix</p>
          <CombatantCard
            v-for="combatant in combatStore.allies"
            :key="combatant.id"
            :combatant="combatant"
            :is-current-actor="isVisuallyActive(combatant.id)"
            :is-selected-target="combatStore.isSelectedTarget(combatant.id)"
            :is-selectable="canSelect(combatant.id)"
            :is-targetable="canSelect(combatant.id)"
            :is-invalid-target="isInvalidTarget(combatant.id)"
            :is-active-player="combatant.side === 'Player' && isPlayerTurn"
            :is-thinking="combatStore.thinkingCombatantId === combatant.id"
            :is-damaged="combatStore.recentlyDamagedIds.includes(combatant.id)"
            :is-guarded="combatStore.recentlyGuardedIds.includes(combatant.id)"
            :is-just-defeated="combatStore.recentlyDefeatedIds.includes(combatant.id)"
            :is-acting="combatStore.recentlyActingId === combatant.id"
            @select="handleSelect"
          />

          <!-- Actions inline -->
          <div class="combat-scene__inline-actions">
            <SkillBar
              :combatant="activeCombatant"
              :selected-skill-key="combatStore.selectedSkillKey"
              :is-player-turn="isPlayerTurn"
              :is-loading="combatStore.isResolvingAction"
              :usable-battle-items="combatStore.combat?.usableBattleItems ?? []"
              :selected-item-id="combatStore.selectedItemId"
              @select-skill="combatStore.selectSkill"
              @select-item="handleSelectItem"
            />
            <button
              v-if="combatStore.selectedSkillKey || combatStore.selectedItemId || combatStore.selectedTargetIds.length > 0"
              class="es-btn es-btn--ghost"
              :disabled="combatStore.isResolvingAction"
              @click="handleClearSelection"
            >
              Annuler
            </button>
          </div>
        </div>

        <!-- Seuil (central divider) -->
        <div class="combat-scene__seuil">
          <div class="seuil-line" />
          <span class="seuil-glyph">◈</span>
          <div class="seuil-line" />
        </div>

        <!-- Les Manifestations (enemies) -->
        <div class="combat-scene__side combat-scene__side--manifestations">
          <p class="combat-scene__side-title">Les Manifestations</p>
          <CombatantCard
            v-for="(combatant, idx) in combatStore.enemies"
            :key="combatant.id"
            :combatant="combatant"
            :is-current-actor="isVisuallyActive(combatant.id)"
            :is-selected-target="combatStore.isSelectedTarget(combatant.id)"
            :is-selectable="canSelect(combatant.id)"
            :is-targetable="canSelect(combatant.id)"
            :is-invalid-target="isInvalidTarget(combatant.id)"
            :is-active-player="false"
            :is-thinking="combatStore.thinkingCombatantId === combatant.id"
            :is-damaged="combatStore.recentlyDamagedIds.includes(combatant.id)"
            :is-guarded="combatStore.recentlyGuardedIds.includes(combatant.id)"
            :is-just-defeated="combatStore.recentlyDefeatedIds.includes(combatant.id)"
            :is-acting="combatStore.recentlyActingId === combatant.id"
            :style="{ '--enemy-jitter': getEnemyJitter(idx) }"
            @select="handleSelect"
          />
        </div>
      </div>

      <CombatLogPanel :entries="combatStore.logEntries" />

      <!-- Outcome overlay -->
      <CombatOutcomePanel
        v-if="combatStore.isVictory || combatStore.isDefeat"
        :is-victory="combatStore.isVictory"
        :is-loading="combatStore.isLoading"
        @continue="handleContinue"
        @leave-run="$emit('leaveRun')"
      />

      <!-- Resolving indicator -->
      <div v-if="combatStore.isResolvingAction" class="combat-scene__resolving" aria-live="polite">
        Résolution…
      </div>

      <div v-if="combatStore.error" class="combat-scene__error">
        {{ combatStore.error }}
      </div>
    </template>
  </section>
</template>

<style scoped>
.combat-scene {
  display: grid;
  grid-template-rows: auto auto 1fr auto;
  grid-template-columns: 1fr;
  gap: 0;
  height: 100%;
  min-height: 0;
  position: relative;
}

.combat-scene__header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-4);
  border-bottom: 1px solid var(--line-soft);
}

.combat-scene__arena {
  display: grid;
  grid-template-columns: 1fr auto 3fr;
  gap: 0;
  min-height: 0;
  overflow-y: auto;
  padding: var(--space-4);
}

.combat-scene__side {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-2);
  min-width: 0;
}

.combat-scene__side-title {
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin: 0 0 var(--space-2);
}

.combat-scene__side--voix {
  align-items: stretch;
}

.combat-scene__inline-actions {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding: var(--space-2) 0;
  border-top: 1px solid var(--line-soft);
  margin-top: var(--space-2);
}

.combat-scene__side--manifestations {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: var(--space-3);
  align-content: start;
}

.combat-scene__seuil {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
  padding: 0 var(--space-3);
  align-self: stretch;
}

.seuil-line {
  flex: 1;
  width: 1px;
  background: linear-gradient(to bottom, transparent, var(--line-strong), transparent);
}

.seuil-glyph {
  font-size: 1.2rem;
  color: var(--ink-4);
  opacity: 0.5;
}

.combat-scene__placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  text-align: center;
  padding: var(--space-8);
}

.combat-scene__error {
  position: absolute;
  bottom: var(--space-2);
  left: 50%;
  transform: translateX(-50%);
  color: var(--blood);
  font-size: 0.78rem;
  padding: var(--space-2) var(--space-4);
  border: 1px solid color-mix(in oklch, var(--blood), transparent 50%);
  border-radius: var(--radius-sm);
  background: oklch(0.20 0.04 13 / 0.8);
  z-index: var(--z-popover);
}

.combat-scene__resolving {
  position: absolute;
  left: 50%;
  bottom: var(--space-6);
  transform: translateX(-50%);
  padding: var(--space-2) var(--space-4);
  border: 1px solid var(--edge-gold);
  border-radius: var(--radius-sm);
  background: oklch(0.20 0.04 272 / 0.85);
  color: var(--gold);
  font-family: var(--font-caps);
  font-size: 0.68rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  z-index: var(--z-popover);
  animation: breathe 1.4s ease-in-out infinite;
}

@keyframes breathe {
  0%, 100% { opacity: 0.65; }
  50% { opacity: 1; }
}

@media (max-width: 900px) {
  .combat-scene {
  grid-template-rows: auto auto 1fr auto;
    grid-template-columns: 1fr;
  }

  .combat-scene__arena {
    grid-template-columns: 1fr;
    grid-template-rows: auto auto auto;
  }

  .combat-scene__seuil {
    flex-direction: row;
    padding: var(--space-2) 0;
  }

  .combat-scene__side--manifestations {
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  }

  .seuil-line {
    height: 1px;
    width: auto;
    flex: 1;
    background: linear-gradient(to right, transparent, var(--line-strong), transparent);
  }

  .combat-scene__side-panel {
    grid-column: 1;
    grid-row: auto;
    border-left: none;
    border-top: 1px solid var(--line-soft);
    max-height: 16rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .combat-scene__resolving { animation: none; }
}
</style>
