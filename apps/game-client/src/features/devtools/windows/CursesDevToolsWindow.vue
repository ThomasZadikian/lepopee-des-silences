<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  activateCurse: [curseKey: string];
  clearCurses: [];
}>();

const curseKey = ref('');

function confirmAndClearCurses() {
  if (window.confirm('Confirmer clear curses ?')) emit('clearCurses');
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Malédictions</h2>
      <p>Active une malédiction par sa clé de catalogue, ou efface toutes les malédictions actives de la run.</p>
    </header>

    <div class="devtools-window__body">
      <label class="devtools-label">
        Clé de la malédiction
        <input v-model="curseKey" class="devtools-input" placeholder="curse.old-wound">
      </label>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading || !curseKey.trim()" @click="emit('activateCurse', curseKey.trim())">
        Activer la malédiction
      </button>
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmAndClearCurses">
        Effacer toutes les malédictions
      </button>
    </div>
  </div>
</template>
