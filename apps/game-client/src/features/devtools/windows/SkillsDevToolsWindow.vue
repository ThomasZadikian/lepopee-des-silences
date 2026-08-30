<script setup lang="ts">
import { computed, ref } from 'vue';
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
}>();

const search = ref('');
const selectedCharacterId = ref(props.characters[0]?.id ?? '');
const selectedSkillKey = ref<string | null>(null);

const filteredSkills = computed(() => {
  const query = search.value.trim().toLowerCase();
  if (!query) return props.allSkills;
  return props.allSkills.filter((skill) =>
    skill.displayName.toLowerCase().includes(query) ||
    skill.key.toLowerCase().includes(query) ||
    skill.category.toLowerCase().includes(query));
});

const selectedSkill = computed(() =>
  props.allSkills.find((skill) => skill.key === selectedSkillKey.value) ?? null);

function selectSkill(skill: SkillDefinitionView) {
  selectedSkillKey.value = selectedSkillKey.value === skill.key ? null : skill.key;
}

function unlock() {
  const characterId = selectedCharacterId.value || props.characters[0]?.id;
  if (!characterId || !selectedSkillKey.value) return;
  emit('unlockSkill', characterId, selectedSkillKey.value);
}
</script>

<template>
  <div class="devtools-window">
    <header class="devtools-window__head">
      <h2>Sorts</h2>
      <p>Débloque un sort du grimoire directement pour un personnage, sans passer par un PNJ.</p>
    </header>

    <div class="devtools-window__body">
      <label v-if="characters.length > 1" class="devtools-label">
        Personnage cible
        <select v-model="selectedCharacterId" class="devtools-input">
          <option v-for="c in characters" :key="c.id" :value="c.id">{{ c.displayName }}</option>
        </select>
      </label>

      <div class="devtools-catalog-layout">
        <div>
          <div class="devtools-catalog-toolbar">
            <input v-model="search" class="devtools-input" placeholder="Rechercher un sort…">
          </div>
          <p v-if="filteredSkills.length === 0" class="devtools-catalog-empty">Aucun sort trouvé.</p>
          <div v-else class="devtools-catalog-grid">
            <button
              v-for="skill in filteredSkills"
              :key="skill.key"
              type="button"
              class="devtools-catalog-cell"
              :class="{ 'devtools-catalog-cell--sel': selectedSkillKey === skill.key }"
              @click="selectSkill(skill)"
            >
              <span class="devtools-catalog-cell__name">{{ skill.displayName }}</span>
              <span class="devtools-catalog-cell__meta">{{ skill.category }} · {{ skill.skillType }}</span>
            </button>
          </div>
        </div>

        <div class="devtools-catalog-sheet" v-if="selectedSkill">
          <h3 class="devtools-catalog-sheet__name">{{ selectedSkill.displayName }}</h3>
          <p class="devtools-catalog-sheet__desc">{{ selectedSkill.description }}</p>
          <div class="devtools-catalog-sheet__facts">
            <span class="devtools-catalog-fact">{{ selectedSkill.category }}</span>
            <span class="devtools-catalog-fact">{{ selectedSkill.skillType }}</span>
            <span class="devtools-catalog-fact">{{ selectedSkill.targetingType }}</span>
            <span v-if="selectedSkill.manaCost > 0" class="devtools-catalog-fact">{{ selectedSkill.manaCost }} PP</span>
            <span v-if="selectedSkill.chargeCost > 0" class="devtools-catalog-fact">{{ selectedSkill.chargeCost }} charge</span>
            <span v-if="selectedSkill.basePower > 0" class="devtools-catalog-fact">Puissance {{ selectedSkill.basePower }}</span>
            <span v-if="selectedSkill.isUltimate" class="devtools-catalog-fact">Ultime</span>
          </div>
          <button
            class="devtools-btn"
            :disabled="props.disabled || props.isLoading || characters.length === 0"
            @click="unlock"
          >
            Débloquer pour {{ characters.find((c) => c.id === (selectedCharacterId || characters[0]?.id))?.displayName ?? '…' }}
          </button>
        </div>
        <p v-else class="devtools-catalog-empty">Sélectionne un sort pour voir son descriptif.</p>
      </div>
    </div>
  </div>
</template>
