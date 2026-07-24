<script setup lang="ts">
import { computed } from 'vue';
import type { RoomDto, RoomGridDto } from '../../runs/types/runTypes';
import { useSceneDecor } from '../composables/useSceneDecor';
import DecorPart3D from './DecorPart3D.vue';

const props = defineProps<{
  room: RoomDto;
  grid: RoomGridDto | null;
  accentColor: number;
}>();

const room = computed(() => props.room);
const grid = computed(() => props.grid);
const accentColor = computed(() => props.accentColor);

const { decorProps } = useSceneDecor(room, grid, accentColor);
</script>

<template>
  <!-- Keyed by room id (not just index) so a room change fully remounts each prop
       cluster instead of reactively re-patching it in place — see DecorPart3D's own
       comment for why that matters for the geometry assignment underneath. -->
  <TresGroup
    v-for="(prop, i) in decorProps"
    :key="`${room.id}-${i}`"
    :position="prop.position"
    :rotation="[0, prop.rotationY, 0]"
    :scale="prop.scale"
  >
    <DecorPart3D v-for="(part, j) in prop.parts" :key="j" :part="part" />
  </TresGroup>
</template>
