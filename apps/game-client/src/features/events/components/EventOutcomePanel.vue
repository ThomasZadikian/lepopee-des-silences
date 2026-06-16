<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { EventOutcomeDto } from '../types/eventTypes'
import { getOutcomeChoices, getOutcomeFamily, isChoiceOutcome } from '../types/eventTypes'

const props = defineProps<{ outcome: EventOutcomeDto; isLoading: boolean }>()
const emit = defineEmits<{ continue: []; selectChoice: [choiceId: string] }>()

const selectedChoiceId = ref<string | null>(null)
const drawerOpen = ref(false)
const openChoiceIds = ref<Set<string>>(new Set())

const choices = computed(() => getOutcomeChoices(props.outcome))
const requiresChoice = computed(() => isChoiceOutcome(props.outcome))
const outcomeFamily = computed(() => getOutcomeFamily(props.outcome.resolutionKind ?? ''))

watch(() => props.outcome.nodeId, () => {
  selectedChoiceId.value = null
  drawerOpen.value = false
  openChoiceIds.value = new Set()
})

function familyTone(): 'frost' | 'gold' | 'blood' | 'sap' {
  const k = props.outcome.resolutionKind ?? ''
  if (k.includes('Combat') || k.includes('Encounter') || k.includes('Boss') || k.includes('Curse')) return 'blood'
  if (k.includes('Reward') || k.includes('Rare') || k.includes('Tome') || k.includes('Law')) return 'gold'
  if (k.includes('Rest')) return 'sap'
  return 'frost'
}

function familyColor(): string {
  const t = familyTone()
  if (t === 'blood') return 'var(--blood, oklch(0.52 0.15 20))'
  if (t === 'gold')  return 'var(--gold, oklch(0.72 0.1 85))'
  if (t === 'sap')   return 'var(--sap, oklch(0.68 0.1 130))'
  return 'var(--frost, oklch(0.6 0.1 195))'
}

const eliseQuote = computed(() =>
  props.outcome.narrativeFragments?.[0]?.text ?? props.outcome.description ?? ''
)
const eliseLabel = computed(() =>
  props.outcome.narrativeFragments?.[0]?.speaker ?? 'Élise'
)

const bodyFragments = computed(() =>
  (props.outcome.narrativeFragments?.slice(1) ?? [])
)

function toggleChoice(id: string) {
  const s = new Set(openChoiceIds.value)
  if (s.has(id)) s.delete(id)
  else s.add(id)
  openChoiceIds.value = s
}

function selectAndClose(id: string) {
  selectedChoiceId.value = id
  openChoiceIds.value = new Set()
}

function confirmChoice() {
  if (selectedChoiceId.value) {
    emit('selectChoice', selectedChoiceId.value)
  }
}
</script>

<template>
  <div class="eo-root" :style="{ '--family-color': familyColor() }">
    <!-- Atmospheric backdrop -->
    <div class="eo-atmos" aria-hidden="true" />

    <!-- Two-column layout -->
    <div class="eo-layout">
      <!-- ── LEFT: Elise column ── -->
      <aside class="eo-left">
        <!-- Portrait placeholder -->
        <div class="eo-portrait">
          <div class="eo-portrait__inner">
            <svg width="56" height="56" viewBox="0 0 56 56" fill="none" aria-hidden="true">
              <circle cx="28" cy="20" r="10" stroke="var(--family-color)" stroke-width="1" opacity=".45"/>
              <path d="M8 52c0-11 8.95-20 20-20s20 8.95 20 20" stroke="var(--family-color)" stroke-width="1" opacity=".35"/>
            </svg>
            <span class="eo-portrait__label es-label">Portraiture</span>
          </div>
        </div>

        <!-- Elise dialogue -->
        <div class="eo-elise-block">
          <div class="eo-elise-name">
            <svg width="9" height="9" viewBox="0 0 9 9" fill="var(--family-color)" aria-hidden="true">
              <circle cx="4.5" cy="4.5" r="3.5"/>
            </svg>
            <span class="es-label" style="color: var(--family-color);">{{ eliseLabel }}</span>
          </div>
          <p class="eo-elise-text">{{ eliseQuote }}</p>
        </div>
      </aside>

      <!-- ── RIGHT: Narrative column ── -->
      <section class="eo-right">
        <!-- Corner accents -->
        <span class="es-corner tl" />
        <span class="es-corner tr" />
        <span class="es-corner bl" />
        <span class="es-corner br" />

        <!-- Chip + resolution kind -->
        <div class="eo-top-row">
          <span
            class="es-chip"
            :class="{
              'es-chip--frost': familyTone() === 'frost',
              'es-chip--gold':  familyTone() === 'gold',
              'es-chip--blood': familyTone() === 'blood',
              'es-chip--sap':   familyTone() === 'sap',
            }"
          >{{ outcomeFamily }}</span>
          <span class="es-label" style="color: var(--ink-4);">{{ outcome.resolutionKind }}</span>
        </div>

        <!-- Title -->
        <h2 class="eo-title">{{ outcome.title }}</h2>

        <!-- Illustration slot -->
        <div class="eo-illus">
          <span class="es-label eo-illus__label">Souvenir</span>
        </div>

        <!-- Description -->
        <p v-if="outcome.description" class="es-lede eo-desc">{{ outcome.description }}</p>

        <!-- Additional fragments (index 1+) -->
        <template v-if="bodyFragments.length">
          <article
            v-for="(frag, i) in bodyFragments"
            :key="i"
            class="eo-fragment"
          >
            <span class="es-label eo-fragment__speaker">{{ frag.speaker }}</span>
            <p class="es-body">{{ frag.text }}</p>
          </article>
        </template>

        <!-- Spacer -->
        <div style="flex: 1;" />

        <!-- Footer -->
        <div class="eo-footer">
          <span class="es-label" style="color: var(--gold-dim, oklch(.65 .09 84 / .55));">
            ◆ Loi du Silence — conséquences voilées
          </span>
          <!-- Choice opener -->
          <button
            v-if="requiresChoice"
            class="es-btn es-btn--frost"
            :disabled="isLoading"
            @click="drawerOpen = true"
          >
            Que fais-tu ? ↓
          </button>
          <!-- Direct continue for non-choice -->
          <button
            v-else
            class="es-btn es-btn--frost es-btn--lg"
            :disabled="isLoading"
            @click="$emit('continue')"
          >
            {{ isLoading ? 'Résolution…' : 'Continuer →' }}
          </button>
        </div>
      </section>
    </div>

    <!-- ── BOTTOM DRAWER ── -->
    <Transition name="eo-drawer">
      <div v-if="drawerOpen && requiresChoice" class="eo-drawer">
        <div class="eo-drawer__inner">
          <!-- Drawer header -->
          <div class="eo-drawer__header">
            <span class="es-label" style="color: var(--ink-3);">Que fais-tu ?</span>
            <button class="eo-drawer__close" @click="drawerOpen = false" aria-label="Fermer">
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">
                <path d="M3 3l8 8M11 3l-8 8"/>
              </svg>
            </button>
          </div>

          <!-- Choice accordions -->
          <div class="eo-drawer__choices">
            <div
              v-for="choice in choices"
              :key="choice.id"
              class="eo-choice"
              :class="{
                'eo-choice--selected': selectedChoiceId === choice.id,
                'eo-choice--disabled': !choice.isEnabled,
              }"
            >
              <!-- Choice header (accordion toggle) -->
              <button
                class="eo-choice__hd"
                :disabled="!choice.isEnabled"
                @click="toggleChoice(choice.id)"
              >
                <svg
                  class="eo-choice__chevron"
                  :class="{ 'eo-choice__chevron--open': openChoiceIds.has(choice.id) }"
                  width="12" height="12" viewBox="0 0 12 12" fill="none"
                  stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
                  aria-hidden="true"
                >
                  <path d="M4 2l4 4-4 4"/>
                </svg>

                <span class="eo-choice__title">{{ choice.label }}</span>

                <span
                  v-if="selectedChoiceId === choice.id"
                  class="es-chip es-chip--frost eo-choice__sel-chip"
                >Sélectionné</span>
              </button>

              <!-- Choice body (collapsible) -->
              <div
                class="eo-choice__body"
                :class="{ 'eo-choice__body--open': openChoiceIds.has(choice.id) }"
              >
                <p class="es-body eo-choice__desc">{{ choice.description }}</p>
                <button
                  class="es-btn es-btn--ghost"
                  @click="selectAndClose(choice.id)"
                >
                  Sélectionner ce geste
                </button>
              </div>
            </div>
          </div>

          <!-- Drawer footer -->
          <div class="eo-drawer__footer">
            <button
              class="es-btn es-btn--ghost"
              @click="drawerOpen = false"
            >
              ← Revenir
            </button>
            <button
              class="es-btn es-btn--frost"
              :disabled="isLoading || !selectedChoiceId"
              @click="confirmChoice"
            >
              {{ isLoading ? 'Résolution…' : 'Valider ce choix →' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
/* ── Root ── */
.eo-root {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

/* ── Atmos ── */
.eo-atmos {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    radial-gradient(ellipse 70% 50% at 20% 60%, oklch(.28 .04 260 / .35) 0%, transparent 60%),
    radial-gradient(ellipse 50% 40% at 80% 30%, oklch(.22 .03 290 / .25) 0%, transparent 60%);
  z-index: 0;
}

/* ── Two-column layout ── */
.eo-layout {
  position: relative;
  z-index: 1;
  display: flex;
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

/* ── LEFT column ── */
.eo-left {
  width: 380px;
  flex-shrink: 0;
  border-right: 1px solid var(--line-soft, oklch(0.38 0.02 275 / .4));
  display: flex;
  flex-direction: column;
  padding: 40px 32px;
  gap: 28px;
  overflow-y: auto;
}

.eo-portrait {
  border: 1px solid var(--line, oklch(0.38 0.02 275 / .3));
  border-radius: 4px;
  background: linear-gradient(160deg, oklch(0.18 0.025 270 / .9), oklch(0.14 0.02 268 / .95));
  height: 260px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.eo-portrait__inner {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.eo-portrait__label {
  color: var(--ink-4);
}

.eo-elise-block {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.eo-elise-name {
  display: flex;
  align-items: center;
  gap: 8px;
}

.eo-elise-text {
  font-family: var(--font, serif);
  font-style: italic;
  font-size: 14.5px;
  line-height: 1.65;
  color: var(--ink-2, oklch(0.72 0.018 70));
  margin: 0;
}

/* ── RIGHT column ── */
.eo-right {
  position: relative;
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  padding: 40px 44px 32px;
  overflow-y: auto;
}

/* ── Top row: chip + label ── */
.eo-top-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}

/* ── Title ── */
.eo-title {
  font-family: var(--display, serif);
  font-size: 44px;
  font-weight: 400;
  line-height: 1.04;
  letter-spacing: -0.01em;
  color: var(--ink);
  margin: 0 0 22px;
}

/* ── Illustration slot ── */
.eo-illus {
  height: 180px;
  margin-bottom: 22px;
  border: 1px solid var(--line, oklch(0.38 0.02 275 / .3));
  border-radius: 4px;
  background: linear-gradient(160deg, oklch(0.18 0.025 270 / .9), oklch(0.14 0.02 268 / .95));
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.eo-illus__label {
  color: var(--ink-4);
}

/* ── Description ── */
.eo-desc {
  margin-bottom: 14px;
}

/* ── Body fragments (index 1+) ── */
.eo-fragment {
  padding: 12px 14px;
  background: var(--card-soft, oklch(0.24 0.025 270));
  border: 1px solid var(--line-soft);
  border-radius: 4px;
  margin-bottom: 8px;
}

.eo-fragment__speaker {
  display: block;
  margin-bottom: 4px;
}

/* ── Footer ── */
.eo-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 18px;
  border-top: 1px solid var(--line-soft, oklch(0.38 0.02 275 / .4));
  margin-top: 22px;
}

/* ── DRAWER ── */
.eo-drawer {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 10;
  height: 380px;
  background: var(--panel, oklch(0.20 0.025 270));
  border-top: 1px solid var(--line-soft, oklch(0.38 0.02 275 / .4));
  box-shadow: 0 -16px 48px -8px oklch(0.08 0.02 270 / .7);
  display: flex;
  flex-direction: column;
}

.eo-drawer__inner {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 0 44px;
}

.eo-drawer__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 0;
  border-bottom: 1px solid var(--line-soft, oklch(0.38 0.02 275 / .35));
  flex-shrink: 0;
}

.eo-drawer__close {
  background: none;
  border: none;
  cursor: pointer;
  color: var(--ink-4);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border-radius: 3px;
  transition: color .15s;
}
.eo-drawer__close:hover { color: var(--ink); }

.eo-drawer__choices {
  flex: 1;
  overflow-y: auto;
  padding: 12px 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.eo-drawer__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 0;
  border-top: 1px solid var(--line-soft, oklch(0.38 0.02 275 / .35));
  flex-shrink: 0;
}

/* ── Choice accordion ── */
.eo-choice {
  border: 1px solid var(--line-soft, oklch(0.38 0.02 275 / .3));
  border-radius: 4px;
  overflow: hidden;
  transition: border-color .18s;
}

.eo-choice--selected {
  border-color: var(--frost, oklch(0.6 0.1 195));
}

.eo-choice--disabled {
  opacity: 0.4;
  pointer-events: none;
}

.eo-choice__hd {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: none;
  border: none;
  cursor: pointer;
  text-align: left;
  color: var(--ink-2);
  transition: background .15s, color .15s;
}
.eo-choice__hd:hover {
  background: oklch(0.26 0.02 270 / .5);
  color: var(--ink);
}
.eo-choice--selected .eo-choice__hd {
  background: oklch(0.28 0.03 232 / .2);
}

.eo-choice__chevron {
  flex-shrink: 0;
  color: var(--ink-4);
  transition: transform .2s ease, color .15s;
}
.eo-choice__chevron--open {
  transform: rotate(90deg);
  color: var(--family-color, var(--frost));
}

.eo-choice__title {
  flex: 1;
  font-size: 14px;
  font-weight: 600;
  line-height: 1.3;
}

.eo-choice__sel-chip {
  font-size: 9px;
  padding: 2px 8px;
  flex-shrink: 0;
}

.eo-choice__body {
  max-height: 0;
  overflow: hidden;
  opacity: 0;
  transition: max-height .28s ease, opacity .22s ease;
}
.eo-choice__body--open {
  max-height: 200px;
  opacity: 1;
}

.eo-choice__desc {
  padding: 8px 16px 12px 40px;
  margin: 0;
  font-size: 13px;
  color: var(--ink-3);
}

.eo-choice__body .es-btn {
  margin: 0 16px 14px 40px;
}

/* ── Drawer transition ── */
.eo-drawer-enter-active,
.eo-drawer-leave-active {
  transition: transform .3s cubic-bezier(.32, .72, 0, 1);
}
.eo-drawer-enter-from,
.eo-drawer-leave-to {
  transform: translateY(100%);
}
</style>
