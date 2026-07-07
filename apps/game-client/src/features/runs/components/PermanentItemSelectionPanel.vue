<script setup lang="ts">
import { ref } from 'vue';
import type { PermanentItemCandidateDto } from '../types/runTypes';

const props = defineProps<{
  candidates: PermanentItemCandidateDto[];
  isLoading: boolean;
}>();

const emit = defineEmits<{
  confirm: [itemDefinitionKeys: string[]];
}>();

const selectedKeys = ref<Set<string>>(new Set());

function toggleSelection(itemDefinitionKey: string) {
  if (selectedKeys.value.has(itemDefinitionKey)) {
    selectedKeys.value.delete(itemDefinitionKey);
  } else {
    selectedKeys.value.add(itemDefinitionKey);
  }
  // Force reactivity — Set mutations aren't tracked in-place by Vue's proxy.
  selectedKeys.value = new Set(selectedKeys.value);
}

function confirmSelection() {
  emit('confirm', Array.from(selectedKeys.value));
}

function getRarityTone(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'sap';
    case 'Rare':     return 'frost';
    case 'Epic':     return 'gold';
    default:         return '';
  }
}

function getRarityLabel(rarity: string): string {
  switch (rarity) {
    case 'Uncommon': return 'Peu commun';
    case 'Rare':     return 'Rare';
    case 'Epic':     return 'Épique';
    default:         return 'Commun';
  }
}
</script>

<template>
  <section class="pis-screen">
    <header class="pis-header">
      <p class="es-kicker">Fin de la traversée</p>
      <h2 class="es-h2">Ce que tu emportes avec toi</h2>
      <p class="es-lede es-dim">
        Certains objets trouvés durant cette run peuvent rejoindre ton sac permanent,
        conservé d'une traversée à l'autre. Le sac permanent n'a pas de limite de place —
        choisis librement ce qui mérite d'être gardé.
      </p>
    </header>

    <div v-if="props.candidates.length === 0" class="pis-empty">
      <p class="es-body">Aucun objet éligible n'a été trouvé durant cette run.</p>
    </div>

    <div v-else class="pis-grid">
      <button
        v-for="candidate in props.candidates"
        :key="candidate.itemDefinitionKey"
        type="button"
        class="pis-card"
        :class="[
          selectedKeys.has(candidate.itemDefinitionKey) ? 'pis-card--sel' : '',
          getRarityTone(candidate.rarity) ? `pis-card--${getRarityTone(candidate.rarity)}` : '',
        ]"
        @click="toggleSelection(candidate.itemDefinitionKey)"
      >
        <div class="pis-card__head">
          <h3 class="pis-card__name">{{ candidate.displayName }}</h3>
          <span
            class="pis-chip"
            :class="getRarityTone(candidate.rarity) ? `pis-chip--${getRarityTone(candidate.rarity)}` : ''"
          >
            {{ getRarityLabel(candidate.rarity) }}
          </span>
        </div>
        <p class="pis-card__desc">{{ candidate.description }}</p>
        <div class="pis-card__mark">{{ selectedKeys.has(candidate.itemDefinitionKey) ? '✦ Conservé' : 'Toucher pour conserver' }}</div>
      </button>
    </div>

    <footer class="pis-actions">
      <button
        class="es-btn es-btn--gold es-btn--lg"
        :disabled="isLoading"
        @click="confirmSelection"
      >
        {{ isLoading ? 'Confirmation…' : `Confirmer (${selectedKeys.size} objet${selectedKeys.size > 1 ? 's' : ''})` }}
      </button>
    </footer>
  </section>
</template>

<style scoped>
.pis-screen {
  display: flex;
  flex-direction: column;
  gap: var(--space-6, 24px);
  height: 100%;
  padding: var(--space-8, 48px);
  overflow-y: auto;
}

.pis-header {
  text-align: center;
  max-width: 640px;
  margin: 0 auto;
}

.pis-header .es-lede {
  margin-top: 12px;
}

.pis-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--ink-4);
}

.pis-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 260px));
  gap: 16px;
  justify-content: center;
  flex: 1;
  align-content: start;
}

.pis-card {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 18px 18px 16px;
  border-radius: 6px;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  background: linear-gradient(180deg, var(--panel-2, oklch(0.28 0.03 60 / 0.6)), var(--panel, oklch(0.24 0.025 60)));
  cursor: pointer;
  text-align: left;
  transition: border-color 0.2s, transform 0.2s, box-shadow 0.2s;
}

.pis-card:hover {
  transform: translateY(-3px);
}

.pis-card--sel {
  border-color: var(--frost);
  box-shadow: 0 0 24px -10px var(--frost);
}

.pis-card--sel.pis-card--gold {
  border-color: var(--gold);
  box-shadow: 0 0 24px -10px var(--gold);
}

.pis-card__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
}

.pis-card__name {
  margin: 0;
  font-family: var(--font-display, var(--font));
  font-size: 16px;
  font-weight: 600;
  color: var(--ink, oklch(.88 .015 70));
}

.pis-chip {
  flex: 0 0 auto;
  font-family: var(--font-caps, var(--font));
  font-size: 9px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 2px 7px;
  border-radius: 3px;
  border: 1px solid var(--line, oklch(.35 .025 60 / .6));
  color: var(--ink-4, oklch(.45 .015 275));
}

.pis-chip--sap   { border-color: oklch(.70 .09 162 / .45); color: var(--sap, oklch(.70 .09 162)); }
.pis-chip--frost { border-color: oklch(.70 .07 232 / .45); color: var(--frost, oklch(.70 .07 232)); }
.pis-chip--gold  { border-color: oklch(.72 .1 85 / .45); color: var(--gold, oklch(.72 .1 85)); }

.pis-card__desc {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--ink-3, oklch(.65 .02 275));
  flex: 1;
}

.pis-card__mark {
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4, oklch(.45 .015 275));
}

.pis-card--sel .pis-card__mark {
  color: var(--frost, oklch(.70 .07 232));
}

.pis-actions {
  display: flex;
  justify-content: center;
}
</style>
