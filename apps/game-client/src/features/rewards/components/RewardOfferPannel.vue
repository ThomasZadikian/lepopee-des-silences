<script setup lang="ts">
import type { RewardOfferDto } from '../types/rewardTypes';

defineProps<{
  offer: RewardOfferDto;
  isLoading: boolean;
}>();

defineEmits<{
  selectReward: [optionId: string];
}>();

function getOptionId(option: {
  id?: string;
  rewardId?: string;
  key?: string;
}) {
  return option.id ?? option.rewardId ?? option.key ?? '';
}
</script>

<template>
  <section class="reward-offer">
    <header>
      <p class="system-label">Récompense · reward offered</p>
      <h2>{{ offer.title ?? 'Le Palais reconnaît ta traversée' }}</h2>
      <p>
        {{ offer.description ?? 'Choisis une faveur. Une seule. Le reste se referme.' }}
      </p>
    </header>

    <section class="reward-offer__options">
      <button
        v-for="option in offer.options"
        :key="getOptionId(option)"
        class="reward-offer__option"
        :disabled="isLoading || !getOptionId(option)"
        @click="$emit('selectReward', getOptionId(option))"
      >
        <span class="system-label">
          {{ option.rarity ?? 'Récompense' }}
        </span>

        <strong>
          {{ option.displayName ?? option.name ?? option.key ?? 'Récompense inconnue' }}
        </strong>

        <small>
          {{ option.rewardType ?? 'Type non renseigné' }}
        </small>

        <p>
          {{ option.description ?? 'Le Tome n’a pas encore décrit cette faveur.' }}
        </p>
      </button>
    </section>
  </section>
</template>

<style scoped>
.reward-offer {
  display: grid;
  gap: var(--space-6);
  height: 100%;
  align-content: center;
}

.reward-offer header {
  max-width: 48rem;
}

.reward-offer h2 {
  margin: var(--space-2) 0;
  color: var(--color-gold);
  font-size: clamp(2rem, 4vw, 4rem);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.reward-offer header p:last-child {
  color: var(--color-muted);
  line-height: 1.6;
}

.reward-offer__options {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-4);
}

.reward-offer__option {
  min-height: 18rem;
  display: grid;
  align-content: start;
  gap: var(--space-3);
  padding: var(--space-5);
  color: var(--color-ink);
  background: color-mix(in oklch, var(--color-panel), transparent 4%);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 25%);
  text-align: left;
  cursor: pointer;
}

.reward-offer__option:hover:not(:disabled) {
  border-color: var(--color-gold);
  box-shadow: 0 0 32px color-mix(in oklch, var(--color-gold), transparent 78%);
}

.reward-offer__option:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.reward-offer__option strong {
  color: var(--color-ink);
  font-size: 1.35rem;
}

.reward-offer__option small {
  color: var(--color-gold);
  font-family: var(--font-mono);
  text-transform: uppercase;
}

.reward-offer__option p {
  color: var(--color-muted);
  line-height: 1.55;
}
</style>