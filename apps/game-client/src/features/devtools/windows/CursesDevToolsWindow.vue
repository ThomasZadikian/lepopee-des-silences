<script setup lang="ts">
import { computed, ref } from 'vue';
import type { CurseDefinitionView } from '../../palace-laws/types/curseTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  allCurses: CurseDefinitionView[];
}>();

const emit = defineEmits<{
  activateCurse: [curseKey: string];
  clearCurses: [];
}>();

const search = ref('');
const selectedCurseKey = ref<string | null>(null);

const filteredCurses = computed(() => {
  const query = search.value.trim().toLowerCase();
  if (!query) return props.allCurses;
  return props.allCurses.filter((curse) =>
    curse.displayName.toLowerCase().includes(query) ||
    curse.key.toLowerCase().includes(query));
});

const selectedCurse = computed(() =>
  props.allCurses.find((curse) => curse.key === selectedCurseKey.value) ?? null);

function selectCurse(curse: CurseDefinitionView) {
  selectedCurseKey.value = selectedCurseKey.value === curse.key ? null : curse.key;
}

function activate() {
  if (!selectedCurseKey.value) return;
  emit('activateCurse', selectedCurseKey.value);
}

function confirmAndClearCurses() {
  if (window.confirm('Confirmer clear curses ?')) emit('clearCurses');
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Malédictions</h2>
      <p>Active une malédiction du catalogue sur la run, ou efface toutes les malédictions actives.</p>
    </header>

    <div class="devtools-window__body">
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmAndClearCurses">
        Effacer toutes les malédictions
      </button>

      <div class="devtools-catalog-layout">
        <div>
          <div class="devtools-catalog-toolbar">
            <input v-model="search" class="devtools-input" placeholder="Rechercher une malédiction…">
          </div>
          <p v-if="filteredCurses.length === 0" class="devtools-catalog-empty">Aucune malédiction trouvée.</p>
          <div v-else class="devtools-catalog-grid">
            <button
              v-for="curse in filteredCurses"
              :key="curse.key"
              type="button"
              class="devtools-catalog-cell"
              :class="{ 'devtools-catalog-cell--sel': selectedCurseKey === curse.key }"
              @click="selectCurse(curse)"
            >
              <span class="devtools-catalog-cell__name">{{ curse.displayName }}</span>
              <span class="devtools-catalog-cell__meta">Sévérité {{ curse.severity }} · {{ curse.duration }}</span>
            </button>
          </div>
        </div>

        <div class="devtools-catalog-sheet" v-if="selectedCurse">
          <h3 class="devtools-catalog-sheet__name">{{ selectedCurse.displayName }}</h3>
          <p class="devtools-catalog-sheet__desc">{{ selectedCurse.description }}</p>
          <p v-if="selectedCurse.narrativeText" class="devtools-catalog-sheet__desc">{{ selectedCurse.narrativeText }}</p>
          <div class="devtools-catalog-sheet__facts">
            <span class="devtools-catalog-fact">Sévérité {{ selectedCurse.severity }}</span>
            <span class="devtools-catalog-fact">{{ selectedCurse.duration }}</span>
            <span v-if="selectedCurse.trigger" class="devtools-catalog-fact">{{ selectedCurse.trigger }}</span>
          </div>
          <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="activate">
            Activer cette malédiction
          </button>
        </div>
        <p v-else class="devtools-catalog-empty">Sélectionne une malédiction pour voir son descriptif.</p>
      </div>
    </div>
  </div>
</template>
