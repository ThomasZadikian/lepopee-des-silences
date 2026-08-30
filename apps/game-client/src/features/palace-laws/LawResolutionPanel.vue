<script setup lang="ts">
import SealGlyph from '../../shared/components/SealGlyph.vue'
import { computed, ref } from 'vue'
import type { EventOutcomeDto } from '../events/types/eventTypes'
import { getOutcomeChoices, isChoiceOutcome } from '../events/types/eventTypes'
import type { ActivePalaceLawDto } from '../runs/types/runTypes'

// ── Props & Emits ─────────────────────────────────────────────────────────
const props = defineProps<{
  outcome: EventOutcomeDto
  isLoading: boolean
  activeLaws?: ActivePalaceLawDto[] | null
}>()

const emit = defineEmits<{
  continue: []
  selectChoice: [choiceId: string]
}>()

// ── State ─────────────────────────────────────────────────────────────────
const sealed = ref(false)
const stamping = ref(false)

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
const sealState = computed<'idle' | 'confirming' | 'confirmed'>(() =>
  sealed.value ? 'confirmed' : stamping.value ? 'confirming' : 'idle',
)

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
    <span class="vlo-kicker">{{ sealed ? 'Promulguée · scellée au Tome' : 'Édit du Palais · loi proposée' }}</span>

    <SealGlyph
      kind="loi"
      tone="mint"
      :size="150"
      :sigil-size="60"
      top-text="Édit du Palais"
      :bottom-text="`v${outcome.riskLevel ?? '1'}.0`"
      :state="sealState"
    />

    <h1 class="vlo-title">{{ outcome.title }}</h1>

    <p v-if="outcome.description" class="vlo-desc">{{ outcome.description }}</p>

    <section class="vlo-law-detail" aria-labelledby="law-detail-title">
      <span class="vlo-law-detail__kicker">Loi proposée</span>
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

    <div v-if="outcome.narrativeFragments?.length" class="vlo-fragments">
      <p
        v-for="(frag, i) in outcome.narrativeFragments"
        :key="'frag-' + i"
        class="vlo-fragment"
      >
        <span class="vlo-fragment__speaker">{{ frag.speaker }}</span>
        {{ frag.text }}
      </p>
    </div>

    <footer class="vlo-footer">
      <button
        v-if="!sealed"
        class="vlo-seal-btn"
        :disabled="!canSealDecision"
        @click="applySeal"
      >
        {{ sealButtonText }}
      </button>
      <button
        v-else
        class="vlo-proceed-btn"
        :disabled="isLoading"
        @click="proceed"
      >
        ✓ Loi scellée · Poursuivre →
      </button>
    </footer>
  </div>
</template>

<style scoped>
.vlo-root {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 12px;
  padding: 40px 36px;
  background: var(--panel);
  border: 1px solid var(--line);
  color: var(--ink);
  font-family: var(--font);
}

.vlo-kicker {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--mint-dim);
  margin-bottom: 8px;
}

.vlo-title {
  margin: 12px 0 4px;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 30px;
  color: var(--ink);
}

.vlo-desc {
  margin: 0 0 8px;
  font-style: italic;
  font-size: 15px;
  line-height: 1.55;
  color: var(--ink-3);
  max-width: 48ch;
}

.vlo-law-detail {
  width: 100%;
  margin-top: 12px;
  padding: 16px 18px;
  border: 1px solid var(--line-soft);
  background: var(--panel-2);
  text-align: left;
}

.vlo-law-detail__kicker {
  display: block;
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin-bottom: 8px;
}

.vlo-law-detail__title {
  margin: 0 0 8px;
  font-family: var(--font-display);
  font-style: italic;
  font-size: 19px;
  color: var(--ink);
}

.vlo-law-detail__description {
  margin: 0;
  color: var(--ink-3);
  font-size: 13.5px;
  line-height: 1.6;
}

.vlo-law-detail__note {
  margin: 12px 0 0;
  color: var(--ink-4);
  font-size: 11.5px;
  line-height: 1.5;
}

.vlo-choice-empty {
  margin: 0;
  color: var(--ink-4);
  font-size: 12px;
}

.vlo-fragments {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 8px;
  text-align: left;
}

.vlo-fragment {
  margin: 0;
  padding-left: 12px;
  border-left: 1px solid var(--line-soft);
  font-size: 12.5px;
  line-height: 1.55;
  color: var(--ink-3);
  font-style: italic;
}

.vlo-fragment__speaker {
  display: block;
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  font-style: normal;
  color: var(--ink-4);
  margin-bottom: 2px;
}

.vlo-footer {
  margin-top: 12px;
}

.vlo-seal-btn,
.vlo-proceed-btn {
  padding: 11px 26px;
  background: transparent;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s;
}

.vlo-seal-btn:hover:not(:disabled),
.vlo-proceed-btn:hover:not(:disabled) { opacity: .8; }

.vlo-seal-btn:disabled {
  color: var(--ink-5);
  border-color: var(--line);
  cursor: not-allowed;
}
</style>
