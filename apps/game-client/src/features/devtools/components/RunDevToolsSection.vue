<script setup lang="ts">
import { ref } from 'vue';

import type { PalaceRoomStateKey, RoomClimateKey } from '../types/devToolsTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  advanceRoom: [];
  advanceRooms: [count: number];
  forcePalaceState: [state: PalaceRoomStateKey];
  forceClimate: [climate: RoomClimateKey];
  activateLaw: [lawKey: string];
  clearLaws: [];
  activateCurse: [curseKey: string];
  clearCurses: [];
}>();

const roomCount = ref(3);
const palaceState = ref<PalaceRoomStateKey>('Silent');
const climate = ref<RoomClimateKey>('Heatwave');
const lawKey = ref('');
const curseKey = ref('');

const palaceStates: PalaceRoomStateKey[] = ['Neutral', 'Silent', 'Painful', 'Enraged', 'Violent'];
const climates: RoomClimateKey[] = ['None', 'Grey', 'Rain', 'Heatwave', 'Hail'];

function clampRoomCount(): number {
  return Math.min(10, Math.max(1, Number(roomCount.value) || 1));
}

function confirmAndClearLaws() {
  if (window.confirm('Confirmer clear laws ?')) emit('clearLaws');
}

function confirmAndClearCurses() {
  if (window.confirm('Confirmer clear curses ?')) emit('clearCurses');
}
</script>

<template>
  <section class="devtools-grid">
    <div class="devtools-card">
      <h3>Run</h3>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('advanceRoom')">
        Advance room
      </button>
      <div class="devtools-inline-form">
        <input v-model.number="roomCount" class="devtools-input devtools-input--small" type="number" min="1" max="10">
        <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('advanceRooms', clampRoomCount())">
          Advance N rooms
        </button>
      </div>
    </div>

    <div class="devtools-card">
      <h3>Room</h3>
      <label class="devtools-label">
        PalaceRoomState
        <select v-model="palaceState" class="devtools-input">
          <option v-for="state in palaceStates" :key="state" :value="state">{{ state }}</option>
        </select>
      </label>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('forcePalaceState', palaceState)">
        Force state
      </button>

      <label class="devtools-label">
        RoomClimate
        <select v-model="climate" class="devtools-input">
          <option v-for="entry in climates" :key="entry" :value="entry">{{ entry }}</option>
        </select>
      </label>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('forceClimate', climate)">
        Force climate
      </button>
    </div>

    <div class="devtools-card">
      <h3>Laws</h3>
      <input v-model="lawKey" class="devtools-input" placeholder="law-aegis-v1">
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading || !lawKey.trim()" @click="emit('activateLaw', lawKey.trim())">
        Activate law
      </button>
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmAndClearLaws">
        Clear laws
      </button>
    </div>

    <div class="devtools-card">
      <h3>Curses</h3>
      <input v-model="curseKey" class="devtools-input" placeholder="curse.old-wound">
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading || !curseKey.trim()" @click="emit('activateCurse', curseKey.trim())">
        Activate curse
      </button>
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmAndClearCurses">
        Clear curses
      </button>
    </div>
  </section>
</template>
