<script setup lang="ts">
import { computed, ref } from 'vue';
import { useLoop } from '@tresjs/core';
import { hashSeed } from '../composables/usePalaceTerrain';
import { getFogSpriteTexture } from './fogTexture';
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
// whichever renderer draws it — plus a third seed (fog3), new to the 3D renderer, used
// only to scatter the extra billboard layers below (the CSS renderer has no equivalent
// of layering multiple sprites, so nothing there depends on this one matching).
const fogSeed = hashSeed(`${props.roomId}:fog:${props.x}:${props.y}`);
const fogSeed2 = hashSeed(`${props.roomId}:fog2:${props.x}:${props.y}`);
const fogSeed3 = hashSeed(`${props.roomId}:fog3:${props.x}:${props.y}`);

const jitterX = ((fogSeed % 41) - 20) / 20;
const jitterZ = ((Math.floor(fogSeed / 41) % 41) - 20) / 20;
const cloudScale = 0.6 + ((fogSeed2 % 100) / 100) * 0.5;
const driftPhase = ((fogSeed % 1000) / 1000) * Math.PI * 2;

const basePosition: [number, number, number] = [
  props.x * TILE_SIZE + jitterX * 0.25,
  0.18,
  props.y * TILE_SIZE + jitterZ * 0.25,
];

// Real fog reads as an irregular, layered puff, not one clean circle — three
// billboards of varying size/opacity/offset, deterministic per cell, stand in for
// that without any actual volumetric rendering.
const puffs = [
  { dx: 0, dz: 0, scale: 1, opacity: 0.34 },
  {
    dx: (((fogSeed % 17) - 8) / 8) * 0.32,
    dz: (((fogSeed2 % 17) - 8) / 8) * 0.32,
    scale: 0.7 + ((fogSeed3 % 40) / 100),
    opacity: 0.24,
  },
  {
    dx: (((fogSeed3 % 23) - 11) / 11) * 0.3,
    dz: (((fogSeed % 23) - 11) / 11) * 0.3,
    scale: 0.55 + ((fogSeed2 % 30) / 100),
    opacity: 0.2,
  },
];

const groupRef = ref<{ position: { x: number; z: number }; rotation: { y: number } } | null>(null);

if (!props.reducedMotion) {
  const { onBeforeRender } = useLoop();
  onBeforeRender(({ elapsed }) => {
    const group = groupRef.value;
    if (!group) return;
    const t = elapsed * 0.25 + driftPhase;
    group.position.x = basePosition[0] + Math.sin(t) * 0.1;
    group.position.z = basePosition[2] + Math.cos(t * 0.8) * 0.1;
  });
}

const colorHex = computed(() => props.fogColor);
const texture = getFogSpriteTexture();
</script>

<template>
  <TresGroup ref="groupRef" :position="basePosition">
    <TresSprite
      v-for="(puff, i) in puffs"
      :key="i"
      :position="[puff.dx, (puff.scale - 1) * 0.05, puff.dz]"
      :scale="[cloudScale * puff.scale, cloudScale * puff.scale, 1]"
    >
      <TresSpriteMaterial
        :map="texture"
        :color="colorHex"
        :transparent="true"
        :opacity="puff.opacity"
        :depth-write="false"
      />
    </TresSprite>
  </TresGroup>
</template>
