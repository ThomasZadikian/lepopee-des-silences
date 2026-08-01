<script setup lang="ts">
import { computed, ref } from 'vue';
import type { ItemDefinitionView } from '../../party/types/itemTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  allItems: ItemDefinitionView[];
}>();

const emit = defineEmits<{
  addItem: [itemDefinitionKey: string, quantity: number];
}>();

const search = ref('');
const selectedItemKey = ref<string | null>(null);
const quantity = ref(1);

const filteredItems = computed(() => {
  const query = search.value.trim().toLowerCase();
  if (!query) return props.allItems;
  return props.allItems.filter((item) =>
    item.displayName.toLowerCase().includes(query) ||
    item.key.toLowerCase().includes(query) ||
    item.category.toLowerCase().includes(query));
});

const selectedItem = computed(() =>
  props.allItems.find((item) => item.key === selectedItemKey.value) ?? null);

function selectItem(item: ItemDefinitionView) {
  selectedItemKey.value = selectedItemKey.value === item.key ? null : item.key;
}

function clampQuantity(): number {
  return Math.min(99, Math.max(1, Number(quantity.value) || 1));
}

function add() {
  if (!selectedItemKey.value) return;
  emit('addItem', selectedItemKey.value, clampQuantity());
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Objets</h2>
      <p>Ajoute un objet du catalogue directement dans la besace de la run, sans passer par le butin/marchand/PNJ.</p>
    </header>

    <div class="devtools-window__body">
      <div class="devtools-catalog-layout">
        <div>
          <div class="devtools-catalog-toolbar">
            <input v-model="search" class="devtools-input" placeholder="Rechercher un objet…">
          </div>
          <p v-if="filteredItems.length === 0" class="devtools-catalog-empty">Aucun objet trouvé.</p>
          <div v-else class="devtools-catalog-grid">
            <button
              v-for="item in filteredItems"
              :key="item.key"
              type="button"
              class="devtools-catalog-cell"
              :class="{ 'devtools-catalog-cell--sel': selectedItemKey === item.key }"
              @click="selectItem(item)"
            >
              <span class="devtools-catalog-cell__name">{{ item.displayName }}</span>
              <span class="devtools-catalog-cell__meta">{{ item.rarity }} · {{ item.category }}</span>
            </button>
          </div>
        </div>

        <div class="devtools-catalog-sheet" v-if="selectedItem">
          <h3 class="devtools-catalog-sheet__name">{{ selectedItem.displayName }}</h3>
          <p class="devtools-catalog-sheet__desc">{{ selectedItem.description }}</p>
          <div class="devtools-catalog-sheet__facts">
            <span class="devtools-catalog-fact">{{ selectedItem.rarity }}</span>
            <span class="devtools-catalog-fact">{{ selectedItem.category }}</span>
            <span class="devtools-catalog-fact">{{ selectedItem.itemType }}</span>
          </div>
          <div class="devtools-inline-form">
            <input v-model.number="quantity" class="devtools-input devtools-input--small" type="number" min="1" max="99">
            <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="add">
              Ajouter à la besace
            </button>
          </div>
        </div>
        <p v-else class="devtools-catalog-empty">Sélectionne un objet pour voir son descriptif.</p>
      </div>
    </div>
  </div>
</template>
