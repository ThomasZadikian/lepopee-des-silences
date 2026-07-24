<script setup lang="ts">
import { computed, ref } from 'vue';
import { useLoop } from '@tresjs/core';
import { hashSeed } from '../composables/usePalaceTerrain';
import { TILE_SIZE } from './sceneConstants';

const props = defineProps<{
  x: number;
  y: number;
  roomId: string;
  fogColor: number;
  reducedMotion: boolean;
}>();

// Same hash seeds as the CSS renderer's --fog-jx/--fog-jy/--fog-scale (cellStyle in
// TacticalGridMap.vue) so an irregular, non-repeating cloud shape reads consistently
// whichever renderer draws it.
const fogSeed = hashSeed(`${props.roomId}:fog:${props.x}:${props.y}`);
const fogSeed2 = hashSeed(`${props.roomId}:fog2:${props.x}:${props.y}`);

const jitterX = ((fogSeed % 41) - 20) / 20;
const jitterZ = ((Math.floor(fogSeed / 41) % 41) - 20) / 20;
const cloudScale = 0.6 + ((fogSeed2 % 100) / 100) * 0.5;
const driftPhase = ((fogSeed % 1000) / 1000) * Math.PI * 2;

const basePosition: [number, number, number] = [
  props.x * TILE_SIZE + jitterX * 0.25,
  0.12,
  props.y * TILE_SIZE + jitterZ * 0.25,
];

const meshRef = ref<{ position: { x: number; z: number }; rotation: { y: number } } | null>(null);

if (!props.reducedMotion) {
  const { onBeforeRender } = useLoop();
  onBeforeRender(({ elapsed }) => {
    const mesh = meshRef.value;
    if (!mesh) return;
    const t = elapsed * 0.3 + driftPhase;
    mesh.position.x = basePosition[0] + Math.sin(t) * 0.08;
    mesh.position.z = basePosition[2] + Math.cos(t * 0.8) * 0.08;
    mesh.rotation.y = t * 0.2;
  });
}

const colorHex = computed(() => props.fogColor);
</script>

<template>
  <TresMesh ref="meshRef" :position="basePosition" :scale="cloudScale">
    <TresIcosahedronGeometry :args="[0.55, 0]" />
    <TresMeshStandardMaterial :color="colorHex" :transparent="true" :opacity="0.22" :roughness="1" />
  </TresMesh>
</template>
