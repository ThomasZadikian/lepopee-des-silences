<script setup lang="ts">
import { computed } from 'vue';
import { getToonGradientTexture } from './toonGradientTexture';
import { TILE_SIZE, HEIGHT_UNIT } from './sceneConstants';

const gradientTexture = getToonGradientTexture();

const props = defineProps<{
  x: number;
  y: number;
  height: number;
}>();

const PARTY_COLOR = '#cbb26a';

const spherePosition = computed<[number, number, number]>(() => [
  props.x * TILE_SIZE,
  props.height * HEIGHT_UNIT + 0.3,
  props.y * TILE_SIZE,
]);

const ringPosition = computed<[number, number, number]>(() => [
  props.x * TILE_SIZE,
  props.height * HEIGHT_UNIT + 0.02,
  props.y * TILE_SIZE,
]);

const ringRotation: [number, number, number] = [Math.PI / 2, 0, 0];
</script>

<template>
  <TresMesh :position="spherePosition" :cast-shadow="true">
    <TresSphereGeometry :args="[0.22, 16, 12]" />
    <TresMeshToonMaterial
      :color="PARTY_COLOR"
      :gradient-map="gradientTexture"
      :emissive="PARTY_COLOR"
      :emissive-intensity="0.6"
    />
  </TresMesh>

  <TresMesh :position="ringPosition" :rotation="ringRotation">
    <TresTorusGeometry :args="[0.32, 0.03, 8, 24]" />
    <TresMeshToonMaterial :color="PARTY_COLOR" :gradient-map="gradientTexture" />
  </TresMesh>

  <TresPointLight :position="spherePosition" :color="PARTY_COLOR" :intensity="1.2" :distance="3" />
</template>
