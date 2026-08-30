<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import SigilIcon from '@/shared/components/SigilIcon.vue'
import { computed, ref } from 'vue'
import type { EventOutcomeDto } from '../types/eventTypes'
import { getOutcomeChoices } from '../types/eventTypes'

// ── Props & Emits ─────────────────────────────────────────────────────────
const props = defineProps<{
  outcome: EventOutcomeDto
  isLoading: boolean
}>()

const emit = defineEmits<{
  continue: []
  selectChoice: [choiceId: string]
}>()

// ── State ─────────────────────────────────────────────────────────────────
const selectedId = ref<string | null>(null)

// ── Computed ──────────────────────────────────────────────────────────────
const choices = computed(() => getOutcomeChoices(props.outcome))
const hasChoices = computed(() => choices.value.length > 0)

// ── Actions ───────────────────────────────────────────────────────────────
function selectItem(id: string) {
  selectedId.value = id
}

function confirm() {
  if (selectedId.value) {
    emit('selectChoice', selectedId.value)
  } else {
    emit('continue')
  }
}

function decline() {
  emit('continue')
}
</script>

<template>
  <div class="mrc-root">
    <div class="mrc-content">

      <!-- ── Header ── -->
      <div class="mrc-header">
        <div class="mrc-header__sigil">
          <SigilIcon kind="marchand" :size="32" :stroke-width="1.2" style="color: var(--mint-dim)" />
        </div>
        <div class="mrc-header__text">
          <span class="es-kicker" style="color: var(--mint-dim); display: block; margin-bottom: 6px">Marchand du Palais</span>
          <h2 class="es-h2" style="font-size: 30px; color: var(--ink)">
            {{ outcome.title ?? 'Le Marchand vous attend' }}
          </h2>
        </div>
        <ChipBadge tone="mint">TradeOffered</ChipBadge>
      </div>

      <!-- Description -->
      <p
        v-if="outcome.description"
        class="es-body"
        style="color: var(--ink-3); font-style: italic; max-width: 640px; margin: 0 0 4px"
      >
        {{ outcome.description }}
      </p>

      <!-- Separator -->
      <div class="mrc-separator">
        <span class="mrc-separator__label">Que souhaitez-vous acquérir ?</span>
      </div>

      <!-- ── Items grid or empty state ── -->
      <div v-if="hasChoices" class="mrc-grid">
        <button
          v-for="choice in choices"
          :key="choice.id"
          type="button"
          :class="['mrc-item', selectedId === choice.id && 'mrc-item--sel', !choice.isEnabled && 'mrc-item--disabled']"
          :disabled="!choice.isEnabled"
          @click="selectItem(choice.id)"
        >
          <div class="mrc-item__top">
            <span class="mrc-item__name">{{ choice.label }}</span>
            <span v-if="selectedId === choice.id" class="mrc-item__pick">✓</span>
          </div>
          <p v-if="choice.description" class="mrc-item__desc">{{ choice.description }}</p>
          <span v-if="!choice.isEnabled" class="mrc-item__unavail">Indisponible</span>
        </button>
      </div>

      <!-- Empty state -->
      <div v-else class="mrc-empty">
        <SigilIcon kind="marchand" :size="42" :stroke-width="0.9" style="color: var(--ink-4); margin-bottom: 14px" />
        <p class="es-body" style="color: var(--ink-4); text-align: center">Le marchand n'a rien à proposer.</p>
      </div>

      <!-- ── Footer ── -->
      <div class="mrc-footer">
        <button
          class="es-btn es-btn--ghost es-btn--lg"
          :disabled="isLoading"
          @click="decline"
        >
          Passer
        </button>
        <button
          class="es-btn es-btn--mint es-btn--lg"
          :disabled="!selectedId || isLoading"
          :style="{ opacity: !selectedId ? 0.4 : 1, minWidth: '180px' }"
          @click="confirm"
        >
          Acquérir →
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ── Root ── */
.mrc-root {
  position: relative;
  width: 100%;
  background: var(--panel);
  border: 1px solid var(--line);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

/* ── Content layout ── */
.mrc-content {
  position: relative;
  display: flex;
  flex-direction: column;
  padding: 32px 40px;
  gap: 16px;
}

/* ── Header ── */
.mrc-header {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 18px;
  flex: 0 0 auto;
}

.mrc-header__sigil {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: var(--panel-2);
  border: 1px solid var(--mint-dim);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.mrc-header__text {
  flex: 1;
}

/* ── Separator ── */
.mrc-separator {
  padding: 10px 0;
  border-top: 1px solid var(--line-soft);
  border-bottom: 1px solid var(--line-soft);
  flex: 0 0 auto;
}

.mrc-separator__label {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

/* ── Items grid — cartes compactes, un popup pas un écran ── */
.mrc-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 200px));
  gap: 12px;
  justify-content: center;
  align-content: start;
  max-height: 52vh;
  overflow-y: auto;
}

/* ── Single item card ── */
.mrc-item {
  all: unset;
  box-sizing: border-box;
  border: 1px solid var(--line);
  background: var(--panel-2);
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  cursor: pointer;
  transition: border-color 0.2s;
}

.mrc-item:hover:not(.mrc-item--disabled) { border-color: var(--mint-dim); }
.mrc-item--sel { border-color: var(--mint-dim); background: var(--panel); }
.mrc-item--disabled { opacity: 0.4; cursor: not-allowed; }

.mrc-item__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.mrc-item__name {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 15px;
  color: var(--ink);
}

.mrc-item__pick { color: var(--mint-dim); font-size: 13px; }

.mrc-item__desc {
  margin: 0;
  font-size: 11.5px;
  line-height: 1.4;
  color: var(--ink-3);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.mrc-item__unavail {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--ink-5);
}

/* ── Empty state ── */
.mrc-empty {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

/* ── Footer ── */
.mrc-footer {
  flex: 0 0 auto;
  display: flex;
  justify-content: center;
  gap: 12px;
  padding-top: 12px;
  border-top: 1px solid var(--line-soft);
}
</style>
