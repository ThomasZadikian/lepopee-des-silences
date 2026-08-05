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

const eliseQuote = computed(() =>
  props.outcome.narrativeFragments?.[0]?.text ?? props.outcome.description ?? ''
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
  <div class="eo-root">
    <div class="eo-top-row">
      <span class="eo-chip">{{ outcomeFamily }}</span>
      <span class="eo-kind">{{ outcome.resolutionKind }}</span>
    </div>

    <h2 class="eo-title">{{ outcome.title }}</h2>

    <p v-if="eliseQuote" class="eo-quote">{{ eliseQuote }}</p>

    <div v-if="bodyFragments.length" class="eo-fragments">
      <article
        v-for="(frag, i) in bodyFragments"
        :key="i"
        class="eo-fragment"
      >
        <span class="eo-fragment__speaker">{{ frag.speaker }}</span>
        <p>{{ frag.text }}</p>
      </article>
    </div>

    <footer class="eo-footer">
      <button
        v-if="requiresChoice"
        class="eo-btn"
        :disabled="isLoading"
        @click="drawerOpen = true"
      >
        Que fais-tu ? ↓
      </button>
      <button
        v-else
        class="eo-btn"
        :disabled="isLoading"
        @click="$emit('continue')"
      >
        {{ isLoading ? 'Résolution…' : 'Continuer →' }}
      </button>
    </footer>

    <!-- ── Choice drawer ── -->
    <Transition name="eo-drawer">
      <div v-if="drawerOpen && requiresChoice" class="eo-drawer">
        <div class="eo-drawer__header">
          <span class="eo-drawer__title">Que fais-tu ?</span>
          <button class="eo-drawer__close" @click="drawerOpen = false" aria-label="Fermer">✕</button>
        </div>

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
            <button
              class="eo-choice__hd"
              :disabled="!choice.isEnabled"
              @click="toggleChoice(choice.id)"
            >
              <span class="eo-choice__title">{{ choice.label }}</span>
              <span v-if="selectedChoiceId === choice.id" class="eo-choice__sel">Sélectionné</span>
            </button>

            <div
              class="eo-choice__body"
              :class="{ 'eo-choice__body--open': openChoiceIds.has(choice.id) }"
            >
              <p class="eo-choice__desc">{{ choice.description }}</p>
              <button class="eo-btn eo-btn--ghost" @click="selectAndClose(choice.id)">
                Sélectionner ce geste
              </button>
            </div>
          </div>
        </div>

        <div class="eo-drawer__footer">
          <button class="eo-btn eo-btn--ghost" @click="drawerOpen = false">← Revenir</button>
          <button
            class="eo-btn"
            :disabled="isLoading || !selectedChoiceId"
            @click="confirmChoice"
          >
            {{ isLoading ? 'Résolution…' : 'Valider ce choix →' }}
          </button>
        </div>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.eo-root {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 36px 40px;
  background: var(--panel);
  border: 1px solid var(--line);
  color: var(--ink);
  font-family: var(--font);
}

.eo-top-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.eo-chip {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: .08em;
  text-transform: uppercase;
  padding: 2px 7px;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
}

.eo-kind {
  font-family: var(--font-mono);
  font-size: 10px;
  color: var(--ink-4);
}

.eo-title {
  margin: 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 30px;
  line-height: 1.1;
  color: var(--ink);
}

.eo-quote {
  margin: 0;
  font-style: italic;
  font-size: 15px;
  line-height: 1.6;
  color: var(--ink-2);
}

.eo-fragments {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.eo-fragment {
  padding: 10px 12px;
  background: var(--panel-2);
  border: 1px solid var(--line-soft);
}

.eo-fragment__speaker {
  display: block;
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: .1em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin-bottom: 4px;
}

.eo-fragment p {
  margin: 0;
  font-size: 13px;
  color: var(--ink-3);
  line-height: 1.5;
}

.eo-footer {
  display: flex;
  justify-content: center;
  padding-top: 10px;
  border-top: 1px solid var(--line-soft);
}

.eo-btn {
  padding: 10px 22px;
  background: transparent;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: .08em;
  text-transform: uppercase;
  cursor: pointer;
  transition: opacity .15s;
}

.eo-btn:hover:not(:disabled) { opacity: .8; }
.eo-btn:disabled { color: var(--ink-5); border-color: var(--line); cursor: not-allowed; }

.eo-btn--ghost {
  border-color: var(--line-soft);
  color: var(--ink-3);
}

/* ── Drawer ── */
.eo-drawer {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-top: 4px;
  padding-top: 14px;
  border-top: 1px solid var(--line-soft);
}

.eo-drawer__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 8px;
}

.eo-drawer__title {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: .06em;
  text-transform: uppercase;
  color: var(--ink-3);
}

.eo-drawer__close {
  all: unset;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 12px;
  padding: 4px;
  transition: color .15s;
}
.eo-drawer__close:hover { color: var(--mint-dim); }

.eo-drawer__choices {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.eo-choice {
  border: 1px solid var(--line-soft);
  transition: border-color .18s;
}

.eo-choice--selected { border-color: var(--mint-dim); }
.eo-choice--disabled { opacity: 0.4; pointer-events: none; }

.eo-choice__hd {
  all: unset;
  width: 100%;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 10px 14px;
  cursor: pointer;
  color: var(--ink-2);
  transition: color .15s;
}
.eo-choice__hd:hover { color: var(--ink); }

.eo-choice__title { font-size: 13px; }

.eo-choice__sel {
  flex-shrink: 0;
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: .06em;
  text-transform: uppercase;
  color: var(--mint-dim);
}

.eo-choice__body {
  max-height: 0;
  overflow: hidden;
  opacity: 0;
  transition: max-height .28s ease, opacity .22s ease;
}
.eo-choice__body--open { max-height: 200px; opacity: 1; }

.eo-choice__desc {
  padding: 0 14px 10px 14px;
  margin: 0;
  font-size: 12.5px;
  color: var(--ink-3);
}

.eo-choice__body .eo-btn {
  margin: 0 14px 12px;
  padding: 6px 14px;
  font-size: 10px;
}

.eo-drawer__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-top: 10px;
}

.eo-drawer-enter-active,
.eo-drawer-leave-active {
  transition: opacity .2s ease;
}
.eo-drawer-enter-from,
.eo-drawer-leave-to {
  opacity: 0;
}
</style>
