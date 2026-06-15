<script setup lang="ts">
import { ref } from 'vue';
import { useRunStore } from '../../runs/stores/runStore';
import type { RunItemDto } from '../../runs/types/runTypes';
import { inventoryApi } from '../api/inventoryApi';

const props = defineProps<{
  items: RunItemDto[];
  runId: string;
}>();

const emit = defineEmits<{
  close: [];
}>();

const runStore = useRunStore();
const selectedItem = ref<RunItemDto | null>(null);
const isLoading = ref(false);
const error = ref<string | null>(null);

function getRarityChip(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'es-chip--sap';
    case 'Rare': return 'es-chip--frost';
    case 'Epic': return 'es-chip--gold';
    default: return '';
  }
}

function getEffectLabel(effectType: string, effectAmount: number): string {
  switch (effectType) {
    case 'Heal': return `+${effectAmount} Vitalité`;
    case 'Guard': return `+${effectAmount} Garde`;
    case 'ManaRestore': return `+${effectAmount} Mana`;
    case 'ChargeRestore': return `+${effectAmount} Charge`;
    case 'NextCombatGuard': return `+${effectAmount} Garde (prochain combat)`;
    case 'NarrativeFragment': return 'Fragment narratif';
    default: return '';
  }
}

function selectItem(item: RunItemDto) {
  selectedItem.value = item;
  error.value = null;
}

async function useItem() {
  if (!selectedItem.value) return;
  isLoading.value = true;
  error.value = null;
  try {
    await inventoryApi.useItem(props.runId, selectedItem.value.id);
    await runStore.loadRun(props.runId);
    selectedItem.value = null;
  } catch (err) {
    error.value = err instanceof Error ? err.message : "L'utilisation a échoué.";
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <aside class="besace-drawer">
    <header class="besace-drawer__header">
      <div>
        <p class="es-kicker">La Besace</p>
        <h3 class="es-h3">Objets de run</h3>
      </div>
      <button class="es-btn es-btn--ghost" @click="emit('close')">✕</button>
    </header>

    <hr class="es-rule" />

    <div v-if="items.length === 0" class="besace-drawer__empty">
      <p class="es-body">Ton sac est vide. Les objets que tu trouveras apparaîtront ici.</p>
    </div>

    <div v-else class="besace-drawer__grid">
      <button
        v-for="item in items"
        :key="item.id"
        class="besace-cell"
        :class="{ 'besace-cell--selected': selectedItem?.id === item.id }"
        @click="selectItem(item)"
      >
        <span class="besace-cell__name">{{ item.displayName }}</span>
        <span v-if="item.quantity > 1" class="besace-cell__qty">×{{ item.quantity }}</span>
        <span class="es-chip" :class="getRarityChip(item.rarity)">{{ item.rarity }}</span>
      </button>
    </div>

    <!-- Living sheet for selected item -->
    <Transition name="slide-up">
      <div v-if="selectedItem" class="besace-sheet">
        <div class="besace-sheet__header">
          <h4 class="es-h3">{{ selectedItem.displayName }}</h4>
          <span v-if="selectedItem.quantity > 1" class="es-chip">×{{ selectedItem.quantity }}</span>
        </div>

        <p class="es-body">{{ selectedItem.description }}</p>

        <div v-if="selectedItem.effectAmount > 0" class="besace-sheet__effect">
          <span class="es-label">Effet</span>
          <span class="besace-sheet__effect-value">{{ getEffectLabel(selectedItem.effectType, selectedItem.effectAmount) }}</span>
        </div>

        <div class="besace-sheet__meta">
          <span class="es-chip">{{ selectedItem.type }}</span>
          <span class="es-chip" :class="getRarityChip(selectedItem.rarity)">{{ selectedItem.rarity }}</span>
        </div>

        <div class="besace-sheet__actions">
          <button
            v-if="selectedItem.isUsable"
            class="es-btn es-btn--gold es-btn--lg"
            :disabled="isLoading"
            @click="useItem"
          >
            {{ isLoading ? 'Utilisation…' : 'Utiliser' }}
          </button>
          <p v-else class="es-label">Cet objet ne peut pas être utilisé actuellement.</p>
        </div>

        <p v-if="error" class="besace-sheet__error">{{ error }}</p>
      </div>
    </Transition>
  </aside>
</template>

<style scoped>
.besace-drawer {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 40px;
  width: 300px;
  display: flex;
  flex-direction: column;
  padding: var(--space-4);
  background: oklch(0.22 0.04 272 / 0.92);
  border-left: 1px solid var(--line-soft);
  backdrop-filter: blur(8px);
  overflow-y: auto;
  z-index: var(--z-drawer);
}

.besace-drawer__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-2);
}

.besace-drawer__header h3 {
  margin: var(--space-1) 0 0;
}

.besace-drawer__empty {
  padding: var(--space-4) 0;
}

.besace-drawer__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
  gap: var(--space-2);
  padding: var(--space-2) 0;
}

.besace-cell {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  padding: var(--space-2) var(--space-3);
  background: var(--card-soft);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
  cursor: pointer;
  text-align: left;
  transition: border-color 0.2s ease;
}

.besace-cell:hover {
  border-color: var(--line-strong);
}

.besace-cell--selected {
  border-color: var(--edge-frost);
  background: var(--card-sel-frost);
}

.besace-cell__name {
  font-family: var(--font);
  font-size: 0.82rem;
  color: var(--ink-2);
}

.besace-cell__qty {
  font-family: var(--font-mono);
  font-size: 0.72rem;
  color: var(--ink-4);
}

.besace-sheet {
  margin-top: var(--space-3);
  padding: var(--space-4);
  background: var(--card-soft);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.besace-sheet__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.besace-sheet__header h4 {
  margin: 0;
}

.besace-sheet__effect {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.besace-sheet__effect-value {
  color: var(--sap);
  font-family: var(--font);
  font-size: 0.9rem;
}

.besace-sheet__meta {
  display: flex;
  gap: var(--space-2);
}

.besace-sheet__actions {
  margin-top: var(--space-2);
}

.besace-sheet__actions .es-btn {
  width: 100%;
  justify-content: center;
}

.besace-sheet__error {
  color: var(--blood);
  font-size: 0.82rem;
  margin: 0;
}
</style>
