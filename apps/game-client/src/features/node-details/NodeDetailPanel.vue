<script setup lang="ts">
import type { NodeDto } from '../runs/types/runTypes';

defineProps<{
  node: NodeDto | null;
  isLoading: boolean;
}>();

defineEmits<{
  resolveCurrentEvent: [];
  generateNextNodes: [];
}>();
</script>

<template>
  <section class="panel node-detail">
    <template v-if="node">
      <p class="system-label">
        {{ node.eventTypes[0] }} · profondeur {{ node.nodeDepth }} · {{ node.state }}
      </p>

      <h2>{{ node.eventTypes.join(' · ') }}</h2>

      <div class="node-detail__image">
        {{ node.eventTypes[0] }} · Placeholder
      </div>

      <p>
        Le Palais a isolé ce nœud. Le backend attend une intention :
        résoudre l’événement courant ou poursuivre si le nœud est déjà clos.
      </p>

      <div class="node-detail__meta">
        <div>
          <span class="system-label">Risque</span>
          <strong>{{ node.riskLevel }}</strong>
        </div>
        <div>
          <span class="system-label">Récompense</span>
          <strong>{{ node.rewardProfile }}</strong>
        </div>
      </div>

      <button
        v-if="node.state === 'Selected'"
        class="ghost-button node-detail__confirm"
        :disabled="isLoading"
        @click="$emit('resolveCurrentEvent')"
      >
        Résoudre →
      </button>

      <button
        v-if="node.state === 'Resolved'"
        class="ghost-button node-detail__confirm"
        :disabled="isLoading"
        @click="$emit('generateNextNodes')"
      >
        Prochaine strate →
      </button>
    </template>

    <template v-else>
      <p class="system-label">Aucun nœud sélectionné</p>
      <h2>Choisis un chemin</h2>
      <p>
        Les chemins disponibles sont visibles sur la carte. Une fois confirmé,
        le Palais ferme les alternatives.
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
</style>