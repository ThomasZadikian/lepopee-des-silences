<script setup lang="ts">
import { onMounted, ref } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';
import type { ItemDefinitionView } from '../../../party/types/itemTypes';
import { statDescriptions, statLabels, statOrder, statValue } from '../../../party/constants/statDescriptions';
import { skillsApi } from '../../../party/api/skillsApi';
import { itemsApi } from '../../../party/api/itemsApi';
import StatTooltip from '../../../../shared/components/StatTooltip.vue';
import ChipBadge from '../../../../shared/components/ChipBadge.vue';

defineProps<{ characters: PlayerCharacterView[] }>();

const allSkills = ref<SkillDefinitionView[]>([]);
const allItems = ref<ItemDefinitionView[]>([]);

onMounted(() => {
  // Each catalog lookup is best-effort and independent: a slow/unavailable item
  // endpoint must not make the skill names regress to raw keys (or vice versa).
  void skillsApi.listActive()
    .then((response) => { allSkills.value = response.skills; })
    .catch(() => undefined);
  void itemsApi.listActive()
    .then((response) => { allItems.value = response.items; })
    .catch(() => undefined);
});

function skillDisplayName(skillKey: string): string {
  return allSkills.value.find((s) => s.key === skillKey)?.displayName ?? skillKey;
}

function skillContract(skillKey: string): string | null {
  const skill = allSkills.value.find((candidate) => candidate.key === skillKey);
  if (!skill) return null;
  const sight = skill.requiresLineOfSight ? ' · vue' : '';
  const cooldown = (skill.cooldown ?? 0) > 0 ? ` · recharge ${skill.cooldown}` : '';
  return `P${skill.tacticalRange ?? 1} · ${skill.tacticalAreaShape ?? 'Single'}${sight}${cooldown}`;
}

function itemDisplayName(itemKey: string): string {
  return allItems.value.find((item) => item.key === itemKey)?.displayName ?? itemKey;
}

function itemSlotLabel(slot: string | undefined): string {
  return slot === 'Weapon' ? 'Arme' : slot === 'Accessory' ? 'Accessoire' : 'Relique';
}
</script>

<template>
  <div class="tov-root">
    <article v-for="character in characters" :key="character.id" class="tov-card">
      <header class="tov-card__header">
        <span class="tov-card__name">
          {{ character.displayName }}
          <ChipBadge v-if="character.characterType === 'Companion'" tone="mint">Compagnon</ChipBadge>
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
        <div class="tov-stat tov-stat--tactical">
          <span class="tov-stat__label">Déplacement</span>
          <span class="tov-stat__value">{{ character.stats.movement ?? 4 }}</span>
        </div>
        <div class="tov-stat tov-stat--resource">
          <span class="tov-stat__label">Charge</span>
          <span class="tov-stat__value">0 / 5</span>
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
            <span>{{ skillDisplayName(skill.skillKey) }}</span>
            <small v-if="skillContract(skill.skillKey)" class="tov-card__chip-meta">
              {{ skillContract(skill.skillKey) }}
            </small>
          </span>
          <span v-if="!character.skills.some((s) => s.isEquipped)" class="tov-empty">
            Aucun sort équipé.
          </span>
        </div>
      </div>

      <div class="tov-card__skills">
        <span class="es-label">Équipement</span>
        <div class="tov-card__skill-chips">
          <span
            v-for="item in character.items.filter((candidate) => candidate.isEquipped)"
            :key="item.itemKey"
            class="es-chip tov-card__equipment-chip"
          >
            <span>{{ itemDisplayName(item.itemKey) }}</span>
            <small class="tov-card__chip-meta">{{ itemSlotLabel(item.slot) }}</small>
          </span>
          <span v-if="!character.items.some((item) => item.isEquipped)" class="tov-empty">
            Aucun objet équipé.
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
  background: var(--panel-2);
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
  font-family: var(--font-display);
  font-style: italic;
  font-size: 18px;
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
  background: var(--panel);
}

.tov-stat__label {
  font-size: 11px;
  color: var(--ink-4);
}

.tov-stat__value {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--ink-2);
}

.tov-stat--tactical,
.tov-stat--resource {
  border: 1px solid var(--line-soft);
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

.tov-card__skill-chips .es-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.tov-card__chip-meta {
  opacity: 0.6;
  font-family: var(--font-mono);
  font-size: 9px;
}

.tov-card__equipment-chip {
  border-color: var(--mint-dim);
}

.tov-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
}
</style>
