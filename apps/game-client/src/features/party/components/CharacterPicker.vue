<script setup lang="ts">
import type { PlayerCharacterView } from '../types/playerTypes';
import ChipBadge from '../../../shared/components/ChipBadge.vue';

defineProps<{
  characters: PlayerCharacterView[];
  modelValue: string | null;
}>();

defineEmits<{ 'update:modelValue': [characterId: string] }>();
</script>

<template>
  <div v-if="characters.length > 1" class="character-picker">
    <button
      v-for="character in characters"
      :key="character.id"
      type="button"
      class="character-picker__chip"
      :class="{ 'character-picker__chip--active': modelValue === character.id }"
      @click="$emit('update:modelValue', character.id)"
    >
      {{ character.displayName }}
      <ChipBadge v-if="character.characterType === 'Companion'" tone="mint">Compagnon</ChipBadge>
    </button>
  </div>
</template>

<style scoped>
.character-picker {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.character-picker__chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 12px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  font-family: var(--font-mono);
  font-size: 11px;
  cursor: pointer;
  transition: color .15s, border-color .15s;
}

.character-picker__chip--active {
  border-color: var(--mint-dim);
  color: var(--mint-dim);
}
</style>
