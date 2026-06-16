<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { EventOutcomeDto } from '../types/eventTypes';
import { getOutcomeChoices, getOutcomeFamily, isChoiceOutcome } from '../types/eventTypes';

const props = defineProps<{ outcome: EventOutcomeDto; isLoading: boolean }>()
const emit = defineEmits<{ continue: []; selectChoice: [choiceId: string] }>()

const selectedChoiceId = ref<string | null>(null)
const choices = computed(() => getOutcomeChoices(props.outcome))
const requiresChoice = computed(() => isChoiceOutcome(props.outcome))
const outcomeFamily = computed(() => getOutcomeFamily(props.outcome.resolutionKind))

watch(() => props.outcome.nodeId, () => { selectedChoiceId.value = null })

// Map resolutionKind to tone for chips
function familyTone(): 'frost' | 'gold' | 'blood' | '' {
  const k = props.outcome.resolutionKind ?? ''
  if (k.includes('Combat') || k.includes('Encounter') || k.includes('Boss') || k.includes('Curse')) return 'blood'
  if (k.includes('Reward') || k.includes('Rare') || k.includes('Tome') || k.includes('Law')) return 'gold'
  return 'frost'
}

function selectChoice(id: string) {
  selectedChoiceId.value = id
  emit('selectChoice', id)
}
</script>

<template>
  <div class="ev-root">
    <div class="ev-card es-plate es-plate--inset">
      <!-- Corner accents -->
      <span class="es-corner tl" />
      <span class="es-corner tr" />
      <span class="es-corner bl" />
      <span class="es-corner br" />

      <!-- Top row: chip + labels -->
      <div class="es-row" style="justify-content: space-between; margin-bottom: 18px;">
        <div class="es-row" style="gap: 12px; align-items: center;">
          <span
            class="es-chip"
            :class="{
              'es-chip--frost': familyTone() === 'frost',
              'es-chip--gold':  familyTone() === 'gold',
              'es-chip--blood': familyTone() === 'blood',
            }"
          >{{ outcomeFamily }}</span>
          <span class="es-label">
            {{ outcome.primaryEventType ?? outcome.eventTypes?.[0] ?? '' }}
          </span>
        </div>
        <span class="es-label" style="color: var(--ink-4)">{{ outcome.resolutionKind }}</span>
      </div>

      <!-- Title -->
      <h2 class="es-h2" style="font-size: 42px;">{{ outcome.title }}</h2>

      <!-- Illustration placeholder -->
      <div class="ev-slot">
        <span class="es-label ev-slot__label">Souvenir</span>
      </div>

      <!-- Description -->
      <p class="es-lede" style="margin-bottom: 10px;">{{ outcome.description }}</p>

      <!-- Narrative fragments -->
      <template v-if="outcome.narrativeFragments?.length">
        <article
          v-for="(fragment, i) in outcome.narrativeFragments"
          :key="i"
          class="ev-fragment"
        >
          <span class="es-label">{{ fragment.speaker }}</span>
          <p class="es-body">{{ fragment.text }}</p>
        </article>
      </template>

      <!-- Choice prompt rule -->
      <div v-if="requiresChoice" class="es-rule" style="margin-bottom: 16px;">
        <span class="es-label" style="flex: 0 0 auto; color: var(--ink-4);">Que fais-tu ?</span>
      </div>

      <!-- Choices -->
      <div v-if="requiresChoice" class="ev-choices">
        <div
          v-for="choice in choices"
          :key="choice.id"
          class="ev-choice"
          :class="{
            'ev-choice--sel': selectedChoiceId === choice.id,
            'ev-choice--dim': selectedChoiceId !== null && selectedChoiceId !== choice.id,
          }"
          @click="choice.isEnabled && selectChoice(choice.id)"
        >
          <!-- Left icon -->
          <svg
            class="ev-choice__icon"
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
            style="flex: 0 0 auto;"
          >
            <circle cx="12" cy="12" r="10"/>
            <path d="M12 8v4l3 3"/>
          </svg>

          <!-- Center content -->
          <div style="flex: 1;">
            <div class="ev-choice__title">{{ choice.label }}</div>
            <div class="es-body ev-choice__desc">{{ choice.description }}</div>
          </div>

          <!-- Right indicator -->
          <div class="ev-choice__indicator" style="flex: 0 0 auto;">
            <!-- Checkmark when selected -->
            <svg
              v-if="selectedChoiceId === choice.id"
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            >
              <circle cx="12" cy="12" r="10"/>
              <path d="M8 12l3 3 5-5"/>
            </svg>
            <!-- Circle when not selected -->
            <svg
              v-else
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="1.5"
              stroke-linecap="round"
            >
              <circle cx="12" cy="12" r="10"/>
            </svg>
          </div>
        </div>
      </div>

      <!-- Spacer -->
      <div style="flex: 1;" />

      <!-- Footer -->
      <div class="ev-footer">
        <span class="es-label" style="color: var(--gold-dim, oklch(.65 .09 84 / .55));">
          ◆ Loi du Silence — conséquences voilées
        </span>
        <button
          v-if="requiresChoice"
          class="es-btn es-btn--frost"
          :disabled="isLoading || !selectedChoiceId"
          @click="emit('selectChoice', selectedChoiceId!)"
        >
          Résoudre →
        </button>
        <button
          v-else
          class="es-btn es-btn--frost es-btn--lg"
          :disabled="isLoading"
          @click="$emit('continue')"
        >
          {{ isLoading ? 'Résolution…' : 'Continuer →' }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.ev-root {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: 28px 0;
}

.ev-card {
  position: relative;
  z-index: 1;
  width: min(900px, 94vw);
  padding: 40px;
  display: flex;
  flex-direction: column;
  max-height: 85vh;
  overflow-y: auto;
}

.ev-slot {
  height: 200px;
  margin: 18px 0;
  border: 1px solid var(--line);
  border-radius: 4px;
  background: linear-gradient(160deg, oklch(0.18 0.025 270 / 0.9), oklch(0.14 0.02 268 / 0.95));
  display: flex;
  align-items: center;
  justify-content: center;
}

.ev-slot__label {
  color: var(--ink-4);
}

.ev-fragment {
  padding: 12px 14px;
  background: var(--card-soft, oklch(0.24 0.025 270));
  border: 1px solid var(--line-soft);
  border-radius: 4px;
  margin-bottom: 8px;
}

.ev-fragment .es-body {
  margin: 4px 0 0;
  color: var(--ink);
}

.ev-choices {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 22px;
}

.ev-choice {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 14px 16px;
  cursor: pointer;
  border: 1px solid var(--line);
  border-radius: 4px;
  background: oklch(0.24 0.015 283 / 0.4);
  transition: border-color .18s, background .18s, box-shadow .18s;
}

.ev-choice:hover {
  border-color: var(--frost-dim);
}

.ev-choice--sel {
  border-color: var(--frost) !important;
  background: oklch(0.3 0.03 232 / 0.18) !important;
  box-shadow: 0 0 24px -8px oklch(0.7 0.07 232 / 0.6);
}

.ev-choice--dim {
  opacity: 0.55;
}

.ev-choice__title {
  font-size: 15px;
  font-weight: 600;
  color: var(--ink-2);
}

.ev-choice--sel .ev-choice__title {
  color: var(--ink);
}

.ev-choice__desc {
  font-size: 12.5px;
  margin-top: 2px;
  color: var(--ink-3);
}

.ev-choice__icon {
  color: var(--ink-3);
  transition: color .18s;
}

.ev-choice--sel .ev-choice__icon {
  color: var(--frost);
}

.ev-choice__indicator {
  color: var(--ink-4);
  transition: color .18s;
}

.ev-choice--sel .ev-choice__indicator {
  color: var(--frost);
}

.ev-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 22px;
  padding-top: 18px;
  border-top: 1px solid var(--line-soft);
}
</style>
