<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  advanceRoom: [];
  advanceRooms: [count: number];
}>();

const roomCount = ref(3);

function clampRoomCount(): number {
  return Math.min(10, Math.max(1, Number(roomCount.value) || 1));
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Run</h2>
      <p>Avance la run d'une ou plusieurs salles sans jouer les nœuds.</p>
    </header>

    <div class="devtools-window__body">
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('advanceRoom')">
        Avancer d'une salle
      </button>

      <label class="devtools-label">
        Nombre de salles (max 10)
        <div class="devtools-inline-form">
          <input v-model.number="roomCount" class="devtools-input devtools-input--small" type="number" min="1" max="10">
          <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('advanceRooms', clampRoomCount())">
            Avancer de {{ clampRoomCount() }} salle(s)
          </button>
        </div>
      </label>
    </div>
  </div>
</template>
