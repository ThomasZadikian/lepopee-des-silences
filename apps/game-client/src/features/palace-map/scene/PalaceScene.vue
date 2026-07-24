<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import * as THREE from 'three';
import { useLoop, useTresContext } from '@tresjs/core';
import { OrbitControls } from '@tresjs/cientos';
import type { NodeDto, RoomDto } from '../../runs/types/runTypes';
import { useGridCells } from '../composables/useGridCells';
import { usePalaceTerrain } from '../composables/usePalaceTerrain';
import { usePartyTokenPath } from '../composables/usePartyTokenPath';
import { useRoomBackdropTheme } from '../composables/useRoomBackdropTheme';
import TerrainTile from './TerrainTile.vue';
import FogCloud from './FogCloud.vue';
import NodeMarker3D from './NodeMarker3D.vue';
import PartyToken3D from './PartyToken3D.vue';
import SceneDecor from './SceneDecor.vue';
import { TILE_SIZE } from './sceneConstants';

const props = defineProps<{ room: RoomDto }>();
const emit = defineEmits<{
  cellClick: [x: number, y: number];
  nodeHover: [payload: { node: NodeDto; clientX: number; clientY: number } | null];
}>();

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

// Half-width of the directional light's shadow-camera frustum — sized to the grid so
// every tile falls inside it (an unsized default frustum, ~-5..5, would clip most of a
// production 10x8 board and silently drop shadows past its edge).
const shadowExtent = computed(() => {
  const g = grid.value;
  if (!g) return 8;
  return (Math.max(g.width, g.height) * TILE_SIZE) / 2 + 2;
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

// ── Scene-level atmospheric fog ──────────────────────────────────────────────────
// Set imperatively on the real THREE.Scene instance rather than via a template
// <primitive attach="fog">: TresJS's patchProp only special-cases a primitive's
// `object` prop at initial creation, not on later reactive patches (the same bug
// class already worked around in NodeMarker3D's geometry assignment) — a plain watch
// mutating an existing Fog instance's own properties sidesteps that entirely.
const { scene } = useTresContext();
const sceneFog = new THREE.FogExp2(palette3D.value.fogColor, palette3D.value.fogDensity);

watch(scene, (s) => { if (s) s.fog = sceneFog; }, { immediate: true });
watch(palette3D, (p) => {
  sceneFog.color.set(p.fogColor);
  sceneFog.density = p.fogDensity;
});

// ── Directional light shadow target + frustum ────────────────────────────────────
// Both set imperatively rather than via template shadow-camera-* props: THREE.
// DirectionalLight aims its shadow camera at `light.target`, which defaults to an
// Object3D sitting at the world origin — never touched here otherwise, so its matrix
// would go stale (the grid itself is centered at `centerTarget`, not the origin).
// And OrthographicCamera (what `light.shadow.camera` is) only recomputes its actual
// projection when `updateProjectionMatrix()` runs — nothing calls that for us after a
// reactive prop patch, so sizing the frustum via template props alone silently keeps
// the default ~-5..5 bounds regardless of what's bound in the template.
const lightRef = ref<THREE.DirectionalLight | null>(null);
watch([lightRef, centerTarget, shadowExtent], ([light, ct, extent]) => {
  if (!light) return;
  light.target.position.set(...ct);
  light.target.updateMatrixWorld();

  const cam = light.shadow.camera;
  cam.left = -extent;
  cam.right = extent;
  cam.top = extent;
  cam.bottom = -extent;
  cam.near = 0.5;
  cam.far = 30;
  cam.updateProjectionMatrix();
}, { immediate: true });
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
    ref="lightRef"
    :color="palette3D.directionalLightColor"
    :intensity="palette3D.directionalLightIntensity"
    :position="lightPosition"
    :cast-shadow="true"
    :shadow-mapSize-width="1024"
    :shadow-mapSize-height="1024"
    :shadow-bias="-0.0015"
  />
  <!-- Soft sky/ground fill — the single biggest lever against the flat, everything-in-
       shadow look a lone ambient+directional pair gives stylized low-poly tiles.
       Scaled off the theme's own ambient intensity rather than a flat value so a
       deliberately dim/oppressive theme (Final) stays dim instead of being washed out
       by a fill light that doesn't know about that intent. -->
  <TresHemisphereLight
    :color="palette3D.ambientLightColor"
    :ground-color="palette3D.floorColor"
    :intensity="palette3D.ambientLightIntensity * 0.45"
  />

  <template v-if="grid">
    <SceneDecor :room="room" :grid="grid" :accent-color="palette3D.accentColor" />

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
        @pointer-enter="(clientX, clientY) => emit('nodeHover', { node: nodeAt(cell.x, cell.y)!, clientX, clientY })"
        @pointer-move="(clientX, clientY) => emit('nodeHover', { node: nodeAt(cell.x, cell.y)!, clientX, clientY })"
        @pointer-leave="emit('nodeHover', null)"
      />
    </template>

    <PartyToken3D
      :x="displayPartyX"
      :y="displayPartyY"
      :height="terrainHeight(displayPartyX, displayPartyY)"
    />
  </template>
</template>
