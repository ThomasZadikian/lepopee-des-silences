<script setup lang="ts">
import { computed, ref } from 'vue';

import ConfirmDialog from '../../shared/components/ConfirmDialog.vue';
import {
  nodeTypeGlyph,
  positionSlotToGridArea,
  type InterludeDto,
  type InterludeNodeDto,
} from './interludeTypes';

const props = defineProps<{
  interlude: InterludeDto;
  isLoading: boolean;
  isSavingAndExiting?: boolean;
  isAbandoningRun?: boolean;
  runActionError?: string | null;
}>();

const emit = defineEmits<{
  enterNextRoom: [];
  saveAndExit: [];
  abandon: [];
}>();

const activeNodeId = ref<string | null>(null);
const showAbandonDialog = ref(false);

const activeNode = computed(() => {
  if (!activeNodeId.value) return null;
  return props.interlude.nodes.find((n) => n.id === activeNodeId.value) ?? null;
});

const saveAndExitAction = computed(() =>
  props.interlude.availableActions.find((a) => a.key === 'save-and-exit'),
);

const abandonAction = computed(() =>
  props.interlude.availableActions.find((a) => a.key === 'abandon-run'),
);

function onAbandonConfirm() {
  showAbandonDialog.value = false;
  emit('abandon');
}

function selectNode(node: InterludeNodeDto) {
  if (!node.isEnabled) return;
  activeNodeId.value = activeNodeId.value === node.id ? null : node.id;
}

function getNodeLabel(node: InterludeNodeDto): string {
  const labels: Record<string, string> = {
    Player:      'Aventurier',
    Elise:       'Elise',
    Inventory:   'Sac à dos',
    Journal:     'Journal',
    Placeholder: 'À venir',
  };
  return labels[node.type] ?? node.label;
}

function isComingSoon(node: InterludeNodeDto): boolean {
  return node.type === 'Placeholder' || !node.isEnabled;
}
</script>

<template>
  <section class="interlude">
    <header class="interlude__header">
      <p class="system-label">Repli du Palais · Salle {{ interlude.displayRoomNumber }}</p>
      <h2 class="interlude__title">Repli du Palais</h2>
    </header>

    <div class="interlude__hub">
      <div
        v-for="node in interlude.nodes"
        :key="node.id"
        class="interlude__node"
        :class="[
          `interlude__node--slot-${positionSlotToGridArea(node.positionSlot)}`,
          {
            'interlude__node--disabled': !node.isEnabled,
            'interlude__node--active': activeNodeId === node.id,
          },
        ]"
        :title="node.description"
        @click="selectNode(node)"
      >
        <span class="interlude__node-glyph">{{ nodeTypeGlyph(node.type) }}</span>
        <span class="interlude__node-label">{{ getNodeLabel(node) }}</span>
      </div>
    </div>

    <Transition name="fade">
      <div
        v-if="activeNode"
        class="interlude__node-detail panel"
      >
        <p class="system-label">{{ getNodeLabel(activeNode) }}</p>

        <p class="interlude__node-desc">
          <template v-if="isComingSoon(activeNode)">
            Fonctionnalité à venir dans une prochaine version du Palais.
          </template>
          <template v-else>
            {{ activeNode.description }}
          </template>
        </p>
      </div>
    </Transition>

    <footer class="interlude__actions">
      <div class="interlude__secondary-actions">
        <button
          v-if="saveAndExitAction?.isEnabled"
          class="ghost-button interlude__action-secondary"
          :disabled="isLoading || isSavingAndExiting || isAbandoningRun"
          :title="saveAndExitAction.description"
          @click="$emit('saveAndExit')"
        >
          <span v-if="isSavingAndExiting">Sauvegarde…</span>
          <span v-else>{{ saveAndExitAction.label }}</span>
        </button>

        <button
          v-if="abandonAction?.isEnabled"
          class="ghost-button interlude__action-abandon"
          :disabled="isLoading || isSavingAndExiting || isAbandoningRun"
          :title="abandonAction.description"
          @click="showAbandonDialog = true"
        >
          <span v-if="isAbandoningRun">Abandon…</span>
          <span v-else>{{ abandonAction.label }}</span>
        </button>
      </div>

      <p v-if="runActionError" class="interlude__action-error">{{ runActionError }}</p>

      <button
        class="ghost-button interlude__cta"
        :disabled="isLoading || isSavingAndExiting || isAbandoningRun"
        @click="$emit('enterNextRoom')"
      >
        <span v-if="isLoading">Préparation en cours…</span>
        <span v-else>Entrer dans la prochaine Room</span>
      </button>
    </footer>

    <ConfirmDialog
      v-if="abandonAction"
      v-model="showAbandonDialog"
      title="Abandonner la run ?"
      :message="abandonAction.description"
      confirm-label="Abandonner définitivement"
      cancel-label="Annuler"
      :danger="true"
      :loading="isAbandoningRun"
      @confirm="onAbandonConfirm"
    />
  </section>
</template>

<style scoped>
.interlude {
  display: grid;
  grid-template-rows: auto 1fr auto auto;
  gap: var(--space-6);
  height: 100%;
}

/* Header */
.interlude__header {
  display: grid;
  gap: var(--space-1);
}

.interlude__title {
  margin: 0;
  color: var(--frost);
  font-size: clamp(1.5rem, 3vw, 2.4rem);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

/* Hub grid */
.interlude__hub {
  display: grid;
  grid-template-areas:
    '.    top   .'
    'left center right'
    'bl   .     br';
  grid-template-columns: 1fr 1.6fr 1fr;
  grid-template-rows: 1fr 2fr 1fr;
  gap: var(--space-4);
  max-width: 48rem;
  width: 100%;
  margin: 0 auto;
}

/* Slot placement */
.interlude__node--slot-center { grid-area: center; }
.interlude__node--slot-top    { grid-area: top; }
.interlude__node--slot-left   { grid-area: left; }
.interlude__node--slot-right  { grid-area: right; }
.interlude__node--slot-bl     { grid-area: bl; }
.interlude__node--slot-br     { grid-area: br; }

/* Node card */
.interlude__node {
  display: grid;
  place-items: center;
  gap: var(--space-2);
  padding: var(--space-4);
  background: color-mix(in oklch, var(--panel), transparent 10%);
  border: 1px solid var(--line);
  cursor: pointer;
  user-select: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.interlude__node:hover:not(.interlude__node--disabled) {
  border-color: var(--frost);
  box-shadow: 0 0 20px color-mix(in oklch, var(--frost), transparent 82%);
}

.interlude__node--active {
  border-color: var(--frost);
  box-shadow: 0 0 28px color-mix(in oklch, var(--frost), transparent 75%);
}

.interlude__node--disabled {
  opacity: 0.4;
  cursor: default;
  border-style: dashed;
}

.interlude__node-glyph {
  font-size: 1.8rem;
  color: var(--frost);
  line-height: 1;
}

.interlude__node--slot-center .interlude__node-glyph {
  font-size: 2.4rem;
}

.interlude__node--disabled .interlude__node-glyph {
  color: var(--ink-4);
}

.interlude__node-label {
  font-size: 0.75rem;
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--ink-3);
}

.interlude__node--active .interlude__node-label {
  color: var(--frost);
}

/* Node detail */
.interlude__node-detail {
  padding: var(--space-4);
  max-width: 36rem;
}

.interlude__node-desc {
  color: var(--ink-3);
  line-height: 1.6;
  margin: var(--space-2) 0 0;
}

/* Actions */
.interlude__actions {
  display: grid;
  gap: var(--space-3);
  padding-top: var(--space-4);
  border-top: 1px solid var(--line);
}

.interlude__secondary-actions {
  display: flex;
  gap: var(--space-3);
  flex-wrap: wrap;
}

.interlude__action-secondary {
  color: var(--ink-4);
  border-color: color-mix(in oklch, var(--line), transparent 20%);
  font-size: 0.82rem;
  padding: var(--space-2) var(--space-4);
}

.interlude__action-secondary:hover:not(:disabled) {
  color: var(--ink-3);
  border-color: var(--frost);
}

.interlude__action-abandon {
  color: var(--blood);
  border-color: color-mix(in oklch, var(--blood), transparent 55%);
  font-size: 0.82rem;
  padding: var(--space-2) var(--space-4);
}

.interlude__action-abandon:hover:not(:disabled) {
  background: color-mix(in oklch, var(--blood), transparent 92%);
  border-color: var(--blood);
}

.interlude__action-error {
  color: var(--blood);
  font-family: var(--font-mono);
  font-size: 0.78rem;
  margin: 0;
}

.interlude__actions > .interlude__cta {
  align-self: end;
  justify-self: end;
}

.interlude__cta {
  border-color: var(--gold);
  color: var(--gold);
  padding: var(--space-3) var(--space-6);
  font-size: 1rem;
}

.interlude__cta:hover:not(:disabled) {
  background: color-mix(in oklch, var(--gold), transparent 88%);
}

.interlude__cta:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

/* Transition */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.18s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
