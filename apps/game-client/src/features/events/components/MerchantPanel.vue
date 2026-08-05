<script setup lang="ts">
import ChipBadge from '@/shared/components/ChipBadge.vue'
import EliseComment from '@/shared/components/EliseComment.vue'
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
        <div
          v-for="choice in choices"
          :key="choice.id"
          :class="['mrc-item', selectedId === choice.id && 'mrc-item--sel', !choice.isEnabled && 'mrc-item--disabled']"
          @click="choice.isEnabled && selectItem(choice.id)"
        >
          <!-- Selection indicator -->
          <div
            class="mrc-item__pick"
            :style="selectedId === choice.id
              ? { borderColor: 'var(--mint-dim)', color: 'var(--mint-dim)' }
              : {}"
          >
            <svg
              v-if="selectedId === choice.id"
              width="12" height="12" viewBox="0 0 12 12" fill="none"
              stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"
            >
              <path d="M2 6l3 3 5-5" />
            </svg>
          </div>

          <!-- Icon circle -->
          <div class="mrc-item__icon" :style="{ borderColor: selectedId === choice.id ? 'var(--mint-dim)' : 'var(--line)' }">
            <SigilIcon
              kind="objet"
              :size="34"
              :stroke-width="1.3"
              :style="{ color: selectedId === choice.id ? 'var(--mint-dim)' : 'var(--ink-3)' }"
            />
          </div>

          <!-- Text -->
          <h3
            class="es-h3"
            style="font-size: 17px; text-align: center; margin-bottom: 8px"
            :style="{ color: selectedId === choice.id ? 'var(--ink)' : 'var(--ink-2)' }"
          >
            {{ choice.label }}
          </h3>

          <p
            v-if="choice.description"
            class="es-body"
            style="font-size: 12.5px; text-align: center; color: var(--ink-4); flex: 1; margin: 0"
          >
            {{ choice.description }}
          </p>

          <div v-if="!choice.isEnabled" class="mrc-item__unavail">
            <span class="es-label" style="color: var(--ink-4); font-size: 9px">Indisponible</span>
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-else class="mrc-empty">
        <SigilIcon kind="marchand" :size="42" :stroke-width="0.9" style="color: var(--ink-4); margin-bottom: 14px" />
        <p class="es-body" style="color: var(--ink-4); text-align: center">Le marchand n'a rien à proposer.</p>
      </div>

      <!-- ── Footer ── -->
      <div class="mrc-footer">
        <EliseComment
          tell="à mi-voix"
          style="flex: 1; max-width: 340px"
        >
          Le Palais vend ce dont il n'a plus besoin. Méfie-toi des bonnes affaires.
        </EliseComment>

        <div class="es-row" style="gap: 12px">
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

/* ── Items grid ── */
.mrc-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 18px;
  align-content: start;
  max-height: 52vh;
  overflow-y: auto;
}

/* ── Single item card ── */
.mrc-item {
  position: relative;
  border: 1px solid var(--line);
  background: var(--panel-2);
  padding: 22px 18px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  cursor: pointer;
  transition: border-color 0.2s, box-shadow 0.2s, transform 0.2s;
  user-select: none;
}

.mrc-item:hover {
  border-color: var(--mint-dim);
}

.mrc-item--sel {
  border-color: var(--mint-dim);
  background: var(--panel);
}

.mrc-item--disabled {
  opacity: 0.38;
  cursor: not-allowed;
  pointer-events: none;
}

/* ── Selection indicator ── */
.mrc-item__pick {
  position: absolute;
  top: 12px;
  right: 12px;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 1px solid var(--line);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

/* ── Icon circle ── */
.mrc-item__icon {
  width: 74px;
  height: 74px;
  border-radius: 50%;
  border: 1px solid var(--line);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: border-color 0.2s;
}

/* ── Unavailable badge ── */
.mrc-item__unavail {
  padding: 3px 9px;
  border-radius: 3px;
  background: var(--line-soft);
  margin-top: 4px;
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
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  padding-top: 12px;
  border-top: 1px solid var(--line-soft);
}
</style>
