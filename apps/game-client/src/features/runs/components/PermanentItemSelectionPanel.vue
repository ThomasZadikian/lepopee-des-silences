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

// Alignée sur l'échelle de rareté déjà retenue pour la Besace : commun/peu commun restent
// neutres (aucune classe), rare/épique gagnent en intensité de mint plutôt qu'en teinte différente.
function getRarityTone(rarity: string): 'mint-dim' | 'mint' | null {
  switch (rarity) {
    case 'Rare': return 'mint-dim';
    case 'Epic': return 'mint';
    default:     return null;
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
      <p class="pis-kicker">Fin de la traversée</p>
      <h2 class="pis-title">Ce que tu emportes avec toi</h2>
      <p class="pis-lede">
        Certains objets trouvés durant cette run peuvent rejoindre ton sac permanent,
        conservé d'une traversée à l'autre. Le sac permanent n'a pas de limite de place —
        choisis librement ce qui mérite d'être gardé.
      </p>
    </header>

    <div v-if="props.candidates.length === 0" class="pis-empty">
      <p>Aucun objet éligible n'a été trouvé durant cette run.</p>
    </div>

    <div v-else class="pis-grid">
      <button
        v-for="candidate in props.candidates"
        :key="candidate.itemDefinitionKey"
        type="button"
        class="pis-card"
        :class="selectedKeys.has(candidate.itemDefinitionKey) ? 'pis-card--sel' : ''"
        @click="toggleSelection(candidate.itemDefinitionKey)"
      >
        <div class="pis-card__head">
          <h3 class="pis-card__name">{{ candidate.displayName }}</h3>
          <span
            class="pis-chip"
            :style="getRarityTone(candidate.rarity) ? { color: `var(--${getRarityTone(candidate.rarity)})`, borderColor: `var(--${getRarityTone(candidate.rarity)})` } : {}"
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
        class="es-btn es-btn--mint es-btn--lg"
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
  gap: 24px;
  height: 100%;
  padding: 48px;
  overflow-y: auto;
  color: var(--ink);
  font-family: var(--font);
}

.pis-header {
  text-align: center;
  max-width: 640px;
  margin: 0 auto;
}

.pis-kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin: 0;
}

.pis-title {
  margin: 10px 0 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 30px;
  color: var(--ink);
}

.pis-lede {
  margin-top: 12px;
  color: var(--ink-3);
  font-size: 14px;
  line-height: 1.6;
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
  border: 1px solid var(--line);
  background: var(--panel-2);
  cursor: pointer;
  text-align: left;
  transition: border-color 0.2s;
}

.pis-card--sel {
  border-color: var(--mint-dim);
  background: var(--panel);
}

.pis-card__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
}

.pis-card__name {
  margin: 0;
  font-family: var(--font-display);
  font-style: italic;
  font-size: 16px;
  color: var(--ink);
}

.pis-chip {
  flex: 0 0 auto;
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 2px 7px;
  border: 1px solid var(--line);
  color: var(--ink-4);
}

.pis-card__desc {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: var(--ink-3);
  flex: 1;
}

.pis-card__mark {
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.pis-card--sel .pis-card__mark {
  color: var(--mint-dim);
}

.pis-actions {
  display: flex;
  justify-content: center;
}
</style>
