<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import EliseComment from '@/shared/components/EliseComment.vue'
import RuleOrnament from '@/shared/components/RuleOrnament.vue'
import SigilIcon from '@/shared/components/SigilIcon.vue'
import LawsPopover from './LawsPopover.vue'
import { computed, ref } from 'vue'
import type { EventOutcomeDto } from '../events/types/eventTypes'
import { getOutcomeChoices, isChoiceOutcome } from '../events/types/eventTypes'
import type { ActivePalaceLawDto, ActiveCurseDto } from '../runs/types/runTypes'

// ── Props & Emits ─────────────────────────────────────────────────────────
const props = defineProps<{
  outcome: EventOutcomeDto
  isLoading: boolean
  activeLaws?: ActivePalaceLawDto[] | null
  activeCurses?: ActiveCurseDto[] | null
}>()

const emit = defineEmits<{
  continue: []
  selectChoice: [choiceId: string]
}>()

// ── State ─────────────────────────────────────────────────────────────────
const sealed = ref(false)
const stamping = ref(false)
const showDrawer = ref(false)

// ── Computed ──────────────────────────────────────────────────────────────
const choices = computed(() => getOutcomeChoices(props.outcome))
const requiresChoice = computed(() => isChoiceOutcome(props.outcome))
const lawApplicationChoice = computed(() =>
  choices.value.find((choice) => choice.id.toLowerCase().startsWith('accept-law') && choice.isEnabled) ?? null,
)
const canSealDecision = computed(() =>
  !props.isLoading && !stamping.value && (!requiresChoice.value || lawApplicationChoice.value !== null),
)
const missingLawApplicationChoice = computed(() =>
  requiresChoice.value && lawApplicationChoice.value === null,
)
const sealButtonText = computed(() => {
  if (stamping.value) return 'Scellage…'
  if (!requiresChoice.value || lawApplicationChoice.value) return 'Apposer le sceau & inscrire au Tome'
  return 'Loi indisponible'
})

const openFragments = ref<Set<number>>(new Set([0]))
function toggleFragment(i: number) {
  if (openFragments.value.has(i)) openFragments.value.delete(i)
  else openFragments.value.add(i)
  openFragments.value = new Set(openFragments.value)
}

// ── Domain chip tone from primaryEventType ────────────────────────────────
function domainTone(type: string): 'gold' | 'frost' | 'blood' | null {
  const t = (type ?? '').toLowerCase()
  if (t.includes('combat') || t.includes('confron')) return 'blood'
  if (t.includes('mem') || t.includes('narr') || t.includes('récit')) return 'frost'
  if (t.includes('loi') || t.includes('édit') || t.includes('law')) return 'gold'
  return null
}

// ── Actions ───────────────────────────────────────────────────────────────
function applySeal() {
  if (!canSealDecision.value) return

  if (sealed.value) {
    proceed()
    return
  }
  stamping.value = true
  setTimeout(() => {
    stamping.value = false
    sealed.value = true
  }, 460)
}

function proceed() {
  if (requiresChoice.value && lawApplicationChoice.value) {
    emit('selectChoice', lawApplicationChoice.value.id)
  } else {
    emit('continue')
  }
}
</script>

<template>
  <div class="vlo-root">
    <div class="es-atmos" />
    <div class="es-vignette" />
    <div class="es-grain" />

    <!-- Influences drawer (opens when "Lois en vigueur" is clicked) -->
    <Transition name="slide">
      <LawsPopover
        v-if="showDrawer"
        :laws="activeLaws"
        :curses="activeCurses"
        @close="showDrawer = false"
      />
    </Transition>

    <div class="vlo-layout">

      <!-- ── LEFT COLUMN ── -->
      <div class="vlo-left">
        <span class="es-kicker" style="color: var(--gold-dim); margin-bottom: 24px">
          {{ sealed ? '◆ Promulguée · scellée au Tome' : '◆ Loi proposée · à inscrire' }}
        </span>

        <!-- Seal circle -->
        <div
          :class="['vlo-seal', sealed && 'vlo-seal--done']"
        >
          <!-- Inner ring -->
          <div class="vlo-seal__ring" />

          <!-- Sigil -->
          <div
            class="vlo-seal__sigil"
            :style="{ opacity: sealed ? 1 : 0.55, color: 'var(--gold)' }"
          >
            <SigilIcon kind="loi" :size="86" :stroke-width="1" />
          </div>

          <!-- Stamp ring (animates in) -->
          <div :class="['vlo-stamp', stamping && 'vlo-stamp--go']" />

          <!-- Checkmark badge (appears when sealed) -->
          <div :class="['vlo-check', sealed && 'vlo-check--show']">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 8l4 4 6-7" />
            </svg>
          </div>

          <!-- SVG arc text -->
          <svg class="vlo-art" viewBox="0 0 218 218" fill="none" aria-hidden="true">
            <path id="vlo-arc-top" d="M 30 109 A 79 79 0 0 1 188 109" fill="none" />
            <path id="vlo-arc-bot" d="M 188 109 A 79 79 0 0 1 30 109" fill="none" />
            <text font-family="var(--caps, 'Marcellus SC', serif)" font-size="9" letter-spacing="3" fill="currentColor" opacity="0.4" text-anchor="middle">
              <textPath href="#vlo-arc-top" startOffset="50%">Édit du Palais</textPath>
            </text>
            <text font-family="var(--mono, 'JetBrains Mono', monospace)" font-size="7.5" letter-spacing="2" fill="currentColor" opacity="0.3" text-anchor="middle">
              <textPath href="#vlo-arc-bot" startOffset="50%">v{{ outcome.riskLevel ?? '1' }}.0</textPath>
            </text>
          </svg>
        </div>

        <!-- Law title -->
        <h3 class="es-h3" style="font-size: 27px; text-align: center; margin-top: 22px; margin-bottom: 14px; color: var(--gold)">
          {{ outcome.title }}
        </h3>

        <!-- Domain chips -->
        <div class="es-row" style="gap: 8px; justify-content: center; margin-bottom: 20px">
          <ChipBadge :tone="domainTone(outcome.primaryEventType)">
            {{ outcome.primaryEventType ?? 'Loi' }}
          </ChipBadge>
          <ChipBadge v-if="outcome.rewardProfile" tone="gold">
            {{ outcome.rewardProfile }}
          </ChipBadge>
        </div>

        <!-- Info block -->
        <div class="es-panel vlo-info">
          <div class="vlo-info__row">
            <span class="es-label" style="color: var(--ink-4)">Portée</span>
            <span class="es-mono" style="font-size: 12px; color: var(--ink-2)">{{ outcome.resolutionKind }}</span>
          </div>
          <div class="vlo-info__row">
            <span class="es-label" style="color: var(--ink-4)">Risque</span>
            <span class="es-mono" style="font-size: 12px; color: var(--gold)">{{ outcome.riskLevel ?? '—' }}</span>
          </div>
          <div class="vlo-info__row">
            <span class="es-label" style="color: var(--ink-4)">Registre</span>
            <span class="es-mono" style="font-size: 12px" :style="{ color: sealed ? 'var(--gold)' : 'var(--ink-4)' }">
              {{ sealed ? 'Inscrite' : 'En attente' }}
            </span>
          </div>
        </div>

        <div style="flex: 1" />

        <!-- Elise comment -->
        <EliseComment tell="grave">
          Une loi de plus pèse sur le Palais. Celle-ci, tu l'as voulue — souviens-t'en quand elle te frappera.
        </EliseComment>
      </div>

      <!-- ── RIGHT COLUMN ── -->
      <div class="vlo-right">

        <!-- Header row -->
        <div class="es-row" style="align-items: center; justify-content: space-between; margin-bottom: 24px; flex: 0 0 auto">
          <div class="es-row" style="align-items: center; gap: 10px">
            <SigilIcon kind="loi" :size="18" :stroke-width="1.4" style="color: var(--gold)" />
            <span class="es-kicker" style="color: var(--gold-dim)">Édit du Palais · loi proposée</span>
          </div>
          <span class="es-label" style="color: var(--ink-4)">PalaceLawOffered</span>
        </div>

        <!-- Plate content -->
        <div class="es-plate es-plate--inset vlo-plate">
          <!-- Corner decorations -->
          <span class="es-corner tl" />
          <span class="es-corner tr" />
          <span class="es-corner bl" />
          <span class="es-corner br" />

          <!-- Title -->
          <h1 class="es-h2" style="font-size: 40px; margin-bottom: 16px; color: var(--ink)">
            {{ outcome.title }}
          </h1>

          <!-- Description as italic quote -->
          <p
            v-if="outcome.description"
            style="font-family: var(--display); font-style: italic; font-size: 22px; line-height: 1.5; color: var(--ink-2); margin-bottom: 24px"
          >
            {{ outcome.description }}
          </p>

          <!-- Decorative rule -->
          <RuleOrnament :frost="true" style="margin: 8px 0 24px" />

          <!-- Explicit law detail block -->
          <section class="vlo-law-detail" aria-labelledby="law-detail-title">
            <span class="es-kicker vlo-law-detail__kicker">Loi proposée</span>
            <h2 id="law-detail-title" class="vlo-law-detail__title">{{ outcome.title }}</h2>
            <p v-if="outcome.description" class="vlo-law-detail__description">
              {{ outcome.description }}
            </p>
            <p class="vlo-law-detail__note">
              Cette Loi ne peut pas être refusée. Elle sera appliquée par le serveur après apposition du sceau.
            </p>
          </section>

          <p v-if="missingLawApplicationChoice" class="vlo-choice-empty">
            Aucune instruction d'application de loi n'a été fournie par le serveur.
          </p>

          <!-- Narrative fragments accordion -->
          <div
            v-if="outcome.narrativeFragments && outcome.narrativeFragments.length"
            class="vlo-fragments"
          >
            <div
              v-for="(frag, i) in outcome.narrativeFragments"
              :key="'frag-' + i"
              class="vlo-frag"
            >
              <button
                class="vlo-frag__hd"
                :aria-expanded="openFragments.has(i)"
                @click="toggleFragment(i)"
              >
                <svg
                  class="vlo-chevron"
                  :class="{ 'vlo-chevron--open': openFragments.has(i) }"
                  width="11" height="11" viewBox="0 0 11 11" fill="none"
                  stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
                >
                  <path d="M3.5 2L7.5 5.5L3.5 9" />
                </svg>
                <span class="es-label">{{ frag.speaker }}</span>
              </button>
              <div
                class="vlo-frag__body"
                :class="{ 'vlo-frag__body--open': openFragments.has(i) }"
              >
                <p class="es-body" style="font-size: 13.5px; line-height: 1.6; padding: 6px 0 14px 22px; margin: 0; color: var(--ink-3)">
                  {{ frag.text }}
                </p>
              </div>
            </div>
          </div>
        </div>

        <!-- Footer -->
        <div class="es-row" style="justify-content: space-between; flex: 0 0 auto; margin-top: 16px; align-items: center">
          <!-- Laws drawer trigger -->
          <button
            class="es-btn es-btn--ghost"
            @click="showDrawer = !showDrawer"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M12 3L4 7v5c0 5 3.5 9.74 8 11 4.5-1.26 8-6 8-11V7l-8-4z" />
            </svg>
            Lois en vigueur
          </button>

          <!-- Seal / proceed -->
          <button
            v-if="!sealed"
            class="es-btn es-btn--gold es-btn--lg"
            :disabled="!canSealDecision"
            @click="applySeal"
          >
            {{ sealButtonText }}
          </button>
          <button
            v-else
            class="es-btn es-btn--frost es-btn--lg"
            :disabled="isLoading"
            @click="proceed"
          >
            ✓ Loi scellée · Poursuivre →
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Root ── */
.vlo-root {
  position: relative;
  width: 100%;
  height: 100%;
  overflow: hidden;
  background:
    radial-gradient(60% 55% at 15% 20%, var(--wash-gold), transparent 60%),
    radial-gradient(55% 50% at 85% 75%, oklch(0.5 0.07 84 / 0.07), transparent 60%),
    radial-gradient(150% 130% at 50% -10%, oklch(0.28 0.048 268) 0%, var(--bg) 50%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

/* ── Two-column layout ── */
.vlo-layout {
  position: relative;
  z-index: 5;
  display: flex;
  flex-direction: row;
  height: 100%;
}

/* ── Left column ── */
.vlo-left {
  flex: 0 0 462px;
  border-right: 1px solid var(--line);
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 34px 30px;
}

/* ── Seal circle ── */
.vlo-seal {
  position: relative;
  width: 218px;
  height: 218px;
  border-radius: 50%;
  background: radial-gradient(circle at 40% 35%, oklch(0.72 0.12 84 / 0.18), oklch(0.55 0.08 84 / 0.08) 55%, transparent 80%);
  border: 1px solid var(--gold-dim);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: border-color 0.4s, box-shadow 0.4s;
}

.vlo-seal--done {
  border-color: var(--gold);
  box-shadow: 0 0 48px -12px oklch(0.72 0.12 84 / 0.5);
}

/* Inner dashed ring */
.vlo-seal__ring {
  position: absolute;
  inset: 12px;
  border-radius: 50%;
  border: 1px dashed var(--gold-dim);
  pointer-events: none;
}

.vlo-seal--done .vlo-seal__ring {
  border-color: oklch(0.72 0.12 84 / 0.5);
}

/* Sigil container (centered) */
.vlo-seal__sigil {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: opacity 0.4s;
}

/* Stamp ring animation */
.vlo-stamp {
  position: absolute;
  inset: -4px;
  border-radius: 50%;
  border: 3px solid var(--gold);
  opacity: 0;
  transform: scale(0.6);
  pointer-events: none;
}

.vlo-stamp--go {
  animation: vloStamp 0.46s cubic-bezier(0.22, 0.8, 0.4, 1) forwards;
}

@keyframes vloStamp {
  0%   { opacity: 0; transform: scale(0.6); }
  50%  { opacity: 1; transform: scale(1.05); }
  100% { opacity: 0; transform: scale(1.18); }
}

/* Gold checkmark badge (bottom right of seal) */
.vlo-check {
  position: absolute;
  bottom: 10px;
  right: 10px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--gold);
  color: var(--void);
  display: flex;
  align-items: center;
  justify-content: center;
  transform: scale(0);
  transition: transform 0.32s cubic-bezier(0.34, 1.56, 0.64, 1);
  pointer-events: none;
}

.vlo-check--show {
  transform: scale(1);
}

/* SVG arc text overlay */
.vlo-art {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  color: var(--gold);
}

/* ── Info panel ── */
.vlo-info {
  width: 100%;
  padding: 14px 18px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 20px;
}

.vlo-info__row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

/* ── Right column ── */
.vlo-right {
  flex: 1;
  padding: 28px 46px;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

/* ── Plate ── */
.vlo-plate {
  flex: 1;
  overflow-y: auto;
  padding: 30px 34px;
  position: relative;
}

/* ── Narrative fragments ── */
.vlo-law-detail {
  margin-bottom: 22px;
  padding: 18px 20px;
  border: 1px solid oklch(0.72 0.12 84 / 0.28);
  border-radius: 18px;
  background:
    linear-gradient(135deg, oklch(0.72 0.12 84 / 0.09), transparent 52%),
    oklch(0.18 0.035 265 / 0.42);
}

.vlo-law-detail__kicker {
  display: block;
  color: var(--gold-dim);
  margin-bottom: 9px;
}

.vlo-law-detail__title {
  margin: 0 0 10px;
  font-family: var(--display);
  font-size: 24px;
  color: var(--gold);
}

.vlo-law-detail__description {
  margin: 0;
  color: var(--ink-2);
  font-size: 15px;
  line-height: 1.65;
}

.vlo-law-detail__note {
  margin: 14px 0 0;
  color: var(--ink-4);
  font-size: 12px;
  line-height: 1.5;
}

.vlo-choice-empty {
  margin: 0 0 24px;
  color: var(--ink-4);
  font-size: 13px;
}

.vlo-fragments {
  display: flex;
  flex-direction: column;
  gap: 0;
  margin-bottom: 20px;
}

.vlo-frag {
  border-bottom: 1px solid var(--line-soft);
}

.vlo-frag__hd {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 12px 0;
  background: none;
  border: none;
  cursor: pointer;
  color: var(--ink-2);
  text-align: left;
  transition: color 0.16s;
}

.vlo-frag__hd:hover {
  color: var(--ink);
}

.vlo-chevron {
  flex-shrink: 0;
  color: var(--ink-4);
  transition: transform 0.22s ease;
}

.vlo-chevron--open {
  transform: rotate(90deg);
  color: var(--gold);
}

.vlo-frag__body {
  max-height: 0;
  overflow: hidden;
  opacity: 0;
  transition: max-height 0.28s ease, opacity 0.22s ease;
}

.vlo-frag__body--open {
  max-height: 400px;
  opacity: 1;
}

/* ── Réécriture / choices ── */
.vlo-rewrite {
  margin-top: 20px;
}

.vlo-choice {
  padding: 14px 0;
  border-bottom: 1px solid var(--line-soft);
}

.vlo-choice:last-child {
  border-bottom: none;
}
</style>
