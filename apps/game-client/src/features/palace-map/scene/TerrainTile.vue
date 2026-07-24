<script setup lang="ts">
import { computed } from 'vue';
import * as THREE from 'three';
import { getToonGradientTexture } from './toonGradientTexture';
import { TILE_SIZE, HEIGHT_UNIT, TILE_THICKNESS } from './sceneConstants';

const gradientTexture = getToonGradientTexture();

const props = defineProps<{
  x: number;
  y: number;
  height: number;
  floorColor: number;
}>();

const emit = defineEmits<{ click: [] }>();

const position = computed<[number, number, number]>(() => [
  props.x * TILE_SIZE,
  props.height * HEIGHT_UNIT - TILE_THICKNESS / 2,
  props.y * TILE_SIZE,
]);

// Taller tiles read lighter — the 3D analogue of the CSS --tile-tint-pct height boost.
const color = computed(() => {
  const base = new THREE.Color(props.floorColor);
  return base.clone().lerp(new THREE.Color(0xffffff), Math.min(props.height, 3) * 0.08);
});
</script>

<template>
  <TresMesh :position="position" :receive-shadow="true" @click="emit('click')">
    <TresBoxGeometry :args="[TILE_SIZE * 0.94, TILE_THICKNESS, TILE_SIZE * 0.94]" />
    <TresMeshToonMaterial :color="color" :gradient-map="gradientTexture" />
  </TresMesh>
</template>
