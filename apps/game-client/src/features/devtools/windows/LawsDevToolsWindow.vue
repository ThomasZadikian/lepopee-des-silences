<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  activateLaw: [lawKey: string];
  clearLaws: [];
}>();

const lawKey = ref('');

function confirmAndClearLaws() {
  if (window.confirm('Confirmer clear laws ?')) emit('clearLaws');
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Lois du Palais</h2>
      <p>Active une loi par sa clé de catalogue, ou efface toutes les lois actives de la run.</p>
    </header>

    <div class="devtools-window__body">
      <label class="devtools-label">
        Clé de la loi
        <input v-model="lawKey" class="devtools-input" placeholder="law-aegis-v1">
      </label>
      <button class="devtools-btn" :disabled="props.disabled || props.isLoading || !lawKey.trim()" @click="emit('activateLaw', lawKey.trim())">
        Activer la loi
      </button>
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="confirmAndClearLaws">
        Effacer toutes les lois
      </button>
    </div>
  </div>
</template>
