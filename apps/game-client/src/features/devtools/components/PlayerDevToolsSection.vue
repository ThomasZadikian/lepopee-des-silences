<script setup lang="ts">
import { ref } from 'vue';
import type { PlayerCharacterView } from '../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../party/types/skillTypes';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  characters: PlayerCharacterView[];
  allSkills: SkillDefinitionView[];
}>();

const emit = defineEmits<{
  unlockSkill: [characterId: string, skillKey: string];
  awardStatPoints: [amount: number];
}>();

const selectedCharacterId = ref('');
const selectedSkillKey = ref('');
const statPointAmount = ref(1);

function clampAmount(): number {
  return Math.max(1, Math.min(20, Number(statPointAmount.value) || 1));
}

function unlockSkill() {
  const characterId = selectedCharacterId.value || props.characters[0]?.id;
  if (!characterId || !selectedSkillKey.value) return;
  emit('unlockSkill', characterId, selectedSkillKey.value);
}
</script>

<template>
  <div class="devtools-card">
    <h3>Joueur</h3>

    <label v-if="characters.length > 1" class="devtools-label">
      Personnage
      <select v-model="selectedCharacterId" class="devtools-input">
        <option v-for="c in characters" :key="c.id" :value="c.id">{{ c.displayName }}</option>
      </select>
    </label>

    <label class="devtools-label">
      Sort à débloquer
      <select v-model="selectedSkillKey" class="devtools-input">
        <option value="" disabled>— choisir un sort —</option>
        <option v-for="s in allSkills" :key="s.key" :value="s.key">{{ s.displayName }}</option>
      </select>
    </label>
    <button
      class="devtools-btn"
      :disabled="disabled || isLoading || !selectedSkillKey || characters.length === 0"
      @click="unlockSkill"
    >
      Débloquer le sort
    </button>

    <div class="devtools-inline-form">
      <input v-model.number="statPointAmount" class="devtools-input devtools-input--small" type="number" min="1" max="20">
      <button class="devtools-btn" :disabled="disabled || isLoading" @click="emit('awardStatPoints', clampAmount())">
        + Points de compétence
      </button>
    </div>
  </div>
</template>
