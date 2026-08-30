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
    class="rcb-root"
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
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: .06em;
  text-transform: uppercase;
  padding: 2px 7px;
  border: 1px solid var(--mint-dim);
  color: var(--mint-dim);
  background: transparent;
  cursor: pointer;
  transition: opacity .15s;
}

.rcb-root:hover { opacity: .8; }

.rcb-root--empty {
  border-color: var(--ink-5);
  color: var(--ink-4);
}

.rcb-root__label {
  color: var(--ink-4);
}
</style>
