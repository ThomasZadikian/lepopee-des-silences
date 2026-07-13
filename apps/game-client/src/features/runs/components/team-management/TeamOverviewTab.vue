<script setup lang="ts">
import { onMounted, ref } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';
import { statDescriptions, statLabels, statOrder, statValue } from '../../../party/constants/statDescriptions';
import { skillsApi } from '../../../party/api/skillsApi';
import StatTooltip from '../../../../shared/components/StatTooltip.vue';
import ChipBadge from '../../../../shared/components/ChipBadge.vue';

defineProps<{ characters: PlayerCharacterView[] }>();

const allSkills = ref<SkillDefinitionView[]>([]);

onMounted(async () => {
  try {
    const response = await skillsApi.listActive();
    allSkills.value = response.skills;
  } catch {
    // Best-effort: fall back to raw keys below if the catalog lookup fails.
  }
});

function skillDisplayName(skillKey: string): string {
  return allSkills.value.find((s) => s.key === skillKey)?.displayName ?? skillKey;
}
</script>

<template>
  <div class="tov-root">
    <article v-for="character in characters" :key="character.id" class="tov-card">
      <header class="tov-card__header">
        <span class="tov-card__name">
          {{ character.displayName }}
          <ChipBadge v-if="character.characterType === 'Companion'" tone="gold">Compagnon</ChipBadge>
        </span>
        <span class="es-label">{{ character.definitionKey }}</span>
      </header>

      <div class="tov-card__stats">
        <div v-for="stat in statOrder" :key="stat" class="tov-stat">
          <StatTooltip :text="statDescriptions[stat]">
            <span class="tov-stat__label">{{ statLabels[stat] }}</span>
          </StatTooltip>
          <span class="tov-stat__value">{{ statValue(character.stats, stat) }}</span>
        </div>
      </div>

      <div class="tov-card__skills">
        <span class="es-label">Sorts équipés</span>
        <div class="tov-card__skill-chips">
          <span
            v-for="skill in character.skills.filter((s) => s.isEquipped)"
            :key="skill.skillKey"
            class="es-chip"
          >
            {{ skillDisplayName(skill.skillKey) }}
          </span>
          <span v-if="!character.skills.some((s) => s.isEquipped)" class="tov-empty">
            Aucun sort équipé.
          </span>
        </div>
      </div>
    </article>

    <p v-if="characters.length === 0" class="tov-empty">Aucun personnage disponible.</p>
  </div>
</template>

<style scoped>
.tov-root {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.tov-card {
  padding: 16px 18px;
  border: 1px solid var(--line-soft);
  border-radius: 6px;
  background: oklch(0.24 0.015 283 / 0.4);
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.tov-card__header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
}

.tov-card__name {
  font-family: var(--font-display, var(--font));
  font-size: 18px;
  font-weight: 600;
  color: var(--ink);
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.tov-card__stats {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 10px;
}

.tov-stat {
  display: flex;
  justify-content: space-between;
  gap: 8px;
  padding: 6px 10px;
  border-radius: 4px;
  background: oklch(0.20 0.02 270 / 0.4);
}

.tov-stat__label {
  font-size: 11px;
  color: var(--ink-4);
}

.tov-stat__value {
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  color: var(--ink-2);
}

.tov-card__skills {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.tov-card__skill-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.tov-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
}
</style>
