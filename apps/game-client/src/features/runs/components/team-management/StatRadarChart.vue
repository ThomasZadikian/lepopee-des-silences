<script setup lang="ts">
import { computed } from 'vue';
import type { PlayerStatKind } from '../../../party/types/playerTypes';
import { statLabels, statOrder, statRadarMax } from '../../../party/constants/statDescriptions';

const props = defineProps<{
  values: Record<PlayerStatKind, number>;
  previewValues?: Record<PlayerStatKind, number> | null;
}>();

const size = 320;
const center = size / 2;
const maxRadius = 120;
const axisCount = statOrder.length;

function angleFor(index: number): number {
  // Start at the top (12 o'clock), step clockwise.
  return (Math.PI * 2 * index) / axisCount - Math.PI / 2;
}

function pointFor(index: number, ratio: number): { x: number; y: number } {
  const angle = angleFor(index);
  const radius = maxRadius * Math.min(1, Math.max(0, ratio));
  return {
    x: center + radius * Math.cos(angle),
    y: center + radius * Math.sin(angle),
  };
}

function ratioFor(stat: PlayerStatKind, value: number): number {
  const max = statRadarMax[stat] || 1;
  return value / max;
}

function polygonPoints(values: Record<PlayerStatKind, number>): string {
  return statOrder
    .map((stat, i) => {
      const point = pointFor(i, ratioFor(stat, values[stat] ?? 0));
      return `${point.x},${point.y}`;
    })
    .join(' ');
}

const gridRings = [0.25, 0.5, 0.75, 1];

function ringPoints(ratio: number): string {
  return statOrder.map((_, i) => {
    const point = pointFor(i, ratio);
    return `${point.x},${point.y}`;
  }).join(' ');
}

const axisLines = computed(() =>
  statOrder.map((stat, i) => ({
    stat,
    ...pointFor(i, 1),
  })),
);

const labelPositions = computed(() =>
  statOrder.map((stat, i) => {
    const point = pointFor(i, 1.16);
    return { stat, label: statLabels[stat], ...point };
  }),
);

const currentPoints = computed(() => polygonPoints(props.values));
const currentMarkers = computed(() =>
  statOrder.map((stat, i) => ({
    stat,
    value: props.values[stat] ?? 0,
    ...pointFor(i, ratioFor(stat, props.values[stat] ?? 0)),
  })),
);

const hasPreview = computed(() =>
  !!props.previewValues
  && statOrder.some((stat) => (props.previewValues?.[stat] ?? 0) !== (props.values[stat] ?? 0)),
);
const previewPoints = computed(() =>
  hasPreview.value && props.previewValues ? polygonPoints(props.previewValues) : '',
);
</script>

<template>
  <svg
    class="stat-radar"
    :viewBox="`0 0 ${size} ${size}`"
    role="img"
    aria-label="Diagramme radar des caractéristiques"
  >
    <polygon
      v-for="ratio in gridRings"
      :key="ratio"
      :points="ringPoints(ratio)"
      class="stat-radar__ring"
    />
    <line
      v-for="axis in axisLines"
      :key="axis.stat"
      :x1="center"
      :y1="center"
      :x2="axis.x"
      :y2="axis.y"
      class="stat-radar__axis"
    />

    <polygon
      v-if="hasPreview"
      :points="previewPoints"
      class="stat-radar__polygon stat-radar__polygon--preview"
    />

    <polygon :points="currentPoints" class="stat-radar__polygon stat-radar__polygon--current" />
    <circle
      v-for="marker in currentMarkers"
      :key="marker.stat"
      :cx="marker.x"
      :cy="marker.y"
      r="4"
      class="stat-radar__marker"
    >
      <title>{{ statLabels[marker.stat] }} : {{ marker.value }}</title>
    </circle>

    <text
      v-for="pos in labelPositions"
      :key="pos.stat"
      :x="pos.x"
      :y="pos.y"
      class="stat-radar__label"
      text-anchor="middle"
      dominant-baseline="middle"
    >{{ pos.label }}</text>
  </svg>
</template>

<style scoped>
.stat-radar {
  width: 100%;
  max-width: 360px;
  height: auto;
  display: block;
  margin: 0 auto;
}

.stat-radar__ring {
  fill: none;
  stroke: var(--line-soft);
  stroke-width: 1;
}

.stat-radar__axis {
  stroke: var(--line-soft);
  stroke-width: 1;
}

.stat-radar__polygon--current {
  fill: color-mix(in oklch, var(--mint) 14%, transparent);
  stroke: var(--mint-dim);
  stroke-width: 2;
  stroke-linejoin: round;
}

.stat-radar__polygon--preview {
  fill: none;
  stroke: var(--ink-3);
  stroke-width: 2;
  stroke-dasharray: 4 3;
  stroke-linejoin: round;
}

.stat-radar__marker {
  fill: var(--mint-dim);
  stroke: var(--panel);
  stroke-width: 2;
}

.stat-radar__label {
  font-family: var(--font-mono);
  font-size: 9px;
  letter-spacing: 0.04em;
  fill: var(--ink-4);
}
</style>
