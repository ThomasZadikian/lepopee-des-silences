<script setup lang="ts">
import { computed } from 'vue';

import type { NodeDto } from '../runs/types/runTypes';

const props = defineProps<{
  nodes: NodeDto[];
  availableNodes: NodeDto[];
  selectedNodeId?: string | null;
  currentRow?: number;
  layoutTemplateKey?: string | null;
  layoutTemplateVersion?: string | null;
}>();

const emit = defineEmits<{
  chooseNode: [nodeId: string];
}>();

type NodeCoordinates = {
  x: number;
  y: number;
};

type MapEdge = {
  id: string;
  parent: NodeDto;
  child: NodeDto;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  className: string;
};

const nodesById = computed(() => {
  return new Map(props.nodes.map((node) => [node.id, node]));
});

const childrenByParentId = computed(() => {
  const result = new Map<string, NodeDto[]>();

  for (const node of props.nodes) {
    for (const parentNodeId of node.parentNodeIds) {
      if (!result.has(parentNodeId)) {
        result.set(parentNodeId, []);
      }

      result.get(parentNodeId)!.push(node);
    }
  }

  for (const children of result.values()) {
    children.sort((left, right) => {
      if (left.row !== right.row) {
        return left.row - right.row;
      }

      if (left.lane !== right.lane) {
        return left.lane - right.lane;
      }

      return left.id.localeCompare(right.id);
    });
  }

  return result;
});

const selectedRouteNodeIds = computed(() => {
  if (!props.selectedNodeId) {
    return new Set<string>();
  }

  const selectedNode = nodesById.value.get(props.selectedNodeId);

  if (!selectedNode) {
    return new Set<string>();
  }

  const result = new Set<string>();

  collectAncestors(selectedNode, result);
  collectDescendants(selectedNode, result);
  result.add(selectedNode.id);

  return result;
});

const nodeEdges = computed<MapEdge[]>(() => {
  return props.nodes.flatMap((child) => {
    const childCoordinates = getNodeCoordinates(child);

    return child.parentNodeIds
      .map((parentNodeId) => {
        const parent = nodesById.value.get(parentNodeId);

        if (!parent) {
          return null;
        }

        if (!shouldDisplayEdge(parent, child)) {
          return null;
        }

        const parentCoordinates = getNodeCoordinates(parent);

        return {
          id: `${parent.id}-${child.id}`,
          parent,
          child,
          x1: parentCoordinates.x,
          y1: parentCoordinates.y,
          x2: childCoordinates.x,
          y2: childCoordinates.y,
          className: getEdgeClass(parent, child),
        };
      })
      .filter((edge): edge is MapEdge => edge !== null);
  });
});

function collectAncestors(node: NodeDto, result: Set<string>) {
  for (const parentNodeId of node.parentNodeIds) {
    if (result.has(parentNodeId)) {
      continue;
    }

    const parent = nodesById.value.get(parentNodeId);

    if (!parent) {
      continue;
    }

    result.add(parent.id);
    collectAncestors(parent, result);
  }
}

function collectDescendants(node: NodeDto, result: Set<string>) {
  const children = childrenByParentId.value.get(node.id) ?? [];

  for (const child of children) {
    if (result.has(child.id)) {
      continue;
    }

    result.add(child.id);
    collectDescendants(child, result);
  }
}

function getNodeGlyph(node: NodeDto): string {
  const type = node.type;

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
  if (isPastUnchosenNode(node)) {
    return 'map__node--past-unchosen';
  }

  if (node.state === 'Resolved') {
    return 'map__node--resolved';
  }

  if (node.id === props.selectedNodeId || node.state === 'Selected') {
    return 'map__node--selected';
  }

  if (node.state === 'Available') {
    return 'map__node--available';
  }

  if (node.isBoss) {
    return 'map__node--danger';
  }

  return 'map__node--locked';
}

function getEdgeClass(parent: NodeDto, child: NodeDto): string {
  if (props.selectedNodeId && isEdgeOnSelectedRoute(parent, child)) {
    return 'map__edge--selected-route';
  }

  if (props.selectedNodeId) {
    return 'map__edge--unselected-route';
  }

  if (child.isBoss) {
    return 'map__edge--danger';
  }

  if (parent.state === 'Resolved' && child.state === 'Available') {
    return 'map__edge--available';
  }

  if (parent.state === 'Selected' || child.state === 'Selected') {
    return 'map__edge--selected';
  }

  if (parent.state === 'Resolved' || child.state === 'Resolved') {
    return 'map__edge--resolved';
  }

  return 'map__edge--locked';
}

function getNodeCoordinates(node: NodeDto): NodeCoordinates {
  const maxRow = Math.max(...props.nodes.map((candidate) => candidate.row), 1);

  const nodesOnSameRow = props.nodes.filter((candidate) => candidate.row === node.row);
  const maxLaneOnRow = Math.max(...nodesOnSameRow.map((candidate) => candidate.lane), 1);

  const x = 8 + (node.row / maxRow) * 84;
  const y = 20 + ((node.lane + 1) / (maxLaneOnRow + 2)) * 60;

  return { x, y };
}

function getNodePosition(node: NodeDto) {
  const coordinates = getNodeCoordinates(node);

  return {
    left: `${coordinates.x}%`,
    top: `${coordinates.y}%`,
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

function isCommittedNode(node: NodeDto): boolean {
  return node.state === 'Resolved' || node.state === 'Selected';
}

function isPastUnchosenNode(node: NodeDto): boolean {
  if (props.currentRow === undefined || props.currentRow === null) {
    return false;
  }

  return node.row < props.currentRow && !isCommittedNode(node);
}

function shouldDisplayEdge(parent: NodeDto, child: NodeDto): boolean {
  if (isPastUnchosenNode(parent) || isPastUnchosenNode(child)) {
    return false;
  }

  return true;
}

function hasSelectedNode(): boolean {
  return Boolean(props.selectedNodeId);
}

function isNodeInSelectedRoute(node: NodeDto): boolean {
  return selectedRouteNodeIds.value.has(node.id);
}

function isEdgeOnSelectedRoute(parent: NodeDto, child: NodeDto): boolean {
  return (
    selectedRouteNodeIds.value.has(parent.id) &&
    selectedRouteNodeIds.value.has(child.id)
  );
}

function getNodePulseDelay(node: NodeDto): number {
  const hash = [...node.id].reduce((accumulator, character) => {
    return accumulator + character.charCodeAt(0);
  }, 0);

  return -(hash % 1800);
}
</script>

<template>
  <section class="map">
    <header>
      <div>
        <p class="system-label">{{ layoutTemplateKey ?? 'CARTE' }}</p>
        <p v-if="layoutTemplateVersion" class="system-dim">{{ layoutTemplateVersion }}</p>
      </div>

      <span class="system-value">{{ nodes.length }} NŒUDS · ROW {{ currentRow ?? 0 }}/{{ Math.max(...nodes.map(n => n.row), 0) }}</span>
    </header>

    <div class="map__canvas" aria-label="Carte roguelite">
      <svg
        class="map__edges"
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
        aria-hidden="true"
      >
        <line
          v-for="edge in nodeEdges"
          :key="edge.id"
          class="map__edge"
          :class="edge.className"
          :x1="edge.x1"
          :y1="edge.y1"
          :x2="edge.x2"
          :y2="edge.y2"
        />
      </svg>

      <button
        v-for="node in nodes"
        :key="node.id"
        class="map__node"
        :class="[
          getNodeClass(node),
          {
            'map__node--selected-route': isNodeInSelectedRoute(node),
            'map__node--unselected-route': hasSelectedNode() && !isNodeInSelectedRoute(node),
          },
        ]"
        :style="{
          ...getNodePosition(node),
          '--node-pulse-delay': `${getNodePulseDelay(node)}ms`,
        }"
        :disabled="node.state !== 'Available'"
        @click="chooseNode(node)"
      >
        <span>{{ getNodeGlyph(node) }}</span>
        <small>{{ node.type }}</small>
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
  overflow: hidden;
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
  z-index: 0;
}

.map__edges {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  pointer-events: none;
  z-index: 1;
}

.map__edge {
  stroke-width: 0.35;
  vector-effect: non-scaling-stroke;
  stroke-linecap: round;
}

.map__edge--locked {
  stroke: color-mix(in oklch, var(--color-line), var(--color-frost) 20%);
  stroke-dasharray: 4 5;
  opacity: 0.13;
}

.map__edge--available {
  stroke: var(--color-frost);
  opacity: 0.5;
  stroke-width: 0.42;
}

.map__edge--selected {
  stroke: var(--color-gold);
  opacity: 0.75;
  stroke-width: 0.48;
}

.map__edge--resolved {
  stroke: oklch(70% 0.16 145);
  opacity: 0.55;
  stroke-width: 0.42;
}

.map__edge--danger {
  stroke: color-mix(in oklch, var(--color-blood), var(--color-gold) 25%);
  opacity: 0.65;
  stroke-width: 0.45;
}

.map__edge--selected-route {
  stroke: var(--color-gold);
  opacity: 0.88;
  stroke-width: 0.62;
  filter: drop-shadow(0 0 5px currentColor);
}

.map__edge--unselected-route {
  stroke: color-mix(in oklch, var(--color-line), white 15%);
  stroke-dasharray: 4 6;
  opacity: 0.07;
  stroke-width: 0.25;
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
  box-shadow: 0 0 14px rgb(0 0 0 / 45%);
  cursor: pointer;
  animation: none;
  isolation: isolate;
  z-index: 2;
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
  box-shadow: 0 0 24px color-mix(in oklch, var(--color-gold), transparent 60%);
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
  box-shadow: 0 0 16px color-mix(in oklch, oklch(70% 0.16 145), transparent 55%);
}

.map__node--resolved::before {
  content: '';
  position: absolute;
  inset: -0.35rem;
  border-radius: 50%;
  border: 1px solid color-mix(in oklch, oklch(70% 0.16 145), transparent 55%);
  opacity: 0.55;
}

.map__node--locked {
  opacity: 0.42;
}

.map__node--past-unchosen {
  color: color-mix(in oklch, white, var(--color-muted) 55%);
  border-color: color-mix(in oklch, white, transparent 78%);
  opacity: 0.08;
  filter: grayscale(1);
  box-shadow: none;
}

.map__node--selected-route {
  opacity: 1;
  filter: brightness(1.18);
}

.map__node--unselected-route {
  opacity: 0.24;
}

.map__node--available {
  animation-duration: 2.4s;
}

.map__node--selected {
  animation-duration: 1.2s;
}

.map__node--resolved {
  animation-duration: 5s;
}

.map__node--danger {
  animation-duration: 2.1s;
}

@media (prefers-reduced-motion: reduce) {
  .map__node {
    animation: none;
  }
}
</style>