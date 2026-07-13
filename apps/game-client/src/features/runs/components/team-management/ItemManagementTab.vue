<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { ItemDefinitionView } from '../../../party/types/itemTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { itemsApi } from '../../../party/api/itemsApi';

const props = defineProps<{ character: PlayerCharacterView }>();

const playerStore = usePlayerStore();

const allItems = ref<ItemDefinitionView[]>([]);

onMounted(async () => {
  try {
    const response = await itemsApi.listActive();
    allItems.value = response.items;
  } catch {
    // Best-effort: fall back to raw keys below if the catalog lookup fails.
  }
});

function itemDisplayName(itemKey: string): string {
  return allItems.value.find((i) => i.key === itemKey)?.displayName ?? itemKey;
}

const equippedItems = computed(() => props.character.items.filter((i) => i.isEquipped));
// Scoped to the character being managed here — NOT playerStore's protagonist-only getter,
// otherwise a companion's loadout stayed stuck at "full" (or "empty") based on the
// protagonist's own equipped count instead of its own.
const isItemLoadoutFull = computed(() => equippedItems.value.length >= props.character.maxEquippedItems);

function isEquippedOnCharacter(itemKey: string): boolean {
  return props.character.items.some((i) => i.itemKey === itemKey && i.isEquipped);
}

function toggleItem(itemKey: string, isEquipped: boolean) {
  if (playerStore.isLoading) return;
  if (isEquipped) {
    playerStore.unequipItem(props.character.id, itemKey);
  } else {
    if (isItemLoadoutFull.value) return;
    playerStore.equipItem(props.character.id, itemKey);
  }
}
</script>

<template>
  <div class="imk-root">
    <p v-if="playerStore.error" class="imk-error">{{ playerStore.error }}</p>

    <!-- ── Section 1: currently equipped ── -->
    <section class="imk-section">
      <h4 class="imk-section__title">
        Objets équipés
        <span class="imk-section__count">{{ equippedItems.length }} / {{ character.maxEquippedItems }}</span>
      </h4>
      <ul v-if="equippedItems.length" class="imk-list">
        <li v-for="item in equippedItems" :key="item.itemKey" class="imk-row">
          <div class="imk-row__info">
            <span class="imk-row__name">{{ itemDisplayName(item.itemKey) }}</span>
          </div>
          <button
            type="button"
            class="imk-toggle imk-toggle--active"
            :disabled="playerStore.isLoading"
            @click="toggleItem(item.itemKey, true)"
          >
            Déséquiper
          </button>
        </li>
      </ul>
      <p v-else class="imk-empty">Aucun objet équipé.</p>
    </section>

    <!-- ── Section 2: permanent backpack (all owned items) ── -->
    <section class="imk-section">
      <h4 class="imk-section__title">Sac permanent</h4>
      <ul v-if="playerStore.permanentItems.length" class="imk-list">
        <li
          v-for="permanentItem in playerStore.permanentItems"
          :key="permanentItem.itemDefinitionKey"
          class="imk-row"
        >
          <div class="imk-row__info">
            <span class="imk-row__name">{{ itemDisplayName(permanentItem.itemDefinitionKey) }}</span>
          </div>
          <button
            type="button"
            class="imk-toggle"
            :class="{ 'imk-toggle--active': isEquippedOnCharacter(permanentItem.itemDefinitionKey) }"
            :disabled="
              playerStore.isLoading ||
              (!isEquippedOnCharacter(permanentItem.itemDefinitionKey) && isItemLoadoutFull)
            "
            @click="toggleItem(permanentItem.itemDefinitionKey, isEquippedOnCharacter(permanentItem.itemDefinitionKey))"
          >
            {{ isEquippedOnCharacter(permanentItem.itemDefinitionKey) ? 'Équipé' : 'Équiper' }}
          </button>
        </li>
      </ul>
      <p v-else class="imk-empty">Le sac permanent est vide pour l'instant.</p>
    </section>
  </div>
</template>

<style scoped>
.imk-root {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.imk-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.imk-section__title {
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  padding-bottom: 6px;
  border-bottom: 1px solid var(--line-soft);
  margin: 0;
}

.imk-section__count {
  float: right;
  font-family: var(--font-mono, monospace);
  color: var(--gold);
}

.imk-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.imk-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 4px;
  background: oklch(0.24 0.015 283 / 0.35);
}

.imk-row__info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.imk-row__name {
  font-size: 13px;
  color: var(--ink-2);
}

.imk-toggle {
  flex-shrink: 0;
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 4px 10px;
  border-radius: 3px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-4);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.imk-toggle:disabled {
  opacity: 0.38;
  cursor: not-allowed;
}

.imk-toggle:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.imk-toggle--active {
  border-color: var(--gold);
  color: var(--gold);
  background: oklch(0.55 0.08 85 / 0.12);
}

.imk-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
  margin: 0;
}

.imk-error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood);
  margin: 0;
}
</style>
