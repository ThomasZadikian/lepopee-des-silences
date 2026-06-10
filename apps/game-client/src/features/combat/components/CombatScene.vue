<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';

import { useCombatStore } from '../stores/useCombatStore';
import CombatantCard from './CombatantCard.vue';
import CombatantSidePanel from './CombatantSidePanel.vue';
import CombatLogPanel from './CombatLogPanel.vue';
import CombatOutcomePanel from './CombatOutcomePanel.vue';
import SkillBar from './SkillBar.vue';

const props = defineProps<{
  runId: string;
  combatId: string;
}>();

const emit = defineEmits<{
  combatCompleted: [];
  combatFailed: [];
}>();

const combatStore = useCombatStore();

const activeCombatant = computed(() => combatStore.currentActor);

const selectedCombatant = computed(() => {
  const targetId = combatStore.selectedTargetIds[0];
  if (!targetId) return null;
  return combatStore.findCombatantById(targetId);
});

const isPlayerTurn = computed(() => combatStore.isPlayerTurn);

function canSelect(combatantId: string): boolean {
  if (!combatStore.selectedSkill || !isPlayerTurn.value) return false;
  return combatStore.validTargets.some((t) => t.id === combatantId);
}

function handleSelect(combatantId: string) {
  const validIds = combatStore.validTargets.map((t) => t.id);
  if (validIds.includes(combatantId)) {
    combatStore.selectTarget(combatantId);
  }
}

async function handleSubmit() {
  await combatStore.submitAction(props.runId, props.combatId);
}

function handleClearSelection() {
  combatStore.clearSelection();
}

function handleContinue() {
  emit('combatCompleted');
}

watch(
  () => combatStore.terminalEvent,
  (event) => {
    if (event?.kind === 'victory') {
      emit('combatCompleted');
    } else if (event?.kind === 'defeat') {
      emit('combatFailed');
    }
  },
);

onMounted(() => {
  if (!combatStore.combat) {
    combatStore.loadCurrentCombat(props.runId);
  }
});

watch(
  () => props.combatId,
  (newId) => {
    if (newId) {
      combatStore.loadCurrentCombat(props.runId);
    }
  },
);
</script>

<template>
  <section class="combat-scene">
    <header class="combat-scene__header">
      <div>
        <p class="system-label">COMBAT</p>
        <h2>Tour {{ combatStore.combat?.turnNumber ?? '?' }}</h2>
      </div>

      <span class="system-value combat-scene__combat-id">
        {{ combatId }}
      </span>
    </header>

    <section class="combat-scene__board">
      <div class="combat-scene__side combat-scene__side--allies">
        <p class="system-label combat-scene__side-label">ALLIÉS</p>

        <CombatantCard
          v-for="combatant in combatStore.allies"
          :key="combatant.id"
          :combatant="combatant"
          :is-current-actor="combatStore.isCurrentActor(combatant.id)"
          :is-selected-target="combatStore.isSelectedTarget(combatant.id)"
          :is-selectable="combatant.side === 'Player' && isPlayerTurn && canSelect(combatant.id)"
          :is-active-player="combatant.side === 'Player' && isPlayerTurn"
          @select="handleSelect"
        />
      </div>

      <div class="combat-scene__side combat-scene__side--enemies">
        <p class="system-label combat-scene__side-label">ENNEMIS</p>

        <CombatantCard
          v-for="combatant in combatStore.enemies"
          :key="combatant.id"
          :combatant="combatant"
          :is-current-actor="combatStore.isCurrentActor(combatant.id)"
          :is-selected-target="combatStore.isSelectedTarget(combatant.id)"
          :is-selectable="canSelect(combatant.id)"
          :is-active-player="false"
          @select="handleSelect"
        />
      </div>
    </section>

    <section class="combat-scene__footer">
      <SkillBar
        :combatant="activeCombatant"
        :selected-skill-key="combatStore.selectedSkillKey"
        :is-player-turn="isPlayerTurn"
        :is-loading="combatStore.isLoading"
        @select-skill="combatStore.selectSkill"
      />

      <CombatLogPanel :entries="combatStore.logEntries" />

      <div v-if="combatStore.error" class="combat-scene__error">
        {{ combatStore.error }}
      </div>
    </section>

    <section class="combat-scene__action-bar">
      <button
        class="ghost-button"
        :disabled="!combatStore.canSubmit || combatStore.isLoading"
        @click="handleSubmit"
      >
        {{ combatStore.isLoading ? 'EXÉCUTION…' : 'EXÉCUTER L\'ACTION' }}
      </button>

      <button
        class="ghost-button"
        :disabled="(!combatStore.selectedSkillKey && combatStore.selectedTargetIds.length === 0) || combatStore.isLoading"
        @click="handleClearSelection"
      >
        ANNULER
      </button>
    </section>

    <CombatantSidePanel
      class="combat-scene__side-panel"
      :combatant="selectedCombatant ?? activeCombatant"
    />

    <CombatOutcomePanel
      v-if="combatStore.isVictory || combatStore.isDefeat"
      :is-victory="combatStore.isVictory"
      :is-loading="combatStore.isLoading"
      @continue="handleContinue"
    />
  </section>
</template>

<style scoped>
.combat-scene {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto auto;
  grid-template-columns: 1fr 16rem;
  gap: var(--space-4);
  height: 100%;
  min-height: 0;
  position: relative;
}

.combat-scene__header {
  grid-column: 1 / -1;
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
}

.combat-scene__header h2 {
  margin: var(--space-1) 0 0;
  color: var(--color-blood);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.combat-scene__combat-id {
  color: var(--color-dim);
}

.combat-scene__board {
  grid-column: 1 / -1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
  min-height: 0;
  overflow: auto;
}

.combat-scene__side {
  display: grid;
  gap: var(--space-3);
  align-content: start;
  min-width: 0;
}

.combat-scene__side-label {
  color: var(--color-dim);
}

.combat-scene__footer {
  grid-column: 1 / -1;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-4);
  align-items: stretch;
  min-height: 0;
}

.combat-scene__action-bar {
  grid-column: 1 / -1;
  display: flex;
  gap: var(--space-3);
}

.combat-scene__error {
  color: var(--color-blood);
  font-size: 0.75rem;
  padding: var(--space-2);
  border: 1px solid var(--color-blood);
  border-radius: var(--radius-sm);
  background: color-mix(in oklch, var(--color-blood), transparent 92%);
}

.combat-scene__side-panel {
  position: absolute;
  right: 0;
  top: 0;
  width: 16rem;
  max-height: 100%;
  overflow-y: auto;
  z-index: 5;
}
</style>
