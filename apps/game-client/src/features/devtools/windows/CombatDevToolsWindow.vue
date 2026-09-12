<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import type {
  TacticalCombatantRuntimeDto,
  TacticalCombatRuntimeDto,
} from '../../combat/types/combatContracts';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  combat: TacticalCombatRuntimeDto | null;
}>();

const emit = defineEmits<{
  killEnemies: [];
  killEnemy: [combatantId: string];
  setVitals: [combatantId: string, vitality: number, guard: number];
  applyStatus: [combatantId: string, statusKey: string, stacks: number, duration: number];
}>();

const selectedCombatantId = ref<string | null>(null);
const vitality = ref(0);
const guard = ref(0);
const statusKey = ref('poison');
const stacks = ref(1);
const duration = ref(6);

const combatants = computed<TacticalCombatantRuntimeDto[]>(() => props.combat
  ? [...props.combat.allies, ...props.combat.enemies]
  : []);
const selected = computed(() => combatants.value.find(
  entry => entry.combatant.id === selectedCombatantId.value,
) ?? null);
const selectedIsEnemy = computed(() => selected.value?.combatant.side === 'Enemy');

watch(
  () => props.combat,
  () => {
    const currentStillExists = combatants.value.some(
      entry => entry.combatant.id === selectedCombatantId.value,
    );
    if (!currentStillExists) {
      selectedCombatantId.value = props.combat?.activeCombatantId
        ?? combatants.value[0]?.combatant.id
        ?? null;
    }
  },
  { immediate: true, deep: true },
);

watch(selected, (combatant) => {
  if (!combatant) return;
  vitality.value = combatant.combatant.currentVitality;
  guard.value = combatant.combatant.guard;
}, { immediate: true });

function applyVitals() {
  if (!selectedCombatantId.value) return;
  emit('setVitals', selectedCombatantId.value, vitality.value, guard.value);
}

function applySelectedStatus() {
  if (!selectedCombatantId.value) return;
  emit(
    'applyStatus',
    selectedCombatantId.value,
    statusKey.value,
    stacks.value,
    duration.value,
  );
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Combat</h2>
      <p>Inspecte et modifie le combat tactique actuellement ouvert dans la sandbox.</p>
    </header>

    <div v-if="!props.combat" class="devtools-window__body">
      <p>Aucun combat actif. Un lanceur de scénarios sera ajouté dans le lot suivant.</p>
    </div>

    <div v-else class="devtools-window__body">
      <p>État : {{ props.combat.status }} · round {{ props.combat.roundNumber }}</p>

      <div class="devtools-catalog-grid">
        <button
          v-for="entry in combatants"
          :key="entry.combatant.id"
          :data-testid="`combatant-${entry.combatant.id}`"
          type="button"
          class="devtools-catalog-cell"
          :class="{ 'devtools-catalog-cell--sel': selectedCombatantId === entry.combatant.id }"
          @click="selectedCombatantId = entry.combatant.id"
        >
          <span class="devtools-catalog-cell__name">{{ entry.combatant.displayName }}</span>
          <small>
            {{ entry.combatant.side === 'Player' ? 'Allié' : 'Ennemi' }} ·
            {{ entry.combatant.currentVitality }}/{{ entry.combatant.maxVitality }} PV ·
            {{ entry.combatant.guard }} garde
          </small>
        </button>
      </div>

      <template v-if="selected">
        <div class="devtools-inline-form">
          <label>
            Vitalité
            <input
              v-model.number="vitality"
              data-testid="vitality-input"
              type="number"
              min="0"
              max="999"
            >
          </label>
          <label>
            Garde
            <input
              v-model.number="guard"
              data-testid="guard-input"
              type="number"
              min="0"
              max="999"
            >
          </label>
          <button
            data-testid="set-vitals"
            type="button"
            class="devtools-btn"
            :disabled="props.disabled || props.isLoading"
            @click="applyVitals"
          >
            Appliquer
          </button>
        </div>

        <div class="devtools-inline-form">
          <label>
            État
            <select v-model="statusKey">
              <option value="poison">Poison</option>
              <option value="burn">Brûlure</option>
              <option value="regen">Régénération</option>
              <option value="atk-up">Attaque +</option>
              <option value="atk-down">Attaque −</option>
              <option value="def-up">Défense +</option>
              <option value="def-down">Défense −</option>
              <option value="stun">Étourdissement</option>
              <option value="silence">Silence</option>
              <option value="slow">Ralentissement</option>
            </select>
          </label>
          <label>
            Cumuls
            <input v-model.number="stacks" type="number" min="1" max="99">
          </label>
          <label>
            Durée
            <input v-model.number="duration" type="number" min="1" max="99">
          </label>
          <button
            data-testid="apply-status"
            type="button"
            class="devtools-btn"
            :disabled="props.disabled || props.isLoading"
            @click="applySelectedStatus"
          >
            Appliquer l’état
          </button>
        </div>

        <button
          v-if="selectedIsEnemy"
          data-testid="kill-combatant"
          type="button"
          class="devtools-btn devtools-btn--danger"
          :disabled="props.disabled || props.isLoading"
          @click="emit('killEnemy', selected.combatant.id)"
        >
          Éliminer cet ennemi
        </button>
      </template>

      <button
        type="button"
        class="devtools-btn devtools-btn--danger"
        :disabled="props.disabled || props.isLoading"
        @click="emit('killEnemies')"
      >
        Éliminer tous les ennemis
      </button>
    </div>
  </div>
</template>
