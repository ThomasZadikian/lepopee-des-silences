<script setup lang="ts">
import { computed } from 'vue';
import type { NodeDto } from '../runs/types/runTypes';

const props = defineProps<{
  node: NodeDto | null;
  isLoading?: boolean;
  hasActiveCombat?: boolean;
  hasPendingReward?: boolean;
}>();

const emit = defineEmits<{
  resolveCurrentEvent: [];
  generateNextNodes: [];
  chooseAndResolve: [];
  close: [];
}>();

const nodeTypeLabel: Record<string, string> = {
  Combat: 'Confrontation',
  Elite: 'Manifestation elite',
  RoomBoss: 'Gardien de salle',
  FinalBoss: 'Confrontation finale',
  Item: 'Offrande',
  Npc: 'Présence',
  Memory: 'Souvenir',
  Rest: 'Repos',
  Merchant: 'Marchand',
  Law: 'Décret du Palais',
  Curse: 'Malédiction',
  Rare: 'Rencontre rare',
};

const stateLabel: Record<string, string> = {
  Available: 'Accessible',
  Selected: 'Sélectionné',
  Resolved: 'Résolu',
  Locked: 'Verrouillé',
  Unreachable: 'Inaccessible',
  Planned: 'En attente',
};

const riskLabel = computed(() => {
  const r = props.node?.riskLevel ?? 0;
  if (r <= 24) return { text: 'Faible', cls: 'risk--low' };
  if (r <= 49) return { text: 'Modéré', cls: 'risk--moderate' };
  if (r <= 74) return { text: 'Élevé', cls: 'risk--high' };
  return { text: 'Critique', cls: 'risk--critical' };
});

const canAct = computed(() => {
  if (!props.node) return false;
  if (props.hasActiveCombat || props.hasPendingReward) return false;
  return props.node.state === 'Available' || props.node.state === 'Selected';
});

const actionLabel = computed(() => {
  if (!props.node) return '';
  if (props.node.state === 'Available') return 'Pénétrer dans le nœud';
  if (props.node.state === 'Selected') return 'Pénétrer dans le nœud';
  return '';
});

function handleAction() {
  if (!props.node) return;
  if (props.node.state === 'Available') {
    emit('chooseAndResolve');
  } else if (props.node.state === 'Selected') {
    emit('resolveCurrentEvent');
  }
}
</script>

<template>
  <aside class="node-drawer">
    <template v-if="node">
      <div class="node-drawer__header">
        <span class="es-kicker">{{ nodeTypeLabel[node.type] ?? node.type }}</span>
        <div class="node-drawer__header-actions">
          <span class="es-chip" :class="node.state === 'Available' ? 'es-chip--frost' : node.state === 'Resolved' ? 'es-chip--sap' : ''">
            {{ stateLabel[node.state] ?? node.state }}
          </span>
          <button class="es-btn es-btn--ghost node-drawer__close" @click="emit('close')">✕</button>
        </div>
      </div>

      <hr class="es-rule" />

      <div class="node-drawer__risk">
        <span class="es-label">Risque</span>
        <span :class="['node-drawer__risk-value', riskLabel.cls]">{{ riskLabel.text }}</span>
        <span class="es-mono">{{ node.riskLevel }}/100</span>
      </div>

      <div v-if="node.rewardProfile" class="node-drawer__reward">
        <span class="es-label">Profil de récompense</span>
        <span class="es-body">{{ node.rewardProfile }}</span>
      </div>

      <div class="node-drawer__spacer" />

      <button
        v-if="canAct"
        class="es-btn es-btn--frost es-btn--lg node-drawer__cta"
        :disabled="isLoading"
        @click="handleAction"
      >
        {{ isLoading ? 'Résolution…' : actionLabel }}
      </button>

      <p v-else-if="node.state === 'Resolved'" class="node-drawer__hint">
        Ce nœud a été résolu. La progression ouvrira le chemin suivant.
      </p>

      <p v-else-if="node.state === 'Locked'" class="node-drawer__hint">
        Ce nœud est verrouillé. Un autre chemin a été choisi.
      </p>
    </template>

    <template v-else>
      <div class="node-drawer__empty">
        <span class="es-kicker">Nœud</span>
        <p class="es-body">Sélectionne un nœud sur la carte pour le consulter.</p>
      </div>
    </template>
  </aside>
</template>

<style scoped>
.node-drawer {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 45px;
  width: 280px;
  display: flex;
  flex-direction: column;
  padding: var(--space-4);
  background: oklch(0.22 0.04 272 / 0.92);
  border-left: 1px solid var(--line-soft);
  backdrop-filter: blur(8px);
  overflow-y: auto;
  z-index: var(--z-drawer);
}

.node-drawer__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.node-drawer__header-actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.node-drawer__close {
  padding: var(--space-1);
  min-height: auto;
  line-height: 1;
}

.node-drawer__risk,
.node-drawer__reward {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  margin-top: var(--space-3);
}

.node-drawer__risk-value {
  font-family: var(--font);
  font-size: 0.9rem;
}

.risk--low { color: var(--ink-3); }
.risk--moderate { color: var(--gold-dim); }
.risk--high { color: var(--blood-dim); }
.risk--critical { color: var(--blood); }

.node-drawer__spacer {
  flex: 1;
  min-height: var(--space-4);
}

.node-drawer__cta {
  width: 100%;
  justify-content: center;
}

.node-drawer__hint {
  font-size: 0.82rem;
  color: var(--ink-4);
  line-height: 1.5;
  margin: 0;
}

.node-drawer__empty {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding-top: var(--space-4);
}
</style>
