<script setup lang="ts">
import { computed, ref } from 'vue';
import { useLoop } from '@tresjs/core';
import { OrbitControls } from '@tresjs/cientos';
import type { RoomDto } from '../../runs/types/runTypes';
import { useGridCells } from '../composables/useGridCells';
import { usePalaceTerrain } from '../composables/usePalaceTerrain';
import { usePartyTokenPath } from '../composables/usePartyTokenPath';
import { useRoomBackdropTheme } from '../composables/useRoomBackdropTheme';
import TerrainTile from './TerrainTile.vue';
import FogCloud from './FogCloud.vue';
import NodeMarker3D from './NodeMarker3D.vue';
import PartyToken3D from './PartyToken3D.vue';
import { TILE_SIZE } from './sceneConstants';

const props = defineProps<{ room: RoomDto }>();
const emit = defineEmits<{ cellClick: [x: number, y: number] }>();

const room = computed(() => props.room);
const grid = computed(() => props.room.grid ?? null);

const { cells, isRevealed, nodeAt, isParty } = useGridCells(room, grid);
const { terrainHeight } = usePalaceTerrain(room, grid);
const { displayPartyX, displayPartyY, prefersReducedMotion } = usePartyTokenPath(room, grid);
const { palette3D } = useRoomBackdropTheme(room);

const centerTarget = computed<[number, number, number]>(() => {
  const g = grid.value;
  if (!g) return [0, 0, 0];
  return [((g.width - 1) / 2) * TILE_SIZE, 0, ((g.height - 1) / 2) * TILE_SIZE];
});

const cameraDistance = computed(() => {
  const g = grid.value;
  if (!g) return 10;
  return Math.max(g.width, g.height) * 1.1 + 4;
});

const cameraPosition = computed<[number, number, number]>(() => {
  const [cx, , cz] = centerTarget.value;
  const d = cameraDistance.value;
  return [cx + d * 0.6, d * 0.75, cz + d * 0.6];
});

const lightPosition = computed<[number, number, number]>(() => {
  const [cx, , cz] = centerTarget.value;
  return [cx + 5, 8, cz + 3];
});

function onCellClick(x: number, y: number) {
  if (!isRevealed(x, y)) return;
  if (isParty(x, y)) return;
  emit('cellClick', x, y);
}

// ── Final theme pulsation — the 3D analogue of the CSS tgrid-backdrop-pulse keyframe
// (6s period, opacity 0.75↔1) — here driving the ambient light intensity instead of a
// CSS opacity, since there's no flat backdrop layer to fade in a real 3D scene.
const pulseMultiplier = ref(1);
if (!prefersReducedMotion) {
  const { onBeforeRender } = useLoop();
  onBeforeRender(({ elapsed }) => {
    const speed = palette3D.value.pulseSpeed;
    if (!speed) {
      pulseMultiplier.value = 1;
      return;
    }
    pulseMultiplier.value = 0.75 + 0.25 * (0.5 + 0.5 * Math.sin(elapsed * speed * Math.PI * 2));
  });
}

const ambientIntensity = computed(() => palette3D.value.ambientLightIntensity * pulseMultiplier.value);
</script>

<template>
  <TresPerspectiveCamera :position="cameraPosition" />
  <OrbitControls
    make-default
    :target="centerTarget"
    :min-distance="cameraDistance * 0.4"
    :max-distance="cameraDistance * 2"
    :min-polar-angle="0.15"
    :max-polar-angle="Math.PI / 2 - 0.05"
  />

  <TresAmbientLight :color="palette3D.ambientLightColor" :intensity="ambientIntensity" />
  <TresDirectionalLight
    :color="palette3D.directionalLightColor"
    :intensity="palette3D.directionalLightIntensity"
    :position="lightPosition"
  />

  <template v-if="grid">
    <template v-for="cell in cells" :key="`${cell.x}-${cell.y}`">
      <TerrainTile
        v-if="isRevealed(cell.x, cell.y)"
        :x="cell.x"
        :y="cell.y"
        :height="terrainHeight(cell.x, cell.y)"
        :floor-color="palette3D.floorColor"
        @click="onCellClick(cell.x, cell.y)"
      />
      <FogCloud
        v-else
        :x="cell.x"
        :y="cell.y"
        :room-id="room.id"
        :fog-color="palette3D.fogColor"
        :reduced-motion="prefersReducedMotion"
      />

      <NodeMarker3D
        v-if="nodeAt(cell.x, cell.y)"
        :node="nodeAt(cell.x, cell.y)!"
        :x="cell.x"
        :y="cell.y"
        :height="terrainHeight(cell.x, cell.y)"
        :ghost="!isRevealed(cell.x, cell.y)"
        :reduced-motion="prefersReducedMotion"
        @click="onCellClick(cell.x, cell.y)"
      />
    </template>

    <PartyToken3D
      :x="displayPartyX"
      :y="displayPartyY"
      :height="terrainHeight(displayPartyX, displayPartyY)"
    />
  </template>
</template>
