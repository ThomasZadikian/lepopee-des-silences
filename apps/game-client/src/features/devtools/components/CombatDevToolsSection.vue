<script setup lang="ts">
import { computed, ref } from 'vue';

import type { CombatRuntimeDto } from '../../combat/types/combatContracts';

const props = defineProps<{
  combat: CombatRuntimeDto | null;
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  killEnemies: [];
  killEnemy: [combatantId: string];
  setVitals: [combatantId: string, vitality: number, guard: number];
  applyStatus: [combatantId: string, statusKey: string, stacks: number, duration: number];
}>();

const selectedEnemyId = ref('');
const selectedCombatantId = ref('');
const vitality = ref(1);
const guard = ref(0);
const statusKey = ref('poison');
const stacks = ref(1);
const duration = ref(1);

const combatants = computed(() => props.combat ? [...props.combat.allies, ...props.combat.enemies] : []);
const enemies = computed(() => props.combat?.enemies ?? []);

function confirmKillEnemies() {
  if (window.confirm('Confirmer kill all enemies ?')) emit('killEnemies');
}

function emitSetVitals() {
  if (!selectedCombatantId.value) return;
  emit('setVitals', selectedCombatantId.value, clamp(vitality.value, 0, 999), clamp(guard.value, 0, 999));
}

function emitStatus() {
  if (!selectedCombatantId.value) return;
  emit('applyStatus', selectedCombatantId.value, statusKey.value.trim(), clamp(stacks.value, 1, 99), clamp(duration.value, 1, 99));
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, Number(value) || min));
}
</script>

<template>
  <section class="devtools-card">
    <div class="devtools-card__header">
      <h3>Combat</h3>
      <span v-if="!props.combat" class="devtools-muted">aucun combat actif</span>
    </div>

    <template v-if="props.combat">
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmKillEnemies">
        Kill all enemies
      </button>

      <div class="devtools-inline-form">
        <select v-model="selectedEnemyId" class="devtools-input">
          <option value="">Choisir un ennemi</option>
          <option v-for="enemy in enemies" :key="enemy.id" :value="enemy.id">
            {{ enemy.displayName }} - {{ enemy.currentVitality }} HP
          </option>
        </select>
        <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading || !selectedEnemyId" @click="emit('killEnemy', selectedEnemyId)">
          Kill enemy
        </button>
      </div>

      <div class="devtools-vitals-grid">
        <select v-model="selectedCombatantId" class="devtools-input">
          <option value="">Choisir un combatant</option>
          <option v-for="combatant in combatants" :key="combatant.id" :value="combatant.id">
            {{ combatant.displayName }} ({{ combatant.side }})
          </option>
        </select>
        <input v-model.number="vitality" class="devtools-input" type="number" min="0" max="999" placeholder="vitality">
        <input v-model.number="guard" class="devtools-input" type="number" min="0" max="999" placeholder="guard">
        <button class="devtools-btn" :disabled="props.disabled || props.isLoading || !selectedCombatantId" @click="emitSetVitals">
          Set vitals
        </button>
      </div>

      <div class="devtools-vitals-grid devtools-vitals-grid--status">
        <input v-model="statusKey" class="devtools-input" placeholder="statusKey">
        <input v-model.number="stacks" class="devtools-input" type="number" min="1" max="99" placeholder="stacks">
        <input v-model.number="duration" class="devtools-input" type="number" min="1" max="99" placeholder="duration">
        <button class="devtools-btn" :disabled="props.disabled || props.isLoading || !selectedCombatantId" @click="emitStatus">
          Apply status
        </button>
      </div>
      <p class="devtools-muted">Status peut etre indisponible tant que le backend n'a pas de runtime status.</p>
    </template>
  </section>
</template>
