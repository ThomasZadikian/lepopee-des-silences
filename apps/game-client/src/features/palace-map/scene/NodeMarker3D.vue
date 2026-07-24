<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import * as THREE from 'three';
import { useLoop } from '@tresjs/core';
import type { NodeDto } from '../../runs/types/runTypes';
import { useNodeGeometry } from '../composables/useNodeGeometry';
import { useNodePresentation } from '../composables/useNodePresentation';
import { TILE_SIZE, HEIGHT_UNIT } from './sceneConstants';

const props = defineProps<{
  node: NodeDto;
  x: number;
  y: number;
  height: number;
  /** Known through fog but not yet revealed — dimmer, per the fog-marker CSS treatment. */
  ghost: boolean;
  reducedMotion: boolean;
}>();

const emit = defineEmits<{ click: [] }>();

const { geometrySpecFor } = useNodeGeometry();
const { sigilKindFor } = useNodePresentation();

// Node type never changes for the lifetime of a mounted marker (the parent's v-for
// :key is tied to the cell, so a genuinely different node always mounts a fresh
// instance) — computed once, non-reactively.
const spec = geometrySpecFor(sigilKindFor(props.node));
const geometry = spec.geometryFactory();

const resolved = computed(() => props.node.state === 'Resolved');

// Three.js has no per-mesh grayscale()/opacity()-as-CSS-filter equivalent — the ghost/
// resolved looks are approximated by desaturating the material color and dropping
// emissive intensity, mirroring the CSS .tgrid__node-icon--ghost/--resolved classes.
const materialColor = computed(() => {
  const base = new THREE.Color(spec.materialParams.color as THREE.ColorRepresentation);
  if (resolved.value) return base.clone().lerp(new THREE.Color(0x777777), 0.6);
  if (props.ghost) return base.clone().lerp(new THREE.Color(0x999999), 0.35);
  return base;
});

const materialOpacity = computed(() => {
  if (resolved.value) return 0.35;
  if (props.ghost) return 0.55;
  return 1;
});

const emissiveIntensity = computed(() => {
  if (resolved.value) return 0;
  const base = spec.materialParams.emissiveIntensity ?? 0;
  return props.ghost ? base * 0.4 : base;
});

const position = computed<[number, number, number]>(() => [
  props.x * TILE_SIZE,
  props.height * HEIGHT_UNIT + 0.35 * spec.scale,
  props.y * TILE_SIZE,
]);

const meshRef = ref<THREE.Mesh | null>(null);

// Assigned directly on the raw THREE.Mesh instance rather than via a template
// <primitive attach="geometry">: TresJS's patchProp has no special case for a
// primitive's `object` prop outside of initial element creation, so passing both
// `object` and `attach` as template props crashes on mount (it falls through to a
// generic "set buffer attribute" branch and tries to spread the geometry instance
// as if it were a plain array).
onMounted(() => {
  if (meshRef.value) meshRef.value.geometry = geometry;
});

if (!props.reducedMotion) {
  const { onBeforeRender } = useLoop();
  onBeforeRender(({ elapsed }) => {
    if (resolved.value) return;
    const mesh = meshRef.value;
    if (!mesh) return;
    mesh.scale.setScalar(spec.scale * (1 + Math.sin(elapsed * 3) * 0.06));
  });
}
</script>

<template>
  <TresMesh ref="meshRef" :position="position" :scale="spec.scale" @click="emit('click')">
    <TresMeshStandardMaterial
      :color="materialColor"
      :roughness="spec.materialParams.roughness"
      :metalness="spec.materialParams.metalness"
      :emissive="spec.materialParams.emissive"
      :emissive-intensity="emissiveIntensity"
      :transparent="materialOpacity < 1"
      :opacity="materialOpacity"
    />
  </TresMesh>
</template>
