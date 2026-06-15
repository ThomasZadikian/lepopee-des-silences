<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import type {
  RewardChoiceDto,
  RewardOfferDto,
  RewardOptionDto,
} from '../types/rewardTypes';

type DisplayRewardChoice = {
  id: string;
  label: string;
  rewardType: string;
  description: string;
  rarity?: string;
};

const props = defineProps<{
  offer: RewardOfferDto;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  selectReward: [choiceId: string];
}>();

const selectedId = ref<string | null>(null);

const rewardChoices = computed<DisplayRewardChoice[]>(() => {
  if (props.offer.choices?.length) return props.offer.choices.map(mapChoice);
  if (props.offer.options?.length) return props.offer.options.map(mapOption);
  return [];
});

watch(() => props.offer.id, () => { selectedId.value = null; });

function mapChoice(c: RewardChoiceDto): DisplayRewardChoice {
  return { id: c.id, label: c.label, rewardType: c.rewardType, description: c.description, rarity: c.rarity };
}

function mapOption(o: RewardOptionDto): DisplayRewardChoice {
  return {
    id: o.id ?? o.rewardId ?? o.key ?? '',
    label: o.displayName ?? o.name ?? o.label ?? o.key ?? 'Faveur',
    rewardType: o.rewardType ?? o.type ?? '—',
    description: o.description ?? '',
    rarity: o.rarity,
  };
}

function select(choiceId: string) {
  if (!choiceId || props.isLoading || selectedId.value) return;
  selectedId.value = choiceId;
  emit('selectReward', choiceId);
}

function rarityChip(rarity?: string): string {
  if (!rarity) return '';
  const r = rarity.toLowerCase();
  if (r === 'rare' || r === 'mémoire') return 'es-chip--frost';
  if (r === 'epic' || r === 'relique') return 'es-chip--gold';
  if (r === 'uncommon') return 'es-chip--sap';
  return '';
}
</script>

<template>
  <section class="reward-scene">
    <div class="es-atmos">
      <div class="es-vignette" />
      <div class="es-grain" />
    </div>

    <div class="reward-scene__card">
      <header class="reward-scene__header">
        <span class="es-kicker">Récompense</span>
        <h2 class="es-h2">{{ offer.title ?? 'Le Palais reconnaît ta traversée' }}</h2>
        <p class="es-lede es-dim">{{ offer.description ?? 'Choisis ce que tu emportes.' }}</p>
      </header>

      <hr class="es-rule" />

      <div v-if="rewardChoices.length > 0" class="reward-scene__favors">
        <span class="es-kicker">Une faveur, une seule</span>

        <div class="reward-scene__cards">
          <button
            v-for="choice in rewardChoices"
            :key="choice.id"
            class="reward-card"
            :class="{
              'reward-card--selected': selectedId === choice.id,
              'reward-card--disabled': selectedId !== null && selectedId !== choice.id,
            }"
            :disabled="isLoading || !choice.id || selectedId !== null"
            @click="select(choice.id)"
          >
            <template v-if="selectedId === choice.id">
              <div class="es-corner es-corner--gold tl" />
              <div class="es-corner es-corner--gold tr" />
              <div class="es-corner es-corner--gold bl" />
              <div class="es-corner es-corner--gold br" />
            </template>

            <div class="reward-card__pick">{{ selectedId === choice.id ? '❦' : '' }}</div>

            <span class="es-chip" :class="rarityChip(choice.rarity)">{{ choice.rarity ?? 'Faveur' }}</span>

            <div class="reward-card__slot">◈</div>

            <h3 class="es-h3 reward-card__name">{{ choice.label }}</h3>
            <span class="es-label">{{ choice.rewardType }}</span>

            <hr class="es-rule" />

            <p class="es-body reward-card__desc">{{ choice.description }}</p>

            <span v-if="selectedId === choice.id" class="reward-card__status">Offerte</span>
          </button>
        </div>
      </div>

      <div v-else class="reward-scene__empty">
        <p class="es-body">Aucune faveur disponible.</p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.reward-scene {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: var(--space-4);
}

.reward-scene__card {
  position: relative;
  z-index: 1;
  width: min(880px, 94vw);
  background: oklch(0.20 0.04 272 / 0.85);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  box-shadow:
    oklch(1 0 0 / 0.03) 0 0 0 1px inset,
    oklch(0.12 0.03 272 / 0.7) 0 0 30px -8px inset,
    var(--wash-gold) 0 0 40px -20px;
  backdrop-filter: blur(12px);
  padding: var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  max-height: 90vh;
  overflow-y: auto;
}

.reward-scene__header {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
}

.reward-scene__header h2 { margin: 0; color: var(--ink); }

.reward-scene__favors {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
}

.reward-scene__cards {
  display: flex;
  gap: var(--space-4);
  justify-content: center;
  flex-wrap: wrap;
}

.reward-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
  width: 260px;
  padding: var(--space-4);
  background: var(--card-soft);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  cursor: pointer;
  font-family: inherit;
  color: var(--ink);
  text-align: center;
  transition: border-color 0.2s ease, opacity 0.3s ease;
}

.reward-card:hover:not(:disabled) {
  border-color: var(--edge-gold);
}

.reward-card--selected {
  border-color: var(--gold) !important;
  box-shadow: 0 0 20px oklch(0.862 0.098 86 / 0.2);
}

.reward-card--disabled {
  opacity: 0.3;
  cursor: default;
}

.reward-card:disabled { cursor: default; }

.reward-card__pick {
  height: 1.2rem;
  color: var(--gold);
  font-size: 1rem;
}

.reward-card__slot {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  font-size: 1.5rem;
  color: var(--ink-4);
  background: var(--panel);
  border: 1px solid var(--line-soft);
  border-radius: 4px;
}

.reward-card__name { margin: 0; color: var(--ink); }

.reward-card__desc {
  margin: 0;
  font-size: 0.82rem;
  color: var(--ink-3);
  line-height: 1.5;
}

.reward-card__status {
  color: var(--gold);
  font-family: var(--font-caps);
  font-size: 0.56rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  margin-top: var(--space-1);
}

.reward-scene__empty {
  text-align: center;
  padding: var(--space-6);
}
</style>
