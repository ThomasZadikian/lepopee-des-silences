<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import {
    getOutcomeChoices,
    getOutcomeFamily,
    isChoiceOutcome,
    isRewardLikeOutcome,
    type EventOutcomeDto,
} from '../types/eventTypes';

const props = defineProps<{
  outcome: EventOutcomeDto;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  continue: [];
  selectChoice: [choiceId: string];
}>();

const selectedChoiceId = ref<string | null>(null);

const choices = computed(() => getOutcomeChoices(props.outcome));

const outcomeFamily = computed(() => getOutcomeFamily(
  props.outcome.resolutionKind,
));

const requiresChoice = computed(() => isChoiceOutcome(props.outcome));

const isRewardLike = computed(() => isRewardLikeOutcome(
  props.outcome.resolutionKind,
));

watch(
  () => props.outcome.nodeId,
  () => {
    selectedChoiceId.value = null;
  },
);

function selectChoice(choiceId: string) {
  if (!choiceId || props.isLoading) {
    return;
  }

  selectedChoiceId.value = choiceId;
  emit('selectChoice', choiceId);
}
</script>

<template>
  <section class="event-outcome">
    <header class="event-outcome__header">
      <p class="system-label">
        {{ outcomeFamily }} · {{ outcome.resolutionKind }}
      </p>

      <h2>{{ outcome.title }}</h2>

      <p>
        {{ outcome.description }}
      </p>
    </header>

    <section
      v-if="outcome.narrativeFragments && outcome.narrativeFragments.length > 0"
      class="event-outcome__fragments"
    >
      <article
        v-for="fragment in outcome.narrativeFragments"
        :key="`${fragment.speaker}-${fragment.text}`"
        class="event-outcome__fragment"
      >
        <span class="system-label">{{ fragment.speaker }}</span>
        <p>{{ fragment.text }}</p>
      </article>
    </section>

    <section class="event-outcome__meta">
      <div>
        <span class="system-label">Risque</span>
        <strong>{{ outcome.riskLevel }}</strong>
      </div>

      <div>
        <span class="system-label">Récompense</span>
        <strong>{{ outcome.rewardProfile }}</strong>
      </div>

      <div>
        <span class="system-label">État</span>
        <strong>
          {{ requiresChoice ? 'Choix requis' : 'Résolu' }}
        </strong>
      </div>
    </section>

    <section
      v-if="requiresChoice"
      class="event-outcome__choices"
    >
      <p class="system-label">Choix joueur requis</p>

      <div
        v-if="choices.length > 0"
        class="event-outcome__choice-list"
      >
        <button
          v-for="choice in choices"
          :key="choice.id"
          class="event-outcome__choice"
          :class="{
            'event-outcome__choice--disabled': !choice.isEnabled,
            'event-outcome__choice--selected': selectedChoiceId === choice.id,
          }"
          :disabled="isLoading || !choice.isEnabled"
          @click="selectChoice(choice.id)"
        >
          <strong>{{ choice.label }}</strong>
          <p>{{ choice.description }}</p>
          <small>{{ choice.id }}</small>

          <span
            v-if="selectedChoiceId === choice.id"
            class="event-outcome__selected-label"
          >
            ◎ Choisi
          </span>
        </button>
      </div>

      <p
        v-else
        class="event-outcome__warning"
      >
        Le backend indique qu’un choix est requis, mais aucun choix exploitable
        n’a été reçu par le client.
      </p>
    </section>

    <section
      v-else
      class="event-outcome__actions"
    >
      <p
        v-if="isRewardLike"
        class="event-outcome__note"
      >
        Le Palais a enregistré l’issue. Si une récompense structurée est requise,
        elle sera proposée dans l’écran dédié.
      </p>

      <button
        class="ghost-button event-outcome__continue"
        :disabled="isLoading"
        @click="$emit('continue')"
      >
        Continuer →
      </button>
    </section>
  </section>
</template>

<style scoped>
.event-outcome {
  display: grid;
  gap: var(--space-6);
  height: 100%;
  align-content: center;
}

.event-outcome__header {
  max-width: 52rem;
}

.event-outcome__header h2 {
  margin: var(--space-2) 0;
  color: var(--color-frost);
  font-size: clamp(2rem, 4vw, 4rem);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.event-outcome__header p:last-child {
  color: var(--color-muted);
  line-height: 1.65;
}

.event-outcome__fragments {
  display: grid;
  gap: var(--space-3);
  max-width: 52rem;
}

.event-outcome__fragment {
  padding: var(--space-4);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 35%);
  background: color-mix(in oklch, var(--color-panel), transparent 8%);
}

.event-outcome__fragment p {
  margin-bottom: 0;
  color: var(--color-ink);
  line-height: 1.6;
}

.event-outcome__meta {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 12rem));
  gap: var(--space-3);
}

.event-outcome__meta div {
  padding-top: var(--space-2);
  border-top: 1px solid var(--color-line);
}

.event-outcome__meta strong {
  display: block;
  margin-top: var(--space-1);
  color: var(--color-gold);
  font-family: var(--font-mono);
  font-size: 0.85rem;
}

.event-outcome__choices {
  display: grid;
  gap: var(--space-4);
}

.event-outcome__choice-list {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: var(--space-4);
}

.event-outcome__choice {
  position: relative;
  min-height: 12rem;
  padding: var(--space-4);
  color: var(--color-ink);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 25%);
  background: color-mix(in oklch, var(--color-panel), transparent 4%);
  text-align: left;
  cursor: pointer;
}

.event-outcome__choice:hover:not(:disabled) {
  border-color: var(--color-gold);
  box-shadow: 0 0 30px color-mix(in oklch, var(--color-gold), transparent 78%);
}

.event-outcome__choice--disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.event-outcome__choice--selected {
  border-color: var(--color-gold);
  box-shadow: 0 0 34px color-mix(in oklch, var(--color-gold), transparent 72%);
}

.event-outcome__choice strong {
  color: var(--color-ink);
  font-size: 1.1rem;
}

.event-outcome__choice p {
  color: var(--color-muted);
  line-height: 1.55;
}

.event-outcome__choice small {
  color: var(--color-dim);
  font-family: var(--font-mono);
  font-size: 0.7rem;
}

.event-outcome__selected-label {
  position: absolute;
  right: var(--space-4);
  bottom: var(--space-4);
  color: var(--color-gold);
  font-family: var(--font-mono);
  font-size: 0.72rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.event-outcome__warning {
  max-width: 48rem;
  color: var(--color-gold);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  line-height: 1.6;
}

.event-outcome__actions {
  display: grid;
  gap: var(--space-4);
  justify-items: start;
}

.event-outcome__note {
  max-width: 46rem;
  color: var(--color-muted);
  line-height: 1.6;
}

.event-outcome__continue {
  border-color: var(--color-frost);
  color: var(--color-frost);
}
</style>