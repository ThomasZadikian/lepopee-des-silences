<script setup lang="ts">
import { computed, onMounted, ref, watch, type ComputedRef } from 'vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { useRunStore } from '../../stores/runStore';
import { skillsApi } from '../../../party/api/skillsApi';
import { categoryLabel } from './grimoireDisplay';
import GrimoireSkillCard from './GrimoireSkillCard.vue';
import GrimoirePaginationControls from './GrimoirePaginationControls.vue';

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

// ── Recherche + tri ──
const PAGE_SIZE = 6;
const searchQuery = ref('');
const sortMode = ref<'alphabetical' | 'category'>('alphabetical');

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

const filteredSkills = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return sortedSkills.value;
  return sortedSkills.value.filter((skill) => skill.displayName.toLowerCase().includes(query));
});

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

// ── Sections : Équipés / Disponibles / Verrouillés ──
const equippedSkills = computed(() =>
  filteredSkills.value.filter((skill) => pendingEquippedKeys.value.has(skill.key)),
);
const availableSkills = computed(() =>
  filteredSkills.value.filter((skill) => knownKeys.value.has(skill.key) && !pendingEquippedKeys.value.has(skill.key)),
);
const lockedSkills = computed(() =>
  filteredSkills.value.filter((skill) => !knownKeys.value.has(skill.key)),
);

function useSectionPagination(items: ComputedRef<SkillDefinitionView[]>) {
  const currentPage = ref(1);
  const totalPages = computed(() => Math.max(1, Math.ceil(items.value.length / PAGE_SIZE)));
  const pageItems = computed(() => {
    const start = (currentPage.value - 1) * PAGE_SIZE;
    return items.value.slice(start, start + PAGE_SIZE);
  });

  watch(totalPages, (newTotal) => {
    if (currentPage.value > newTotal) currentPage.value = newTotal;
  });

  function goToPreviousPage() {
    if (currentPage.value > 1) currentPage.value -= 1;
  }

  function goToNextPage() {
    if (currentPage.value < totalPages.value) currentPage.value += 1;
  }

  return { currentPage, totalPages, pageItems, goToPreviousPage, goToNextPage };
}

const equippedPage = useSectionPagination(equippedSkills);
const availablePage = useSectionPagination(availableSkills);
const lockedPage = useSectionPagination(lockedSkills);

watch([sortMode, searchQuery], () => {
  equippedPage.currentPage.value = 1;
  availablePage.currentPage.value = 1;
  lockedPage.currentPage.value = 1;
});

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
      <input
        v-model="searchQuery"
        type="search"
        class="grimoire-search"
        placeholder="Rechercher un sort…"
      />
      <label class="grimoire-sort">
        Trier par
        <select v-model="sortMode" class="grimoire-sort__select">
          <option value="alphabetical">Alphabétique</option>
          <option value="category">Catégorie d'effet</option>
        </select>
      </label>
    </div>

    <p v-if="isLoadingSkills" class="grimoire-empty">Chargement du grimoire…</p>

    <template v-else>
      <section class="grimoire-section grimoire-section--equipped">
        <header class="grimoire-section__head">
          <h5 class="grimoire-section__title">Sorts équipés</h5>
          <span class="grimoire-section__count">{{ equippedSkills.length }} / {{ character.maxEquippedSkills }}</span>
        </header>
        <p v-if="!equippedSkills.length" class="grimoire-empty">
          {{ searchQuery ? 'Aucun sort équipé ne correspond à la recherche.' : 'Aucun sort équipé pour le moment.' }}
        </p>
        <div v-else class="grimoire-grid">
          <GrimoireSkillCard
            v-for="skill in equippedPage.pageItems.value"
            :key="skill.key"
            :skill="skill"
            :is-known="true"
            :is-equipped="true"
            :disabled="false"
            @toggle-equip="togglePending"
          />
        </div>
        <GrimoirePaginationControls
          v-if="equippedPage.totalPages.value > 1"
          :current-page="equippedPage.currentPage.value"
          :total-pages="equippedPage.totalPages.value"
          @previous="equippedPage.goToPreviousPage"
          @next="equippedPage.goToNextPage"
        />
      </section>

      <section class="grimoire-section grimoire-section--available">
        <header class="grimoire-section__head">
          <h5 class="grimoire-section__title">Sorts disponibles</h5>
          <span class="grimoire-section__count">{{ availableSkills.length }}</span>
        </header>
        <p v-if="!availableSkills.length" class="grimoire-empty">
          {{ searchQuery ? 'Aucun sort disponible ne correspond à la recherche.' : 'Aucun sort disponible pour le moment.' }}
        </p>
        <div v-else class="grimoire-grid">
          <GrimoireSkillCard
            v-for="skill in availablePage.pageItems.value"
            :key="skill.key"
            :skill="skill"
            :is-known="true"
            :is-equipped="false"
            :disabled="isLoadoutFull"
            @toggle-equip="togglePending"
          />
        </div>
        <GrimoirePaginationControls
          v-if="availablePage.totalPages.value > 1"
          :current-page="availablePage.currentPage.value"
          :total-pages="availablePage.totalPages.value"
          @previous="availablePage.goToPreviousPage"
          @next="availablePage.goToNextPage"
        />
      </section>

      <section class="grimoire-section grimoire-section--locked">
        <header class="grimoire-section__head">
          <h5 class="grimoire-section__title">Sorts verrouillés</h5>
          <span class="grimoire-section__count">{{ lockedSkills.length }}</span>
        </header>
        <p v-if="!lockedSkills.length" class="grimoire-empty">
          {{ searchQuery ? 'Aucun sort verrouillé ne correspond à la recherche.' : 'Tous les sorts sont débloqués !' }}
        </p>
        <div v-else class="grimoire-grid">
          <GrimoireSkillCard
            v-for="skill in lockedPage.pageItems.value"
            :key="skill.key"
            :skill="skill"
            :is-known="false"
            :is-equipped="false"
            :disabled="true"
            @toggle-equip="togglePending"
          />
        </div>
        <GrimoirePaginationControls
          v-if="lockedPage.totalPages.value > 1"
          :current-page="lockedPage.currentPage.value"
          :total-pages="lockedPage.totalPages.value"
          @previous="lockedPage.goToPreviousPage"
          @next="lockedPage.goToNextPage"
        />
      </section>
    </template>
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
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.grimoire-search {
  flex: 1;
  min-width: 160px;
  font-family: var(--font, sans-serif);
  font-size: 12px;
  padding: 6px 10px;
  border-radius: 4px;
  border: 1px solid var(--line-soft);
  background: var(--panel, oklch(0.20 0.025 270));
  color: var(--ink-2);
}

.grimoire-search::placeholder {
  color: var(--ink-4);
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
  white-space: nowrap;
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

.grimoire-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.grimoire-section__head {
  display: flex;
  align-items: center;
  gap: 10px;
}

.grimoire-section__title {
  font-family: var(--font-caps, var(--font));
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-3);
  margin: 0;
}

.grimoire-section__count {
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  color: var(--ink-4);
}

.grimoire-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
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
