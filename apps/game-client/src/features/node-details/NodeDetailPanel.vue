<script setup lang="ts">
import type { NodeDto } from '../runs/types/runTypes';

defineProps<{
  node: NodeDto | null;
  isLoading: boolean;
  hasActiveCombat: boolean;
  hasPendingReward: boolean;
}>();

defineEmits<{
  resolveCurrentEvent: [];
  generateNextNodes: [];
}>();

function getRiskLabel(node: NodeDto): string {
  if (node.isBoss) return 'Boss';
  if (node.riskLevel >= 75) return 'Critique';
  if (node.riskLevel >= 50) return 'Élevé';
  if (node.riskLevel >= 25) return 'Modéré';
  return 'Faible';
}

function getRiskClass(node: NodeDto): string {
  if (node.isBoss) return 'risk-label--boss';
  if (node.riskLevel >= 75) return 'risk-label--critical';
  if (node.riskLevel >= 50) return 'risk-label--high';
  if (node.riskLevel >= 25) return 'risk-label--moderate';
  return 'risk-label--low';
}
</script>

<template>
  <section class="panel node-detail">
    <template v-if="node">
      <p class="system-label">
        {{ node.type }} · NODE_DEEP {{ node.row }} · {{ node.state }}
      </p>

      <h2>{{ node.type }}</h2>

      <div class="node-detail__image">
        {{ node.type }} · Placeholder
      </div>

      <p>
        'DESCRIPTION_PLACEHOLDER'
      </p>

      <div class="node-detail__meta">
        <div>
          <span class="system-label">RISQUE</span>
          <strong
            :class="getRiskClass(node)"
            :title="`Niveau brut : ${node.riskLevel}`"
          >{{ getRiskLabel(node) }}</strong>
        </div>

        <div>
          <span class="system-label">REWARD</span>
          <strong>{{ node.rewardProfile }}</strong>
        </div>
      </div>

      <button
        v-if="node.state === 'Available' || node.state === 'Selected'"
        class="ghost-button node-detail__confirm"
        :disabled="isLoading || hasActiveCombat || hasPendingReward"
        @click="$emit('resolveCurrentEvent')"
      >
        <span v-if="hasActiveCombat">EVENT_BATTLE_IN_PROGRESS</span>
        <span v-else-if="hasPendingReward">EVENT_PENDING_REWARD</span>
        <span v-else-if="node.state === 'Available'">EVENT_CONFIRM_AND_RESOLVE</span>
        <span v-else>EVENT_RESOLVE</span>
      </button>

      <button
        v-if="node.state === 'Resolved'"
        class="ghost-button node-detail__confirm"
        :disabled="isLoading || hasActiveCombat || hasPendingReward"
        @click="$emit('generateNextNodes')"
      >
        <span v-if="hasActiveCombat">EVENT_BATTLE_IN_PROGRESS</span>
        <span v-else-if="hasPendingReward">EVENT_PENDING_REWARD</span>
        <span v-else>EVENT_GENERATE_NEXT_NODES</span>
      </button>
    </template>

    <template v-else>
      <p class="system-label">ERROR_MESSAGE_NODE_NOT_FOUND</p>

      <h2>MESSAGE_NEXT_NODES</h2>

      <p>
        PLACEHOLDER_NODE_DETAIL_NEXT_NODE_FUNCTIONNALITY
      </p>
    </template>
  </section>
</template>

<style scoped>
.node-detail {
  min-height: 100%;
  padding: var(--space-4);
  border-color: color-mix(in oklch, var(--color-frost), transparent 45%);
}

.node-detail h2 {
  color: var(--color-frost);
}

.node-detail p {
  color: var(--color-muted);
  line-height: 1.55;
}

.node-detail__image {
  height: 12rem;
  display: grid;
  place-items: center;
  margin: var(--space-6) 0;
  color: var(--color-dim);
  background: radial-gradient(circle, var(--color-panel-soft), var(--color-void));
  border: 1px solid var(--color-line);
  font-family: var(--font-mono);
  font-size: 0.75rem;
  text-transform: uppercase;
}

.node-detail__meta {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
  margin: var(--space-6) 0;
}

.node-detail__meta div {
  border-top: 1px solid var(--color-line);
  padding-top: var(--space-2);
}

.node-detail__meta strong {
  display: block;
  margin-top: var(--space-1);
  color: var(--color-gold);
  font-size: 0.85rem;
}

.node-detail__confirm {
  border-color: var(--color-frost);
  color: var(--color-frost);
}

.risk-label--low      { color: var(--color-frost); }
.risk-label--moderate { color: color-mix(in oklch, var(--color-frost), var(--color-gold) 35%); }
.risk-label--high     { color: var(--color-gold); }
.risk-label--critical { color: color-mix(in oklch, var(--color-blood), var(--color-gold) 20%); }
.risk-label--boss     { color: var(--color-blood); }
</style>