<script setup lang="ts">
import type { NodeDto } from '../runs/types/runTypes';

const props = defineProps<{
  nodes: NodeDto[];
  availableNodes: NodeDto[];
  selectedNodeId?: string | null;
}>();

const emit = defineEmits<{
  chooseNode: [nodeId: string];
}>();

function getNodeGlyph(node: NodeDto): string {
  const type = node.eventTypes[0];

  switch (type) {
    case 'Combat':
      return '△';
    case 'Elite':
      return '▲';
    case 'Item':
      return '♢';
    case 'Npc':
      return '◇';
    case 'Rest':
      return '◐';
    case 'Merchant':
      return '◎';
    case 'Law':
      return '◆';
    case 'RoomBoss':
    case 'FinalBoss':
      return '◎';
    default:
      return '○';
  }
}

function getNodeClass(node: NodeDto): string {
  if (node.state === 'Resolved') {
    return 'map__node--resolved';
  }

  if (node.id === props.selectedNodeId || node.state === 'Selected') {
    return 'map__node--selected';
  }

  if (node.state === 'Available') {
    return 'map__node--available';
  }

  if (node.isRoomBossNode) {
    return 'map__node--danger';
  }

  return 'map__node--locked';
}

function getNodePosition(node: NodeDto) {
  const depth = Math.max(0, node.nodeDepth);
  const sameDepthNodes = props.nodes.filter((candidate) => candidate.nodeDepth === depth);
  const indexAtDepth = sameDepthNodes.findIndex((candidate) => candidate.id === node.id);
  const countAtDepth = Math.max(1, sameDepthNodes.length);

  const maxDepth = Math.max(...props.nodes.map((candidate) => candidate.nodeDepth), 1);

  const x = 8 + (depth / maxDepth) * 84;
  const y = 20 + ((indexAtDepth + 1) / (countAtDepth + 1)) * 60;

  return {
    left: `${x}%`,
    top: `${y}%`,
  };
}

function chooseNode(node: NodeDto) {
  if (!isAvailable(node) && node.state !== 'Available') {
    return;
  }

  emit('chooseNode', node.id);
}

function isAvailable(node: NodeDto): boolean {
  return props.availableNodes.some((availableNode) => availableNode.id === node.id);
}

</script>

<template>
  <section class="map">
    <header>
      <div>
        <p class="system-label">Carte du Palais — embranchements visibles</p>
        <h2>Les chemins sont irréversibles · ils peuvent se rejoindre</h2>
      </div>
      <span class="system-value">Nodes {{ nodes.length }}</span>
    </header>

    <div class="map__canvas" aria-label="Carte roguelite">
      <button
        v-for="node in nodes"
        :key="node.id"
        class="map__node"
        :class="getNodeClass(node)"
        :style="getNodePosition(node)"
        :disabled="!isAvailable(node) && node.state !== 'Available'"
        @click="chooseNode(node)"
      >
        <span>{{ getNodeGlyph(node) }}</span>
        <small>{{ node.eventTypes[0] }}</small>
      </button>
    </div>
  </section>
</template>

<style scoped>
.map {
  height: 100%;
}

.map header {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
}

.map h2 {
  margin: var(--space-1) 0 0;
  color: var(--color-muted);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.map__canvas {
  position: relative;
  height: calc(100% - 4rem);
  margin-top: var(--space-6);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 60%);
  background:
    radial-gradient(circle at 20% 50%, rgb(120 180 220 / 8%), transparent 18%),
    radial-gradient(circle at 88% 50%, rgb(180 40 60 / 9%), transparent 12%);
}

.map__canvas::before {
  content: '';
  position: absolute;
  inset: 12%;
  border-top: 1px dashed color-mix(in oklch, var(--color-line), transparent 35%);
  transform: skewY(-10deg);
}

.map__node {
  position: absolute;
  width: 3.6rem;
  height: 3.6rem;
  display: grid;
  place-items: center;
  gap: 0.1rem;
  translate: -50% -50%;
  border-radius: 50%;
  background: var(--color-panel);
  border: 1px solid var(--color-line);
  color: var(--color-muted);
  font-family: var(--font-mono);
  box-shadow: 0 0 40px rgb(0 0 0 / 50%);
  cursor: pointer;
}

.map__node:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.map__node small {
  position: absolute;
  top: calc(100% + 0.35rem);
  color: var(--color-dim);
  font-size: 0.55rem;
  white-space: nowrap;
  text-transform: uppercase;
}

.map__node--selected {
  color: var(--color-gold);
  border-color: var(--color-gold);
  box-shadow: 0 0 32px color-mix(in oklch, var(--color-gold), transparent 55%);
}

.map__node--available,
.map__node--frost {
  color: var(--color-frost);
  border-color: var(--color-frost);
}

.map__node--danger {
  color: var(--color-blood);
  border-color: var(--color-blood);
}
.map__node--resolved {
  color: oklch(78% 0.14 145);
  border-color: oklch(70% 0.16 145);
  box-shadow:
    0 0 18px color-mix(in oklch, oklch(70% 0.16 145), transparent 45%),
    0 0 42px color-mix(in oklch, oklch(70% 0.16 145), transparent 70%);
}

.map__node--resolved::before {
  content: '';
  position: absolute;
  inset: -0.45rem;
  border-radius: 50%;
  border: 1px solid color-mix(in oklch, oklch(70% 0.16 145), transparent 45%);
  opacity: 0.75;
}
</style>