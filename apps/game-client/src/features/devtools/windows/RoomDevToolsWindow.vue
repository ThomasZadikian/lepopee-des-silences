<script setup lang="ts">
import { ref } from 'vue';
import type { PalaceRoomStateKey, RoomClimateKey } from '../types/devToolsTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  forcePalaceState: [state: PalaceRoomStateKey];
  forceClimate: [climate: RoomClimateKey];
}>();

const palaceState = ref<PalaceRoomStateKey>('Silent');
const climate = ref<RoomClimateKey>('Heatwave');

const palaceStates: PalaceRoomStateKey[] = ['Neutral', 'Silent', 'Painful', 'Enraged', 'Violent'];
const climates: RoomClimateKey[] = ['None', 'Grey', 'Rain', 'Heatwave', 'Hail'];
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Salle</h2>
      <p>Force l'état du Palais ou le climat de la salle courante.</p>
    </header>

    <div class="devtools-window__body">
      <label class="devtools-label">
        État du Palais
        <select v-model="palaceState" class="devtools-input">
          <option v-for="state in palaceStates" :key="state" :value="state">{{ state }}</option>
        </select>
      </label>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('forcePalaceState', palaceState)">
        Forcer cet état
      </button>

      <label class="devtools-label">
        Climat
        <select v-model="climate" class="devtools-input">
          <option v-for="entry in climates" :key="entry" :value="entry">{{ entry }}</option>
        </select>
      </label>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('forceClimate', climate)">
        Forcer ce climat
      </button>
    </div>
  </div>
</template>
