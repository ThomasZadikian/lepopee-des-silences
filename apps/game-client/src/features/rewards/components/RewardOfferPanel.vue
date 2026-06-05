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

const selectedChoiceId = ref<string | null>(
  props.offer.selectedChoiceId ?? props.offer.selectedOptionId ?? null,
);

const rewardChoices = computed<DisplayRewardChoice[]>(() => {
  if (props.offer.choices && props.offer.choices.length > 0) {
    return props.offer.choices.map(mapRewardChoice);
  }

  if (props.offer.options && props.offer.options.length > 0) {
    return props.offer.options.map(mapRewardOption);
  }

  return [];
});

watch(
  () => props.offer.id,
  () => {
    selectedChoiceId.value =
      props.offer.selectedChoiceId ?? props.offer.selectedOptionId ?? null;
  },
);

function mapRewardChoice(choice: RewardChoiceDto): DisplayRewardChoice {
  return {
    id: choice.id,
    label: choice.label,
    rewardType: choice.rewardType,
    description: choice.description,
    rarity: choice.rarity,
  };
}

function mapRewardOption(option: RewardOptionDto): DisplayRewardChoice {
  return {
    id: option.id ?? option.rewardId ?? option.key ?? '',
    label:
      option.displayName
      ?? option.name
      ?? option.label
      ?? option.key
      ?? 'PLACEHOLDER_REWARD_NAME',
    rewardType: option.rewardType ?? option.type ?? 'PLACEHOLDER_REWARD_TYPE',
    description:
      option.description ?? 'PLACEHOLDER_REWARD_DESCRIPTION',
    rarity: option.rarity,
  };
}

function selectChoice(choiceId: string) {
  if (!choiceId || props.isLoading) {
    return;
  }

  selectedChoiceId.value = choiceId;
  emit('selectReward', choiceId);
}
</script>

<template>
  <section class="reward-offer">
    <header>
      <p class="system-label">Récompense · reward offered</p>

      <h2>
        {{ offer.title ?? 'PLACEHOLDER_REWARD_OFFER_TITLE' }}
      </h2>

      <p>
        {{ offer.description ?? 'PLACEHOLDER_REWARD_OFFER_DESCRIPTION' }}
      </p>
    </header>

    <section
      v-if="rewardChoices.length > 0"
      class="reward-offer__options"
    >
      <button
        v-for="choice in rewardChoices"
        :key="choice.id"
        class="reward-offer__option"
        :class="{
          'reward-offer__option--selected': selectedChoiceId === choice.id,
        }"
        :disabled="isLoading || !choice.id"
        @click="selectChoice(choice.id)"
      >
        <span class="system-label">
          {{ choice.rarity ?? 'PLACEHOLDER_RARITY' }}
        </span>

        <strong>
          {{ choice.label }}
        </strong>

        <small>
          {{ choice.rewardType }}
        </small>

        <p>
          {{ choice.description }}
        </p>

        <span
          v-if="selectedChoiceId === choice.id"
          class="reward-offer__selected-label"
        >
          ◎ Choisi
        </span>
      </button>
    </section>

    <section
      v-else
      class="reward-offer__empty panel"
    >
      <p class="system-label">REWARD_OFFER_EMPTY</p>

      <h3>NO_REWARDS_AVAILABLE</h3>

      <p>
        PLACEHOLDER_REWARD_OFFER_EMPTY_DESCRIPTION
      </p>
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
  position: relative;
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

.reward-offer__option--selected {
  border-color: var(--color-gold);
  box-shadow: 0 0 40px color-mix(in oklch, var(--color-gold), transparent 70%);
}

.reward-offer__option:disabled {
  opacity: 0.65;
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

.reward-offer__selected-label {
  position: absolute;
  right: var(--space-4);
  bottom: var(--space-4);
  color: var(--color-gold);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.reward-offer__empty {
  max-width: 42rem;
  padding: var(--space-6);
  border-color: color-mix(in oklch, var(--color-blood), transparent 35%);
}

.reward-offer__empty h3 {
  color: var(--color-blood);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.reward-offer__empty p:last-child {
  color: var(--color-muted);
  line-height: 1.6;
}
</style>