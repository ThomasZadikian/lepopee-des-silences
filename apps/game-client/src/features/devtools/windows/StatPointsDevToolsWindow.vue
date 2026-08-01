<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  awardStatPoints: [amount: number];
}>();

const amount = ref(1);

function clampAmount(): number {
  return Math.max(1, Math.min(20, Number(amount.value) || 1));
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Points de compétence</h2>
      <p>Accorde des points de compétence au profil joueur, à répartir ensuite dans l'onglet Statistiques.</p>
    </header>

    <div class="devtools-window__body">
      <label class="devtools-label">
        Montant
        <input v-model.number="amount" class="devtools-input" type="number" min="1" max="20">
      </label>
      <button
        class="devtools-btn"
        :disabled="props.disabled || props.isLoading"
        @click="emit('awardStatPoints', clampAmount())"
      >
        Accorder {{ clampAmount() }} point(s)
      </button>
    </div>
  </div>
</template>
