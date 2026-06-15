<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import {
  getOutcomeChoices,
  getOutcomeFamily,
  isChoiceOutcome,
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
const outcomeFamily = computed(() => getOutcomeFamily(props.outcome.resolutionKind));
const requiresChoice = computed(() => isChoiceOutcome(props.outcome));

watch(() => props.outcome.nodeId, () => { selectedChoiceId.value = null; });

function selectChoice(choiceId: string) {
  if (!choiceId || props.isLoading) return;
  selectedChoiceId.value = choiceId;
  emit('selectChoice', choiceId);
}
</script>

<template>
  <section class="event-scene">
    <div class="es-atmos">
      <div class="es-vignette" />
      <div class="es-grain" />
    </div>

    <div class="event-scene__card">
      <!-- Header -->
      <header class="event-scene__header">
        <span class="es-kicker">{{ outcomeFamily }}</span>
        <h2 class="es-h2">{{ outcome.title }}</h2>
        <p class="es-lede es-dim">{{ outcome.description }}</p>
      </header>

      <!-- Narrative fragments -->
      <div v-if="outcome.narrativeFragments?.length" class="event-scene__fragments">
        <article
          v-for="fragment in outcome.narrativeFragments"
          :key="`${fragment.speaker}-${fragment.text}`"
          class="event-scene__fragment"
        >
          <span class="es-label">{{ fragment.speaker }}</span>
          <p class="es-body">{{ fragment.text }}</p>
        </article>
      </div>

      <hr class="es-rule" />

      <!-- Choices -->
      <div v-if="requiresChoice && choices.length > 0" class="event-scene__choices">
        <span class="es-kicker">Choix requis</span>
        <div class="event-scene__choice-list">
          <button
            v-for="choice in choices"
            :key="choice.id"
            class="event-choice"
            :class="{ 'event-choice--selected': selectedChoiceId === choice.id }"
            :disabled="isLoading || !choice.isEnabled"
            @click="selectChoice(choice.id)"
          >
            <template v-if="selectedChoiceId === choice.id">
              <div class="es-corner es-corner--frost tl" />
              <div class="es-corner es-corner--frost tr" />
              <div class="es-corner es-corner--frost bl" />
              <div class="es-corner es-corner--frost br" />
            </template>
            <h3 class="es-h3">{{ choice.label }}</h3>
            <p class="es-body">{{ choice.description }}</p>
            <span v-if="selectedChoiceId === choice.id" class="event-choice__picked">❦ Choisi</span>
          </button>
        </div>
      </div>

      <!-- Continue -->
      <div v-else class="event-scene__actions">
        <button class="es-btn es-btn--frost es-btn--lg" :disabled="isLoading" @click="$emit('continue')">
          {{ isLoading ? 'Résolution…' : 'Continuer →' }}
        </button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.event-scene {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: var(--space-6);
}

.event-scene__card {
  position: relative;
  z-index: 1;
  width: min(720px, 90vw);
  background: oklch(0.20 0.04 272 / 0.85);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  box-shadow:
    oklch(1 0 0 / 0.03) 0 0 0 1px inset,
    oklch(0.12 0.03 272 / 0.7) 0 0 30px -8px inset,
    var(--wash-frost) 0 0 40px -20px;
  backdrop-filter: blur(12px);
  padding: var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  max-height: 85vh;
  overflow-y: auto;
}

.event-scene__header {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
}

.event-scene__header h2 {
  margin: 0;
  color: var(--ink);
}

.event-scene__fragments {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.event-scene__fragment {
  padding: var(--space-3) var(--space-4);
  background: var(--card-soft);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
}

.event-scene__fragment .es-body {
  margin: var(--space-1) 0 0;
  color: var(--ink);
  line-height: 1.6;
}

.event-scene__choices {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
}

.event-scene__choice-list {
  display: flex;
  gap: var(--space-4);
  justify-content: center;
  flex-wrap: wrap;
}

.event-choice {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
  width: 200px;
  padding: var(--space-4);
  text-align: center;
  background: var(--card-soft);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  cursor: pointer;
  font-family: inherit;
  color: var(--ink);
  transition: border-color 0.2s ease;
}

.event-choice:hover:not(:disabled) {
  border-color: var(--edge-frost);
}

.event-choice--selected {
  border-color: var(--frost) !important;
  box-shadow: 0 0 16px oklch(0.846 0.100 276 / 0.2);
}

.event-choice:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.event-choice h3 {
  margin: 0;
  color: var(--ink);
}

.event-choice .es-body {
  margin: 0;
  font-size: 0.82rem;
  color: var(--ink-3);
  line-height: 1.5;
}

.event-choice__picked {
  color: var(--frost);
  font-family: var(--font-caps);
  font-size: 0.56rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  margin-top: var(--space-1);
}

.event-scene__actions {
  display: flex;
  justify-content: center;
}
</style>
