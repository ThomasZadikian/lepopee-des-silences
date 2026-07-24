<script setup lang="ts">
import { onMounted, ref } from 'vue';
import * as THREE from 'three';
import type { DecorPart } from '../composables/useSceneDecor';
import { getToonGradientTexture } from './toonGradientTexture';

const props = defineProps<{ part: DecorPart }>();

const gradientTexture = getToonGradientTexture();

// Computed once, non-reactively — SceneDecor.vue keys each prop cluster by room id, so
// a room change fully remounts this component rather than reactively re-patching it.
// (Same bug class as NodeMarker3D's geometry: TresJS's patchProp only special-cases a
// primitive's `object` prop at initial creation, not on later reactive patches — a
// direct assignment on the real THREE.Mesh instance in onMounted sidesteps it.)
const geometry = props.part.geometryFactory();

const meshRef = ref<THREE.Mesh | null>(null);

onMounted(() => {
  if (meshRef.value) meshRef.value.geometry = geometry;
});
</script>

<template>
  <TresMesh ref="meshRef" :position="part.offset" :cast-shadow="true" :receive-shadow="true">
    <TresMeshToonMaterial
      :color="part.color"
      :gradient-map="gradientTexture"
      :emissive="part.emissive"
      :emissive-intensity="part.emissiveIntensity"
    />
  </TresMesh>
</template>
