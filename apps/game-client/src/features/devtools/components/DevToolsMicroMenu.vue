<script setup lang="ts">
import { ref } from 'vue';
import PageOverlayModal from '../../../shared/components/PageOverlayModal.vue';
import type { PlayerCharacterView } from '../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../party/types/skillTypes';
import type { ItemDefinitionView } from '../../party/types/itemTypes';
import type { PalaceLawDefinitionView } from '../../palace-laws/types/lawTypes';
import type { CurseDefinitionView } from '../../palace-laws/types/curseTypes';
import type { DevToolsRunPsycheResponse, PalaceRoomStateKey, RoomClimateKey } from '../types/devToolsTypes';
import SkillsDevToolsWindow from '../windows/SkillsDevToolsWindow.vue';
import ItemsDevToolsWindow from '../windows/ItemsDevToolsWindow.vue';
import StatPointsDevToolsWindow from '../windows/StatPointsDevToolsWindow.vue';
import RunDevToolsWindow from '../windows/RunDevToolsWindow.vue';
import PartyDevToolsWindow from '../windows/PartyDevToolsWindow.vue';
import RoomDevToolsWindow from '../windows/RoomDevToolsWindow.vue';
import LawsDevToolsWindow from '../windows/LawsDevToolsWindow.vue';
import CursesDevToolsWindow from '../windows/CursesDevToolsWindow.vue';
import PsycheDevToolsWindow from '../windows/PsycheDevToolsWindow.vue';

const props = defineProps<{
  disabled: boolean;
  isLoading: boolean;
  characters: PlayerCharacterView[];
  allSkills: SkillDefinitionView[];
  allItems: ItemDefinitionView[];
  allLaws: PalaceLawDefinitionView[];
  allCurses: CurseDefinitionView[];
  psyche: DevToolsRunPsycheResponse | null;
}>();

const emit = defineEmits<{
  advanceRoom: [];
  advanceRooms: [count: number];
  forcePalaceState: [state: PalaceRoomStateKey];
  forceClimate: [climate: RoomClimateKey];
  activateLaw: [lawKey: string];
  clearLaws: [];
  activateCurse: [curseKey: string];
  clearCurses: [];
  addAlly: [companionNpcKey: string];
  removeAlly: [];
  addItem: [itemDefinitionKey: string, quantity: number];
  unlockSkill: [characterId: string, skillKey: string];
  awardStatPoints: [amount: number];
  refreshPsyche: [];
}>();

type WindowKey =
  | 'sorts' | 'objets' | 'points' | 'run' | 'compagnons'
  | 'salle' | 'lois' | 'malediction' | 'psyche';

const entries: { key: WindowKey; label: string; code: string }[] = [
  { key: 'sorts', label: 'Sorts', code: 'SO' },
  { key: 'objets', label: 'Objets', code: 'OB' },
  { key: 'points', label: 'Points de compétence', code: 'PC' },
  { key: 'run', label: 'Run', code: 'RN' },
  { key: 'compagnons', label: 'Compagnons', code: 'CP' },
  { key: 'salle', label: 'Salle', code: 'SA' },
  { key: 'lois', label: 'Lois', code: 'LO' },
  { key: 'malediction', label: 'Malédictions', code: 'MA' },
  { key: 'psyche', label: 'Psyché', code: 'PS' },
];

const activeWindow = ref<WindowKey | null>(null);
</script>

<template>
  <nav class="devtools-micro-menu" aria-label="Menu devtools">
    <button
      v-for="entry in entries"
      :key="entry.key"
      type="button"
      class="devtools-micro-menu__btn"
      :class="{ 'devtools-micro-menu__btn--active': activeWindow === entry.key }"
      :title="entry.label"
      @click="activeWindow = entry.key"
    >
      {{ entry.code }}
    </button>
  </nav>

  <Teleport to="body">
    <PageOverlayModal v-if="activeWindow" @close="activeWindow = null">
      <SkillsDevToolsWindow
        v-if="activeWindow === 'sorts'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        :characters="props.characters"
        :all-skills="props.allSkills"
        @unlock-skill="(characterId, skillKey) => emit('unlockSkill', characterId, skillKey)"
      />
      <ItemsDevToolsWindow
        v-else-if="activeWindow === 'objets'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        :all-items="props.allItems"
        @add-item="(key, quantity) => emit('addItem', key, quantity)"
      />
      <StatPointsDevToolsWindow
        v-else-if="activeWindow === 'points'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        @award-stat-points="(amount) => emit('awardStatPoints', amount)"
      />
      <RunDevToolsWindow
        v-else-if="activeWindow === 'run'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        @advance-room="emit('advanceRoom')"
        @advance-rooms="(count) => emit('advanceRooms', count)"
      />
      <PartyDevToolsWindow
        v-else-if="activeWindow === 'compagnons'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        @add-ally="(key) => emit('addAlly', key)"
        @remove-ally="emit('removeAlly')"
      />
      <RoomDevToolsWindow
        v-else-if="activeWindow === 'salle'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        @force-palace-state="(state) => emit('forcePalaceState', state)"
        @force-climate="(climate) => emit('forceClimate', climate)"
      />
      <LawsDevToolsWindow
        v-else-if="activeWindow === 'lois'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        :all-laws="props.allLaws"
        @activate-law="(key) => emit('activateLaw', key)"
        @clear-laws="emit('clearLaws')"
      />
      <CursesDevToolsWindow
        v-else-if="activeWindow === 'malediction'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        :all-curses="props.allCurses"
        @activate-curse="(key) => emit('activateCurse', key)"
        @clear-curses="emit('clearCurses')"
      />
      <PsycheDevToolsWindow
        v-else-if="activeWindow === 'psyche'"
        :disabled="props.disabled"
        :is-loading="props.isLoading"
        :psyche="props.psyche"
        @refresh="emit('refreshPsyche')"
      />
    </PageOverlayModal>
  </Teleport>
</template>
