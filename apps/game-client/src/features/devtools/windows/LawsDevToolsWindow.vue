<script setup lang="ts">
import { computed, ref } from 'vue';
import type { PalaceLawDefinitionView } from '../../palace-laws/types/lawTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  allLaws: PalaceLawDefinitionView[];
}>();

const emit = defineEmits<{
  activateLaw: [lawKey: string];
  clearLaws: [];
}>();

const search = ref('');
const selectedLawKey = ref<string | null>(null);

const filteredLaws = computed(() => {
  const query = search.value.trim().toLowerCase();
  if (!query) return props.allLaws;
  return props.allLaws.filter((law) =>
    law.name.toLowerCase().includes(query) ||
    law.key.toLowerCase().includes(query) ||
    law.polarity.toLowerCase().includes(query));
});

const selectedLaw = computed(() =>
  props.allLaws.find((law) => law.key === selectedLawKey.value) ?? null);

function selectLaw(law: PalaceLawDefinitionView) {
  selectedLawKey.value = selectedLawKey.value === law.key ? null : law.key;
}

function activate() {
  if (!selectedLawKey.value) return;
  emit('activateLaw', selectedLawKey.value);
}

function confirmAndClearLaws() {
  if (window.confirm('Confirmer clear laws ?')) emit('clearLaws');
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Lois du Palais</h2>
      <p>Active une loi du catalogue sur la run, ou efface toutes les lois actives.</p>
    </header>

    <div class="devtools-window__body">
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmAndClearLaws">
        Effacer toutes les lois
      </button>

      <div class="devtools-catalog-layout">
        <div>
          <div class="devtools-catalog-toolbar">
            <input v-model="search" class="devtools-input" placeholder="Rechercher une loi…">
          </div>
          <p v-if="filteredLaws.length === 0" class="devtools-catalog-empty">Aucune loi trouvée.</p>
          <div v-else class="devtools-catalog-grid">
            <button
              v-for="law in filteredLaws"
              :key="law.key"
              type="button"
              class="devtools-catalog-cell"
              :class="{ 'devtools-catalog-cell--sel': selectedLawKey === law.key }"
              @click="selectLaw(law)"
            >
              <span class="devtools-catalog-cell__name">{{ law.name }}</span>
              <span class="devtools-catalog-cell__meta">{{ law.rarity }} · {{ law.polarity }}</span>
            </button>
          </div>
        </div>

        <div class="devtools-catalog-sheet" v-if="selectedLaw">
          <h3 class="devtools-catalog-sheet__name">{{ selectedLaw.name }}</h3>
          <p class="devtools-catalog-sheet__desc">{{ selectedLaw.description }}</p>
          <div class="devtools-catalog-sheet__facts">
            <span class="devtools-catalog-fact">{{ selectedLaw.rarity }}</span>
            <span class="devtools-catalog-fact">{{ selectedLaw.polarity }}</span>
            <span v-if="selectedLaw.isMajeure" class="devtools-catalog-fact">Majeure</span>
            <span v-for="domain in selectedLaw.impactDomains" :key="domain" class="devtools-catalog-fact">{{ domain }}</span>
          </div>
          <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="activate">
            Activer cette loi
          </button>
        </div>
        <p v-else class="devtools-catalog-empty">Sélectionne une loi pour voir son descriptif.</p>
      </div>
    </div>
  </div>
</template>
