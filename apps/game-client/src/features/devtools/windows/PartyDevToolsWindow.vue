<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
}>();

const emit = defineEmits<{
  addAlly: [companionNpcKey: string];
  removeAlly: [];
}>();

const companionOptions: { key: string; label: string }[] = [
  { key: 'npc.thomas', label: 'Thomas' },
  { key: 'npc.mane', label: 'Mané' },
  { key: 'npc.mina', label: 'Mina' },
  { key: 'npc.elise', label: 'Elise' },
  { key: 'npc.john', label: 'John' },
];
const selectedCompanionKey = ref(companionOptions[0]!.key);
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Compagnons</h2>
      <p>Recrute un compagnon avec son vrai kit de combat authoré (roster max 5), ou retire le dernier recruté.</p>
    </header>

    <div class="devtools-window__body">
      <div class="devtools-catalog-grid">
        <button
          v-for="option in companionOptions"
          :key="option.key"
          type="button"
          class="devtools-catalog-cell"
          :class="{ 'devtools-catalog-cell--sel': selectedCompanionKey === option.key }"
          @click="selectedCompanionKey = option.key"
        >
          <span class="devtools-catalog-cell__name">{{ option.label }}</span>
        </button>
      </div>

      <button class="devtools-btn" :disabled="props.disabled || props.isLoading" @click="emit('addAlly', selectedCompanionKey)">
        + Recruter {{ companionOptions.find((o) => o.key === selectedCompanionKey)?.label }}
      </button>
      <button class="devtools-btn devtools-btn--danger" :disabled="props.disabled || props.isLoading" @click="emit('removeAlly')">
        − Retirer le dernier compagnon
      </button>
    </div>
  </div>
</template>
