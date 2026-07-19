<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView, SkillEffectView } from '../../../party/types/skillTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { useRunStore } from '../../stores/runStore';
import { skillsApi } from '../../../party/api/skillsApi';

const props = defineProps<{ character: PlayerCharacterView }>();

const playerStore = usePlayerStore();
const runStore = useRunStore();

const allSkills = ref<SkillDefinitionView[]>([]);
const isLoadingSkills = ref(false);
const loadError = ref<string | null>(null);

onMounted(loadSkills);

async function loadSkills() {
  isLoadingSkills.value = true;
  loadError.value = null;
  try {
    const response = await skillsApi.listActive();
    allSkills.value = response.skills;
  } catch (caught) {
    loadError.value = caught instanceof Error ? caught.message : 'Chargement du grimoire impossible.';
  } finally {
    isLoadingSkills.value = false;
  }
}

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

// ── Tri + pagination (2 colonnes × 3 sorts par page) ──
const PAGE_SIZE = 6;
const sortMode = ref<'alphabetical' | 'category'>('alphabetical');
const currentPage = ref(1);

const sortedSkills = computed(() => {
  const list = [...allSkills.value];
  if (sortMode.value === 'category') {
    list.sort((a, b) => {
      const categoryCompare = categoryLabel(a.effectType).localeCompare(categoryLabel(b.effectType));
      return categoryCompare !== 0 ? categoryCompare : a.displayName.localeCompare(b.displayName);
    });
  } else {
    list.sort((a, b) => a.displayName.localeCompare(b.displayName));
  }
  return list;
});

const totalPages = computed(() => Math.max(1, Math.ceil(sortedSkills.value.length / PAGE_SIZE)));

const pagedSkills = computed(() => {
  const start = (currentPage.value - 1) * PAGE_SIZE;
  return sortedSkills.value.slice(start, start + PAGE_SIZE);
});

watch(sortMode, () => { currentPage.value = 1; });
watch(totalPages, (newTotal) => {
  if (currentPage.value > newTotal) currentPage.value = newTotal;
});

function goToPreviousPage() {
  if (currentPage.value > 1) currentPage.value -= 1;
}

function goToNextPage() {
  if (currentPage.value < totalPages.value) currentPage.value += 1;
}

const statLabels: Record<string, string> = {
  AttackPower: 'Attaque',
  Defense: 'Défense',
  MagicAttack: 'Attaque magique',
  MagicDefense: 'Défense magique',
  Speed: 'Vitesse',
  Focus: 'Focus',
  CriticalChanceBonus: 'Chances de critique',
  MaxVitality: 'PV max',
};

const effectKindLabels: Record<string, string> = {
  HealOverTime: 'Soin continu',
  GuardOverTime: 'Garde continue',
  DamageOverTime: 'Dégâts continus',
  Silence: 'Silence',
  GuaranteedCritical: 'Critique garanti',
};

/** Formats one skill effect into a short, player-facing French sentence. */
function formatEffect(effect: SkillEffectView): string {
  const durationSuffix = effect.isPermanent
    ? ' (permanent)'
    : effect.durationTicks > 0
      ? ` (${effect.durationTicks} ticks)`
      : '';

  if (effect.kind === 'StatModifier' && effect.stat) {
    const statLabel = statLabels[effect.stat] ?? effect.stat;
    const sign = effect.magnitude >= 0 ? '+' : '';
    const unit = effect.magnitudeIsPercentOfMax || effect.magnitudeIsPercentOfBaseStat ? '%' : '';
    const target = effect.appliesToActor ? ' (sur soi)' : '';
    return `${statLabel} ${sign}${effect.magnitude}${unit}${target}${durationSuffix}`;
  }

  const label = effectKindLabels[effect.kind] ?? effect.kind;
  if (effect.magnitude === 0) return `${label}${durationSuffix}`;
  const unit = effect.magnitudeIsPercentOfMax || effect.magnitudeIsPercentOfBaseStat ? '%' : '';
  return `${label} ${effect.magnitude}${unit}${durationSuffix}`;
}

const knownKeys = computed(() => new Set(props.character.skills.map((s) => s.skillKey)));

// ── Staged equip/unequip state ("Valider les choix") ──
const pendingEquippedKeys = ref<Set<string>>(new Set());

function resetPendingFromCharacter() {
  pendingEquippedKeys.value = new Set(
    props.character.skills.filter((s) => s.isEquipped).map((s) => s.skillKey),
  );
}

resetPendingFromCharacter();
watch(() => props.character.id, resetPendingFromCharacter);

const hasPendingChanges = computed(() => {
  const committed = new Set(props.character.skills.filter((s) => s.isEquipped).map((s) => s.skillKey));
  if (committed.size !== pendingEquippedKeys.value.size) return true;
  for (const key of committed) {
    if (!pendingEquippedKeys.value.has(key)) return true;
  }
  return false;
});

const isLoadoutFull = computed(
  () => pendingEquippedKeys.value.size >= props.character.maxEquippedSkills,
);

function togglePending(skillKey: string) {
  const next = new Set(pendingEquippedKeys.value);
  if (next.has(skillKey)) {
    next.delete(skillKey);
  } else {
    if (isLoadoutFull.value) return;
    next.add(skillKey);
  }
  pendingEquippedKeys.value = next;
}

const isSaving = ref(false);
const saveError = ref<string | null>(null);

async function validateChoices() {
  const committed = new Set(props.character.skills.filter((s) => s.isEquipped).map((s) => s.skillKey));
  const toEquip = [...pendingEquippedKeys.value].filter((key) => !committed.has(key));
  const toUnequip = [...committed].filter((key) => !pendingEquippedKeys.value.has(key));

  if (toEquip.length === 0 && toUnequip.length === 0) return;

  isSaving.value = true;
  saveError.value = null;
  try {
    for (const key of toUnequip) {
      await playerStore.unequipSkill(props.character.id, key);
    }
    for (const key of toEquip) {
      await playerStore.equipSkill(props.character.id, key);
    }
    if (playerStore.error) {
      saveError.value = playerStore.error;
      return;
    }
    if (runStore.currentRun) {
      await runStore.syncPartySkills();
    }
    resetPendingFromCharacter();
  } finally {
    isSaving.value = false;
  }
}

function cancelChoices() {
  resetPendingFromCharacter();
}
</script>

<template>
  <div class="grimoire-root">
    <p v-if="loadError" class="grimoire-error">{{ loadError }}</p>
    <p v-if="saveError" class="grimoire-error">{{ saveError }}</p>

    <header class="grimoire-header">
      <h4 class="grimoire-title">
        Grimoire
        <span class="grimoire-count">{{ pendingEquippedKeys.size }} / {{ character.maxEquippedSkills }} équipés</span>
      </h4>
      <div class="grimoire-actions">
        <button
          type="button"
          class="grimoire-btn grimoire-btn--ghost"
          :disabled="!hasPendingChanges || isSaving"
          @click="cancelChoices"
        >
          Annuler
        </button>
        <button
          type="button"
          class="grimoire-btn grimoire-btn--primary"
          :disabled="!hasPendingChanges || isSaving"
          @click="validateChoices"
        >
          {{ isSaving ? 'Validation…' : 'Valider les choix' }}
        </button>
      </div>
    </header>

    <div v-if="!isLoadingSkills" class="grimoire-toolbar">
      <label class="grimoire-sort">
        Trier par
        <select v-model="sortMode" class="grimoire-sort__select">
          <option value="alphabetical">Alphabétique</option>
          <option value="category">Catégorie d'effet</option>
        </select>
      </label>
    </div>

    <p v-if="isLoadingSkills" class="grimoire-empty">Chargement du grimoire…</p>
    <div v-else class="grimoire-grid">
      <div
        v-for="skill in pagedSkills"
        :key="skill.key"
        class="grimoire-card"
        :class="{
          'grimoire-card--locked': !knownKeys.has(skill.key),
          'grimoire-card--equipped': pendingEquippedKeys.has(skill.key),
        }"
      >
        <div class="grimoire-card__head">
          <span class="grimoire-card__name">{{ skill.displayName }}</span>
          <span class="grimoire-chip">{{ categoryLabel(skill.effectType) }}</span>
          <span class="grimoire-chip grimoire-chip--muted">{{ skill.category === 'Magic' ? 'Magique' : 'Physique' }}</span>
        </div>

        <p class="grimoire-card__desc">{{ skill.description }}</p>

        <ul v-if="skill.effects.length" class="grimoire-effects">
          <li v-for="(effect, index) in skill.effects" :key="index">{{ formatEffect(effect) }}</li>
        </ul>

        <div class="grimoire-card__stats">
          <span>Puissance : {{ skill.basePower }}{{ skill.basePowerIsPercentOfMaxVitality ? '% PV max' : '' }}</span>
          <span v-if="skill.manaCost > 0">Mana : {{ skill.manaCost }}</span>
          <span v-if="skill.chargeCost > 0">Charge : {{ skill.chargeCost }}</span>
        </div>

        <template v-if="knownKeys.has(skill.key)">
          <button
            type="button"
            class="grimoire-toggle"
            :class="{ 'grimoire-toggle--active': pendingEquippedKeys.has(skill.key) }"
            :disabled="!pendingEquippedKeys.has(skill.key) && isLoadoutFull"
            @click="togglePending(skill.key)"
          >
            {{ pendingEquippedKeys.has(skill.key) ? 'Équipé' : 'Équiper' }}
          </button>
        </template>
        <template v-else>
          <p class="grimoire-lock-hint">
            <template v-if="skill.acquisitionHints.length">
              {{ skill.acquisitionHints.join(' · ') }}
            </template>
            <template v-else>Non débloqué.</template>
          </p>
        </template>
      </div>
    </div>

    <div v-if="!isLoadingSkills" class="grimoire-pagination">
      <button
        type="button"
        class="grimoire-page-btn"
        :disabled="currentPage <= 1"
        @click="goToPreviousPage"
      >
        ‹ Précédent
      </button>
      <span class="grimoire-page-indicator">Page {{ currentPage }} / {{ totalPages }}</span>
      <button
        type="button"
        class="grimoire-page-btn"
        :disabled="currentPage >= totalPages"
        @click="goToNextPage"
      >
        Suivant ›
      </button>
    </div>
  </div>
</template>

<style scoped>
.grimoire-root {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.grimoire-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--line-soft);
}

.grimoire-title {
  font-family: var(--font-caps, var(--font));
  font-size: 12px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 10px;
}

.grimoire-count {
  font-family: var(--font-mono, monospace);
  color: var(--gold);
  text-transform: none;
  letter-spacing: normal;
}

.grimoire-actions {
  display: flex;
  gap: 8px;
}

.grimoire-btn {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  padding: 6px 14px;
  border-radius: 4px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.grimoire-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.grimoire-btn--ghost:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.grimoire-btn--primary {
  border-color: var(--gold);
  color: var(--gold);
}

.grimoire-btn--primary:not(:disabled):hover {
  background: oklch(0.55 0.08 85 / 0.12);
}

.grimoire-toolbar {
  display: flex;
  justify-content: flex-end;
}

.grimoire-sort {
  display: flex;
  align-items: center;
  gap: 8px;
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.grimoire-sort__select {
  font-family: var(--font, sans-serif);
  font-size: 12px;
  text-transform: none;
  letter-spacing: normal;
  padding: 4px 8px;
  border-radius: 4px;
  border: 1px solid var(--line-soft);
  background: var(--panel, oklch(0.20 0.025 270));
  color: var(--ink-2);
}

.grimoire-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

.grimoire-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding-top: 8px;
}

.grimoire-page-btn {
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  padding: 6px 14px;
  border-radius: 4px;
  border: 1px solid var(--line-soft);
  background: transparent;
  color: var(--ink-3);
  cursor: pointer;
  transition: opacity 0.15s, border-color 0.15s, color 0.15s;
}

.grimoire-page-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.grimoire-page-btn:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.grimoire-page-indicator {
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  color: var(--ink-3);
}

.grimoire-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
  border-radius: 6px;
  border: 1px solid var(--line-soft);
  background: oklch(0.24 0.015 283 / 0.35);
}

.grimoire-card--equipped {
  border-color: var(--gold);
}

.grimoire-card--locked {
  opacity: 0.5;
  filter: grayscale(0.6);
}

.grimoire-card__head {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.grimoire-card__name {
  font-size: 13px;
  font-weight: 600;
  color: var(--ink-2);
}

.grimoire-chip {
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  padding: 1px 6px;
  border-radius: 999px;
  border: 1px solid var(--line-soft);
  color: var(--ink-3);
}

.grimoire-chip--muted {
  color: var(--ink-4);
}

.grimoire-card__desc {
  margin: 0;
  font-size: 12px;
  color: var(--ink-4);
  line-height: 1.4;
}

.grimoire-effects {
  margin: 0;
  padding-left: 16px;
  font-size: 11px;
  color: var(--frost, var(--ink-3));
  line-height: 1.5;
}

.grimoire-card__stats {
  display: flex;
  gap: 12px;
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--ink-3);
}

.grimoire-lock-hint {
  margin: 0;
  font-size: 11px;
  font-style: italic;
  color: var(--ink-4);
}

.grimoire-toggle {
  align-self: flex-start;
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

.grimoire-toggle:disabled {
  opacity: 0.38;
  cursor: not-allowed;
}

.grimoire-toggle:not(:disabled):hover {
  border-color: var(--ink-3);
  color: var(--ink-2);
}

.grimoire-toggle--active {
  border-color: var(--gold);
  color: var(--gold);
  background: oklch(0.55 0.08 85 / 0.12);
}

.grimoire-empty {
  font-size: 12px;
  color: var(--ink-4);
  font-style: italic;
  margin: 0;
}

.grimoire-error {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--blood);
  margin: 0;
}
</style>
