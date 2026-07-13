<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { skillsApi } from '../../../party/api/skillsApi';

const props = defineProps<{ character: PlayerCharacterView }>();

const playerStore = usePlayerStore();

const allSkills = ref<SkillDefinitionView[]>([]);
const isLoadingSkills = ref(false);
const loadError = ref<string | null>(null);

onMounted(async () => {
  isLoadingSkills.value = true;
  loadError.value = null;
  try {
    const response = await skillsApi.listActive();
    allSkills.value = response.skills;
  } catch (caught) {
    loadError.value = caught instanceof Error ? caught.message : 'Chargement des sorts impossible.';
  } finally {
    isLoadingSkills.value = false;
  }
});

const categoryLabels: Record<string, string> = {
  Damage: 'Offensif',
  Guard: 'Défensif',
  Heal: 'Soins',
  Buff: 'Soutien',
  Debuff: 'Affaiblissement',
  Weaken: 'Affaiblissement',
  Disrupt: 'Perturbation',
  Status: 'Statut',
  CopySkills: 'Copie de sorts',
  ExtendDotDuration: 'Durée de dot',
};

function categoryLabel(effectType: string): string {
  return categoryLabels[effectType] ?? effectType;
}

function skillMeta(key: string): SkillDefinitionView | null {
  return allSkills.value.find((s) => s.key === key) ?? null;
}

const equippedSkills = computed(() => props.character.skills.filter((s) => s.isEquipped));
const knownSkills = computed(() => props.character.skills);
const knownKeys = computed(() => new Set(props.character.skills.map((s) => s.skillKey)));
// Scoped to the character being managed here — NOT playerStore's protagonist-only getter,
// otherwise a companion's loadout stayed stuck at "full" (or "empty") based on the
// protagonist's own equipped count instead of its own.
const isLoadoutFull = computed(() => equippedSkills.value.length >= props.character.maxEquippedSkills);

function toggleSkill(skillKey: string, isEquipped: boolean) {
  if (playerStore.isLoading) return;
  if (isEquipped) {
    playerStore.unequipSkill(props.character.id, skillKey);
  } else {
    if (isLoadoutFull.value) return;
    playerStore.equipSkill(props.character.id, skillKey);
  }
}
</script>

<template>
  <div class="smk-root">
    <p v-if="playerStore.error" class="smk-error">{{ playerStore.error }}</p>
    <p v-if="loadError" class="smk-error">{{ loadError }}</p>

    <!-- ── Section 1: currently equipped ── -->
    <section class="smk-section">
      <h4 class="smk-section__title">
        Sorts équipés
        <span class="smk-section__count">{{ equippedSkills.length }} / {{ character.maxEquippedSkills }}</span>
      </h4>
      <ul v-if="equippedSkills.length" class="smk-list">
        <li v-for="skill in equippedSkills" :key="skill.skillKey" class="smk-row">
          <div class="smk-row__info">
            <span class="smk-row__name">{{ skillMeta(skill.skillKey)?.displayName ?? skill.skillKey }}</span>
            <span v-if="skillMeta(skill.skillKey)" class="es-chip">{{ categoryLabel(skillMeta(skill.skillKey)!.effectType) }}</span>
          </div>
          <button type="button" class="smk-toggle smk-toggle--active" :disabled="playerStore.isLoading" @click="toggleSkill(skill.skillKey, true)">
            Déséquiper
          </button>
        </li>
      </ul>
      <p v-else class="smk-empty">Aucun sort équipé.</p>
    </section>

    <!-- ── Section 2: known by the player ── -->
    <section class="smk-section">
      <h4 class="smk-section__title">Sorts connus</h4>
      <ul v-if="knownSkills.length" class="smk-list">
        <li v-for="skill in knownSkills" :key="skill.skillKey" class="smk-row">
          <div class="smk-row__info">
            <span class="smk-row__name">{{ skillMeta(skill.skillKey)?.displayName ?? skill.skillKey }}</span>
            <span v-if="skillMeta(skill.skillKey)" class="es-chip">{{ categoryLabel(skillMeta(skill.skillKey)!.effectType) }}</span>
          </div>
          <button
            type="button"
            class="smk-toggle"
            :class="{ 'smk-toggle--active': skill.isEquipped }"
            :disabled="playerStore.isLoading || (!skill.isEquipped && isLoadoutFull)"
            @click="toggleSkill(skill.skillKey, skill.isEquipped)"
          >
            {{ skill.isEquipped ? 'Équipé' : 'Équiper' }}
          </button>
        </li>
      </ul>
      <p v-else class="smk-empty">Aucun sort connu.</p>
    </section>

    <!-- ── Section 3: every skill in the game ── -->
    <section class="smk-section">
      <h4 class="smk-section__title">Tous les sorts du jeu</h4>
      <p v-if="isLoadingSkills" class="smk-empty">Chargement…</p>
      <ul v-else class="smk-list">
        <li v-for="skill in allSkills" :key="skill.key" class="smk-row smk-row--detailed">
          <div class="smk-row__info">
            <span class="smk-row__name">{{ skill.displayName }}</span>
            <span class="es-chip">{{ categoryLabel(skill.effectType) }}</span>
            <span class="es-chip" :class="{ 'es-chip--frost': knownKeys.has(skill.key) }">
              {{ knownKeys.has(skill.key) ? 'Connu' : 'Inconnu' }}
            </span>
          </div>
          <p class="smk-row__desc">{{ skill.description }}</p>
          <div class="smk-row__stats">
            <span>Puissance : {{ skill.basePower }}</span>
            <span v-if="skill.manaCost > 0">Mana : {{ skill.manaCost }}</span>
            <span v-if="skill.chargeCost > 0">Charge : {{ skill.chargeCost }}</span>
          </div>
        </li>
      </ul>
    </section>
  </div>
</template>

<style scoped>
.smk-root {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.smk-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.smk-section__title {
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  padding-bottom: 6px;
  border-bottom: 1px solid var(--line-soft);
  margin: 0;
}

.smk-section__count {
  float: right;
  font-family: var(--font-mono, monospace);
  color: var(--gold);
}

.smk-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.smk-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 4px;
  background: oklch(0.24 0.015 283 / 0.35);
}

.smk-row--detailed {
  flex-direction: column;
  align-items: stretch;
  gap: 6px;
}

.smk-row__info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.smk-row__name {
  font-size: 13px;
  color: var(--ink-2);
}

.smk-row__desc {
  margin: 0;
  font-size: 12px;
  color: var(--ink-4);
  line-height: 1.4;
}

.smk-row__stats {
  display: flex;
  gap: 12px;
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--ink-3);
}

.smk-toggle {
  flex-shrink: 0;
  font-family: var(--font-caps, var(--font));
  font-size: 9.5px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  padding: 4px 10px;
  border-radius: 3px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-4);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.smk-toggle:disabled {
  opacity: 0.38;
  cursor: not-allowed;
}

.smk-toggle:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.smk-toggle--active {
  border-color: var(--gold);
  color: var(--gold);
  background: oklch(0.55 0.08 85 / 0.12);
}

.smk-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
  margin: 0;
}

.smk-error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood);
  margin: 0;
}
</style>
