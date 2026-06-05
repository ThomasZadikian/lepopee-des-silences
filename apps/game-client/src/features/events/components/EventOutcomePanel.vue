<script setup lang="ts">
import { computed } from 'vue';

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

defineEmits<{
  continue: [];
}>();

const choices = computed(() => getOutcomeChoices(props.outcome));

const outcomeFamily = computed(() => getOutcomeFamily(
  props.outcome.resolutionKind,
));

const requiresChoice = computed(() => isChoiceOutcome(props.outcome));

const isRewardLike = computed(() => isRewardLikeOutcome(
  props.outcome.resolutionKind,
));
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
        <article
          v-for="choice in choices"
          :key="choice.id"
          class="event-outcome__choice"
          :class="{ 'event-outcome__choice--disabled': !choice.isEnabled }"
        >
          <strong>{{ choice.label }}</strong>
          <p>{{ choice.description }}</p>
          <small>{{ choice.id }}</small>
        </article>
      </div>

      <p class="event-outcome__warning">
        Les choix d’événements sont détectés mais seront branchés dans la prochaine PR frontend.
        Pour le moment, cet événement est affiché sans résolution interactive.
      </p>
    </section>

    <section
      v-else
      class="event-outcome__actions"
    >
      <p v-if="isRewardLike" class="event-outcome__note">
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
  min-height: 12rem;
  padding: var(--space-4);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 25%);
  background: color-mix(in oklch, var(--color-panel), transparent 4%);
}

.event-outcome__choice--disabled {
  opacity: 0.55;
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