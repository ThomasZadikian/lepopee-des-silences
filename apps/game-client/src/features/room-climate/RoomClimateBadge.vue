<script setup lang="ts">
import { computed } from 'vue';

import type { RoomClimateStateDto } from '../runs/types/runTypes';
import { resolveRoomClimateDisplay } from './roomClimateDisplay';

const props = defineProps<{
  climate?: RoomClimateStateDto | null;
}>();

const emit = defineEmits<{
  open: [];
}>();

const display = computed(() => resolveRoomClimateDisplay(props.climate));
</script>

<template>
  <button
    class="es-chip es-chip--frost rcb-root"
    :class="{ 'rcb-root--empty': !display }"
    :title="display ? `Climat actif : ${display.displayName}` : 'Aucun climat actif dans cette Room.'"
    @click="emit('open')"
  >
    <span class="rcb-root__label">Climat</span>
    <span>{{ display?.displayName ?? 'Aucun' }}</span>
  </button>
</template>

<style scoped>
.rcb-root {
  cursor: pointer;
  gap: 4px;
}

.rcb-root--empty {
  opacity: 0.72;
}

.rcb-root__label {
  color: var(--ink-4);
}
</style>
