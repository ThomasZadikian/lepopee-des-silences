<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { EventOutcomeDto } from '../types/eventTypes';
import { getOutcomeChoices, isChoiceOutcome } from '../types/eventTypes';

// ── Props & Emits ──────────────────────────────────────────────────────────
const props = defineProps<{
  outcome: EventOutcomeDto
  isLoading: boolean
}>()

const emit = defineEmits<{
  continue: []
  selectChoice: [choiceId: string]
}>()

// ── Local state ────────────────────────────────────────────────────────────
const selectedChoiceId = ref<string | null>(null)

// ── Watch: reset on node change ────────────────────────────────────────────
watch(() => props.outcome.nodeId, () => {
  selectedChoiceId.value = null
})

// ── Computed ───────────────────────────────────────────────────────────────
const choices = computed(() => getOutcomeChoices(props.outcome))
const requiresChoice = computed(() => isChoiceOutcome(props.outcome))

const outcomeFamily = computed<string>(() => {
  const k = props.outcome.resolutionKind ?? ''
  if (k.includes('Combat') || k.includes('Encounter') || k.includes('Boss') || k.includes('Curse')) return 'blood'
  if (k.includes('Reward') || k.includes('Rare') || k.includes('Tome') || k.includes('Law')) return 'gold'
  if (k.includes('Rest')) return 'sap'
  return 'frost'
})

// ── Family → design token map ──────────────────────────────────────────────
const familyColor = computed(() => {
  const f = outcomeFamily.value
  if (f === 'blood') return 'var(--blood, oklch(0.52 0.15 20))'
  if (f === 'gold')  return 'var(--gold, oklch(0.72 0.1 85))'
  if (f === 'sap')   return 'var(--sap, oklch(0.68 0.1 130))'
  return 'var(--frost, oklch(0.6 0.1 195))'
})

const familyWash = computed(() => {
  const f = outcomeFamily.value
  if (f === 'blood') return 'var(--wash-blood, oklch(0.52 0.15 20 / 0.08))'
  if (f === 'gold')  return 'var(--wash-gold, oklch(0.72 0.1 85 / 0.08))'
  if (f === 'sap')   return 'var(--wash-sap, oklch(0.68 0.1 130 / 0.08))'
  return 'var(--wash-frost, oklch(0.6 0.1 195 / 0.08))'
})

// ── Accordion state for fragments ─────────────────────────────────────────
const openFragments = ref<Set<number>>(new Set([0]))
function toggleFragment(i: number) {
  if (openFragments.value.has(i)) openFragments.value.delete(i)
  else openFragments.value.add(i)
  openFragments.value = new Set(openFragments.value)
}

// ── Actions ────────────────────────────────────────────────────────────────
function confirmChoice() {
  if (requiresChoice.value && selectedChoiceId.value) {
    emit('selectChoice', selectedChoiceId.value)
  } else if (!requiresChoice.value) {
    emit('continue')
  }
}

function selectChoice(id: string) {
  selectedChoiceId.value = id
}
</script>

<template>
  <div
    class="eo-panel es-panel"
    :style="{ '--family-color': familyColor, '--family-wash': familyWash }"
  >
    <!-- Atmospheric grain -->
    <div class="eo-grain" aria-hidden="true" />

    <!-- Top kicker strip -->
    <div class="eo-kicker-row">
      <span class="es-kicker eo-kicker">
        <span class="eo-kicker-dot" :style="{ background: familyColor }" />
        Événement
      </span>
      <span
        v-if="isLoading"
        class="eo-loading-badge"
      >
        Résolution en cours…
      </span>
    </div>

    <!-- ── Main title ─────────────────────────────────────────────────────── -->
    <h2 class="es-h2 eo-title">
      {{ outcome.title }}
    </h2>

    <!-- ── Description ───────────────────────────────────────────────────── -->
    <p
      v-if="outcome.description"
      class="es-body eo-desc"
    >
      {{ outcome.description }}
    </p>

    <!-- ── Fragments narratifs (accordion) ──────────────────────────────── -->
    <div
      v-if="outcome.narrativeFragments && outcome.narrativeFragments.length"
      class="eo-fragments"
    >
      <div class="eo-divider" :style="{ borderColor: familyColor + ' / 0.3' }" />
      <div
        v-for="(frag, i) in outcome.narrativeFragments"
        :key="'frag-' + i"
        class="eo-fragment"
        :class="{ 'eo-fragment--open': openFragments.has(i) }"
      >
        <button
          class="eo-fragment__hd"
          :aria-expanded="openFragments.has(i)"
          @click="toggleFragment(i)"
        >
          <svg
            class="eo-chevron"
            :class="{ 'eo-chevron--open': openFragments.has(i) }"
            width="11"
            height="11"
            viewBox="0 0 11 11"
            fill="none"
            stroke="currentColor"
            stroke-width="1.8"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
          >
            <path d="M3.5 2L7.5 5.5L3.5 9" />
          </svg>
          <span class="eo-fragment__label es-label">{{ frag.speaker }}</span>
        </button>
        <div
          class="eo-fragment__body"
          :class="{ 'eo-fragment__body--open': openFragments.has(i) }"
        >
          <p class="es-body eo-fragment__text">{{ frag.text }}</p>
        </div>
      </div>
      <div class="eo-divider" />
    </div>

    <!-- ── Choices ────────────────────────────────────────────────────────── -->
    <div
      v-if="requiresChoice"
      class="eo-choices"
    >
      <span class="es-label eo-choices-label">Que fais-tu ?</span>

      <div
        v-for="choice in choices"
        :key="choice.id"
        class="eo-choice"
        :class="{
          'eo-choice--selected':   selectedChoiceId === choice.id,
          'eo-choice--unselected': selectedChoiceId !== null && selectedChoiceId !== choice.id,
          'eo-choice--disabled':   !choice.isEnabled,
        }"
        :aria-pressed="selectedChoiceId === choice.id"
        :aria-disabled="!choice.isEnabled"
        role="button"
        :tabindex="choice.isEnabled ? 0 : -1"
        @click="choice.isEnabled && selectChoice(choice.id)"
        @keydown.enter.prevent="choice.isEnabled && selectChoice(choice.id)"
        @keydown.space.prevent="choice.isEnabled && selectChoice(choice.id)"
      >
        <div class="eo-choice__content">
          <div class="eo-choice__label">{{ choice.label }}</div>
          <p
            v-if="choice.description"
            class="es-body eo-choice__desc"
          >
            {{ choice.description }}
          </p>
        </div>

        <!-- Selection indicator -->
        <div class="eo-choice__indicator" aria-hidden="true">
          <svg
            v-if="selectedChoiceId === choice.id"
            width="14"
            height="14"
            viewBox="0 0 14 14"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          >
            <path d="M2 7l4 4 6-7" />
          </svg>
          <svg
            v-else
            width="14"
            height="14"
            viewBox="0 0 14 14"
            fill="none"
            stroke="currentColor"
            stroke-width="1.4"
            stroke-linecap="round"
          >
            <circle cx="7" cy="7" r="5" />
          </svg>
        </div>
      </div>
    </div>

    <!-- ── Footer actions ─────────────────────────────────────────────────── -->
    <div class="eo-footer">
      <!-- Irreversible warning -->
      <div class="eo-footer__warning">
        <svg
          width="12"
          height="12"
          viewBox="0 0 12 12"
          fill="none"
          stroke="var(--blood, oklch(0.52 0.15 20))"
          stroke-width="1.4"
          stroke-linecap="round"
          aria-hidden="true"
        >
          <path d="M6 1L11 10H1L6 1Z" />
          <path d="M6 5v3M6 9.5v.5" />
        </svg>
        <span class="es-label eo-footer__warn-text">Choix irréversible</span>
      </div>

      <!-- Confirm button -->
      <button
        class="eo-confirm-btn"
        :disabled="isLoading || (requiresChoice && !selectedChoiceId)"
        :class="{ 'eo-confirm-btn--disabled': requiresChoice && !selectedChoiceId }"
        @click="confirmChoice"
      >
        <span v-if="isLoading" class="eo-confirm-btn__spinner" aria-hidden="true" />
        {{ requiresChoice ? 'Confirmer ce choix →' : 'Continuer →' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
/* ── Root panel ──────────────────────────────────────────────────────────── */
.eo-panel {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 32px 36px 28px;
  background: var(--panel, oklch(0.24 0.025 270));
  border: 1px solid var(--line-soft, oklch(0.38 0.02 275 / 0.4));
  border-radius: 4px;
  overflow: hidden;
  min-height: 400px;
}

/* ── Grain overlay ───────────────────────────────────────────────────────── */
.eo-grain {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  background-image: url("data:image/svg+xml,%3Csvg viewBox='0 0 200 200' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)' opacity='0.05'/%3E%3C/svg%3E");
  opacity: 0.25;
  mix-blend-mode: overlay;
}

/* Ensure children are above grain */
.eo-panel > *:not(.eo-grain) {
  position: relative;
  z-index: 1;
}

/* ── Kicker row ──────────────────────────────────────────────────────────── */
.eo-kicker-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.eo-kicker {
  display: inline-flex;
  align-items: center;
  gap: 7px;
}
.eo-kicker-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  flex-shrink: 0;
}
.eo-loading-badge {
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--family-color, var(--frost));
  border: 1px solid var(--family-color, var(--frost));
  padding: 3px 9px;
  border-radius: 3px;
  opacity: 0.8;
  animation: eo-pulse 1.6s ease-in-out infinite;
}
@keyframes eo-pulse {
  0%, 100% { opacity: 0.5; }
  50%       { opacity: 1; }
}

/* ── Title ───────────────────────────────────────────────────────────────── */
.eo-title {
  font-family: var(--font-display, serif);
  font-size: clamp(24px, 3.5vw, 38px);
  line-height: 1.06;
  color: var(--family-color, var(--frost));
  margin: 0 0 16px;
  letter-spacing: -0.01em;
}

/* ── Lede ────────────────────────────────────────────────────────────────── */
.eo-lede {
  font-family: var(--font, serif);
  font-size: 16px;
  font-style: italic;
  line-height: 1.6;
  color: var(--ink-2, oklch(0.72 0.018 70));
  margin: 0 0 12px;
}

/* ── Description ─────────────────────────────────────────────────────────── */
.eo-desc {
  font-size: 14px;
  line-height: 1.65;
  color: var(--ink-3, oklch(0.65 0.02 275));
  margin: 0 0 20px;
}

/* ── Divider ─────────────────────────────────────────────────────────────── */
.eo-divider {
  border: none;
  border-top: 1px solid var(--line-soft, oklch(0.38 0.02 275 / 0.4));
  margin: 4px 0;
}

/* ── Fragments ───────────────────────────────────────────────────────────── */
.eo-fragments {
  display: flex;
  flex-direction: column;
  gap: 0;
  margin: 0 0 22px;
}

.eo-fragment {
  border-bottom: 1px solid var(--line-soft, oklch(0.38 0.02 275 / 0.35));
}
.eo-fragment__hd {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 12px 0;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--ink-2, oklch(0.72 0.018 70));
  text-align: left;
  transition: color 0.16s;
}
.eo-fragment__hd:hover {
  color: var(--ink, oklch(0.88 0.015 70));
}
.eo-fragment--open .eo-fragment__hd {
  color: var(--ink, oklch(0.88 0.015 70));
}
.eo-fragment__label {
  font-size: 11px;
  color: inherit;
}

.eo-chevron {
  flex-shrink: 0;
  color: var(--ink-4, oklch(0.45 0.015 275));
  transition: transform 0.22s ease;
}
.eo-chevron--open {
  transform: rotate(90deg);
  color: var(--family-color, var(--frost));
}

.eo-fragment__body {
  max-height: 0;
  overflow: hidden;
  opacity: 0;
  transition: max-height 0.28s ease, opacity 0.22s ease;
}
.eo-fragment__body--open {
  max-height: 400px;
  opacity: 1;
}
.eo-fragment__text {
  font-size: 13.5px;
  line-height: 1.6;
  color: var(--ink-3, oklch(0.65 0.02 275));
  padding: 4px 0 14px 21px;
  margin: 0;
}

/* ── Choices label ───────────────────────────────────────────────────────── */
.eo-choices {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 26px;
}
.eo-choices-label {
  font-size: 9.5px;
  color: var(--ink-4, oklch(0.45 0.015 275));
  margin-bottom: 4px;
}

/* ── Individual choice card ──────────────────────────────────────────────── */
.eo-choice {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 14px;
  padding: 14px 16px;
  border: 1px solid var(--line-soft, oklch(0.38 0.02 275 / 0.4));
  border-radius: 4px;
  background: oklch(0.24 0.015 283 / 0.4);
  cursor: pointer;
  transition: border-color 0.18s, background 0.18s, box-shadow 0.18s;
  user-select: none;
  outline: none;
}
.eo-choice:hover {
  border-color: var(--frost, oklch(0.6 0.1 195));
  background: oklch(0.3 0.03 232 / 0.14);
  box-shadow: 0 0 20px -10px oklch(0.6 0.1 195 / 0.45);
}
.eo-choice:focus-visible {
  border-color: var(--frost, oklch(0.6 0.1 195));
  box-shadow: 0 0 0 2px oklch(0.6 0.1 195 / 0.35);
}
.eo-choice--selected {
  border-color: var(--frost, oklch(0.6 0.1 195)) !important;
  background: oklch(0.3 0.03 232 / 0.18) !important;
  box-shadow: 0 0 24px -8px oklch(0.7 0.07 232 / 0.6) !important;
}
.eo-choice--unselected {
  opacity: 0.55;
}
.eo-choice--disabled {
  opacity: 0.35;
  cursor: not-allowed;
  pointer-events: none;
}

/* ── Risk pill inside choice ─────────────────────────────────────────────── */
.eo-choice__risk {
  position: absolute;
  top: 10px;
  right: 14px;
  font-size: 9px;
  padding: 2px 7px;
  border-radius: 3px;
  background: oklch(0.22 0.02 270 / 0.6);
}
.eo-risk--low     { color: var(--frost, oklch(0.6 0.1 195)); }
.eo-risk--moderate{ color: var(--gold, oklch(0.72 0.1 85)); }
.eo-risk--high    { color: var(--blood, oklch(0.52 0.15 20)); }
.eo-risk--critical{ color: var(--blood); font-weight: 700; }

/* ── Choice text area ─────────────────────────────────────────────────────── */
.eo-choice__content {
  flex: 1;
  min-width: 0;
  padding-right: 40px;
}
.eo-choice__label {
  font-size: 15px;
  font-weight: 600;
  color: var(--ink-2, oklch(0.72 0.018 70));
  margin-bottom: 4px;
  transition: color 0.16s;
  line-height: 1.25;
}
.eo-choice--selected .eo-choice__label {
  color: var(--ink, oklch(0.88 0.015 70));
}
.eo-choice__desc {
  font-size: 12.5px;
  line-height: 1.5;
  color: var(--ink-4, oklch(0.45 0.015 275));
  margin: 0;
}

/* ── Selection circle / check ─────────────────────────────────────────────── */
.eo-choice__indicator {
  flex-shrink: 0;
  margin-top: 2px;
  color: var(--ink-4, oklch(0.45 0.015 275));
  transition: color 0.16s;
}
.eo-choice--selected .eo-choice__indicator {
  color: var(--frost, oklch(0.6 0.1 195));
}

/* ── Footer ──────────────────────────────────────────────────────────────── */
.eo-footer {
  margin-top: auto;
  padding-top: 18px;
  border-top: 1px solid var(--line-soft, oklch(0.38 0.02 275 / 0.4));
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}
.eo-footer__warning {
  display: inline-flex;
  align-items: center;
  gap: 7px;
}
.eo-footer__warn-text {
  color: var(--blood, oklch(0.52 0.15 20));
  opacity: 0.75;
  font-size: 10px;
}

/* ── Confirm button ──────────────────────────────────────────────────────── */
.eo-confirm-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 11px 22px;
  border: 1px solid var(--frost, oklch(0.6 0.1 195));
  border-radius: 3px;
  background: oklch(0.5 0.09 195 / 0.15);
  color: var(--frost, oklch(0.6 0.1 195));
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.18s, box-shadow 0.18s, opacity 0.16s;
}
.eo-confirm-btn:hover:not(:disabled) {
  background: oklch(0.5 0.09 195 / 0.28);
  box-shadow: 0 0 20px -8px oklch(0.6 0.1 195 / 0.5);
}
.eo-confirm-btn:disabled,
.eo-confirm-btn--disabled {
  opacity: 0.38;
  cursor: not-allowed;
  pointer-events: none;
}

/* ── Loading spinner ─────────────────────────────────────────────────────── */
.eo-confirm-btn__spinner {
  display: inline-block;
  width: 12px;
  height: 12px;
  border: 1.5px solid oklch(0.6 0.1 195 / 0.35);
  border-top-color: var(--frost, oklch(0.6 0.1 195));
  border-radius: 50%;
  animation: eo-spin 0.7s linear infinite;
}
@keyframes eo-spin {
  to { transform: rotate(360deg); }
}

/* ── Utility classes shared with design system ───────────────────────────── */
.es-kicker {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-3, oklch(0.65 0.02 275));
}
.es-label {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-3, oklch(0.65 0.02 275));
}
.es-body {
  font-family: var(--font, serif);
  font-size: 14px;
  line-height: 1.65;
  color: var(--ink-3, oklch(0.65 0.02 275));
}
.es-h2 {
  font-family: var(--font-display, serif);
  font-size: 28px;
  font-weight: 400;
  letter-spacing: -0.01em;
  line-height: 1.1;
}
.es-panel {
  background: var(--panel, oklch(0.24 0.025 270));
  border: 1px solid var(--line-soft, oklch(0.38 0.02 275 / 0.4));
  border-radius: 4px;
}
</style>
