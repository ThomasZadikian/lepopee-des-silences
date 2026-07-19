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
      <ChipBadge v-if="character.characterType === 'Companion'" tone="gold">Compagnon</ChipBadge>
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
  border-radius: 999px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  font-size: 12px;
  cursor: pointer;
}

.character-picker__chip--active {
  border-color: var(--frost);
  color: var(--frost);
}
</style>
