<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';

import { useCombatStore } from '../stores/combatStore';
import CombatActionBar from './CombatActionBar.vue';
import CombatantCard from './CombatantCard.vue';
import CombatLogPanel from './CombatLogPanel.vue';
import CombatTurnOrder from './CombatTurnOrder.vue';

const props = defineProps<{
  runId: string;
  combatId: string;
}>();

const emit = defineEmits<{
  combatCompleted: [];
}>();

const combatStore = useCombatStore();

const combat = computed(() => combatStore.activeCombat);

async function loadCombat() {
  await combatStore.loadCombat(props.runId, props.combatId);
}

async function submitBasicAttack() {
  await combatStore.submitBasicAttack(props.runId, props.combatId);
}

onMounted(loadCombat);

watch(
  () => props.combatId,
  async () => {
    await loadCombat();
  },
);
</script>

<template>
  <section class="combat-runtime">
    <template v-if="combat">
      <header class="combat-runtime__header">
        <div>
          <p class="system-label">Combat</p>
          <h2>Tour {{ combat.round }} · {{ combat.state }}</h2>
        </div>
        <button
          v-if="combat.state === 'Completed'"
          class="ghost-button combat-runtime__continue"
          :disabled="combatStore.isLoading"
          @click="emit('combatCompleted')"
        >
          CHOOSE_NODE
        </button>

        <span class="system-value">
          {{ combat.id }}
        </span>
      </header>

      <CombatTurnOrder :combat="combat" />

      <section class="combat-runtime__board">
        <div class="combat-runtime__side">
          <p class="system-label">PLAYER_TEAM</p>

          <CombatantCard
            v-for="combatant in combatStore.playerCombatants"
            :key="combatant.id"
            :combatant="combatant"
            :is-current-actor="combatant.id === combat.currentActorId"
          />
        </div>

        <div class="combat-runtime__side">
          <p class="system-label">ENEMY_TEAM</p>

          <CombatantCard
            v-for="combatant in combatStore.enemyCombatants"
            :key="combatant.id"
            :combatant="combatant"
            :is-current-actor="combatant.id === combat.currentActorId"
            :is-selected-target="combatant.id === combatStore.selectedTargetId"
            :selectable="!combatant.isDefeated"
            @select="combatStore.selectTarget"
          />
        </div>
      </section>

      <section class="combat-runtime__footer">
        <CombatActionBar
          :can-submit="combatStore.canSubmitBasicAttack"
          :is-loading="combatStore.isLoading"
          @basic-attack="submitBasicAttack"
        />

        <CombatLogPanel
          :result="combatStore.lastActionResult"
          :error="combatStore.error"
        />
      </section>
    </template>

    <section v-else class="combat-runtime__loading panel">
      <p class="system-label">LOADING_BATTLE</p>
      <p v-if="combatStore.error">{{ combatStore.error }}</p>
      <p v-else>ENNEMIES_LOADING</p>
    </section>
  </section>
  
</template>

<style scoped>
.combat-runtime {
  display: grid;
  grid-template-rows: auto auto minmax(0, 1fr) auto;
  gap: var(--space-4);
  height: 100%;
  min-height: 0;
}

.combat-runtime__header {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
}

.combat-runtime__header h2 {
  margin: var(--space-1) 0 0;
  color: var(--blood);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.combat-runtime__continue {
  justify-self: start;
  border-color: var(--gold);
  color: var(--gold);
}

.combat-runtime__board {
  display: grid;
  grid-template-columns: 1fr 1.2fr;
  gap: var(--space-4);
  min-height: 0;
  overflow: auto;
  padding-right: var(--space-2);
}

.combat-runtime__side {
  display: grid;
  gap: var(--space-3);
  align-content: start;
  min-width: 0;
}

.combat-runtime__footer {
  display: grid;
  grid-template-columns: minmax(20rem, 1.1fr) minmax(18rem, 1fr);
  gap: var(--space-4);
  align-items: stretch;
  min-height: 9rem;
}

.combat-runtime__loading {
  padding: var(--space-6);
}
</style>