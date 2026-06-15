<script setup lang="ts">
import type { CurrentEventChoiceResultDto } from '../types/eventTypes';

defineProps<{
  result: CurrentEventChoiceResultDto;
  isLoading: boolean;
}>();

defineEmits<{
  continue: [];
}>();
</script>

<template>
  <section class="event-choice-result">
    <header>
      <p class="system-label">
        Choix résolu · {{ result.outcomeKind ?? result.state ?? 'Resolved' }}
      </p>

      <h2>{{ result.title ?? 'CHOISE_DONE' }}</h2>

      <p>
        {{ result.description ?? result.message ?? 'CHOICE_REGISTERED' }}
      </p>
    </header>

    <button
      class="ghost-button event-choice-result__continue"
      :disabled="isLoading"
      @click="$emit('continue')"
    >
      Continuer →
    </button>
  </section>
</template>

<style scoped>
.event-choice-result {
  display: grid;
  gap: var(--space-6);
  height: 100%;
  align-content: center;
}

.event-choice-result header {
  max-width: 52rem;
}

.event-choice-result h2 {
  margin: var(--space-2) 0;
  color: var(--gold);
  font-size: clamp(2rem, 4vw, 4rem);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.event-choice-result p:last-child {
  color: var(--ink-3);
  line-height: 1.65;
}

.event-choice-result__continue {
  justify-self: start;
  border-color: var(--frost);
  color: var(--frost);
}
</style>