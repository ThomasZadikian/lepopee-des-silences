<script setup lang="ts">
import { onMounted, ref } from 'vue';

import { getActivePlayerId, useRunStore } from '../../runs/stores/runStore';
import { skillsApi } from '../../party/api/skillsApi';
import { itemsApi } from '../../party/api/itemsApi';
import { lawsApi } from '../../palace-laws/api/lawsApi';
import { cursesApi } from '../../palace-laws/api/cursesApi';
import { usePlayerStore } from '../../party/stores/playerStore';
import type { SkillDefinitionView } from '../../party/types/skillTypes';
import type { ItemDefinitionView } from '../../party/types/itemTypes';
import type { PalaceLawDefinitionView } from '../../palace-laws/types/lawTypes';
import type { CurseDefinitionView } from '../../palace-laws/types/curseTypes';
import { devToolsApi } from '../api/devToolsApi';
import { useDevTools } from '../composables/useDevTools';
import type { DevToolsRunPsycheResponse, PalaceRoomStateKey, RoomClimateKey } from '../types/devToolsTypes';
import DevToolsTokenGate from './DevToolsTokenGate.vue';
import DevToolsMicroMenu from './DevToolsMicroMenu.vue';

const props = defineProps<{
  runId: string;
}>();

const emit = defineEmits<{
  close: [];
}>();

const runStore = useRunStore();
const playerStore = usePlayerStore();
const devTools = useDevTools();
const psyche = ref<DevToolsRunPsycheResponse | null>(null);
const allSkills = ref<SkillDefinitionView[]>([]);
const allItems = ref<ItemDefinitionView[]>([]);
const allLaws = ref<PalaceLawDefinitionView[]>([]);
const allCurses = ref<CurseDefinitionView[]>([]);

onMounted(() => {
  if (devTools.hasToken.value) {
    void devTools.checkStatus();
    void reloadPsyche();
  }
  void playerStore.loadProfile();
  void loadAllSkills();
  void loadAllItems();
  void loadAllLaws();
  void loadAllCurses();
});

async function loadAllSkills() {
  try {
    const response = await skillsApi.listActive();
    allSkills.value = response.skills;
  } catch {
    // best-effort : la fenêtre Sorts affiche juste une liste vide en cas d'échec
  }
}

async function loadAllItems() {
  try {
    const response = await itemsApi.listActive();
    allItems.value = response.items;
  } catch {
    // best-effort : la fenêtre Objets affiche juste une liste vide en cas d'échec
  }
}

async function loadAllLaws() {
  try {
    const response = await lawsApi.listActive();
    allLaws.value = response.laws;
  } catch {
    // best-effort : la fenêtre Lois affiche juste une liste vide en cas d'échec
  }
}

async function loadAllCurses() {
  try {
    const response = await cursesApi.listAvailable();
    allCurses.value = response.curses;
  } catch {
    // best-effort : la fenêtre Malédictions affiche juste une liste vide en cas d'échec
  }
}

async function refreshServerState() {
  await runStore.loadRun(props.runId);
  await playerStore.loadProfile(); // garde le profil joueur synchro après chaque action (sorts / points)
  await reloadPsyche(); // garde la vue psyché synchro après chaque action (ex. advance rooms)
}

async function reloadPsyche() {
  if (!devTools.hasToken.value) return;
  try {
    psyche.value = await devToolsApi.getPsyche(devTools.token.value, props.runId);
  } catch {
    // best-effort : le bouton « Rafraîchir » fait remonter les erreurs explicitement
  }
}

function refreshPsyche() {
  void devTools.runAction(async (token) => {
    psyche.value = await devToolsApi.getPsyche(token, props.runId);
  }, 'Psyché chargée.');
}

async function execute(action: (token: string) => Promise<unknown>, successMessage: string) {
  const ok = await devTools.runAction(action, successMessage);
  if (ok) await refreshServerState();
}

function saveToken(token: string) {
  devTools.saveToken(token);
  void devTools.checkStatus();
}

function clearToken() {
  devTools.clearToken();
}

function advanceRoom() {
  void execute((token) => devToolsApi.advanceRoom(token, props.runId), 'Room avancee.');
}

function addAlly(companionNpcKey: string) {
  void execute(
    (token) => devToolsApi.addAlly(token, props.runId, companionNpcKey),
    'Compagnon recruté (prochain combat).',
  );
}

function removeAlly() {
  void execute((token) => devToolsApi.removeAlly(token, props.runId), 'Allié retiré (prochain combat).');
}

function addItem(itemDefinitionKey: string, quantity: number) {
  void execute(
    (token) => devToolsApi.addItem(token, props.runId, itemDefinitionKey, quantity),
    'Objet ajouté à la besace.',
  );
}

function advanceRooms(count: number) {
  void execute((token) => devToolsApi.advanceRooms(token, props.runId, count), `${count} rooms avancees.`);
}

function forcePalaceState(state: PalaceRoomStateKey) {
  void execute((token) => devToolsApi.forcePalaceRoomState(token, props.runId, state), 'Etat de room force.');
}

function forceClimate(climate: RoomClimateKey) {
  void execute((token) => devToolsApi.forceRoomClimate(token, props.runId, climate), 'Climat force.');
}

function activateLaw(lawKey: string) {
  void execute((token) => devToolsApi.activateLaw(token, props.runId, lawKey), 'Law activee.');
}

function clearLaws() {
  void execute((token) => devToolsApi.clearLaws(token, props.runId), 'Laws effacees.');
}

function activateCurse(curseKey: string) {
  void execute((token) => devToolsApi.activateCurse(token, props.runId, curseKey), 'Curse activee.');
}

function clearCurses() {
  void execute((token) => devToolsApi.clearCurses(token, props.runId), 'Curses effacees.');
}

function unlockSkill(characterId: string, skillKey: string) {
  void execute(
    (token) => devToolsApi.unlockSkill(token, getActivePlayerId(), characterId, skillKey),
    'Sort debloque.',
  );
}

</script>

<template>
  <aside class="devtools-panel" aria-label="Game client devtools">
    <header class="devtools-panel__header">
      <div>
        <p class="devtools-kicker">Game client</p>
        <h2>DevTools</h2>
      </div>
      <button class="devtools-close" type="button" aria-label="Fermer devtools" @click="emit('close')">
        x
      </button>
    </header>

    <DevToolsTokenGate
      :has-token="devTools.hasToken.value"
      :status="devTools.status.value"
      :environment="devTools.statusEnvironment.value"
      :is-loading="devTools.isLoading.value"
      @save-token="saveToken"
      @clear-token="clearToken"
      @check-status="devTools.checkStatus"
    />

    <p v-if="devTools.message.value" class="devtools-message devtools-message--success">
      {{ devTools.message.value }}
    </p>
    <p v-if="devTools.error.value" class="devtools-message devtools-message--error">
      {{ devTools.error.value }}
    </p>

    <DevToolsMicroMenu
      :disabled="!devTools.hasToken.value"
      :is-loading="devTools.isLoading.value"
      :characters="playerStore.profile?.characters ?? []"
      :all-skills="allSkills"
      :all-items="allItems"
      :all-laws="allLaws"
      :all-curses="allCurses"
      :psyche="psyche"
      @advance-room="advanceRoom"
      @advance-rooms="advanceRooms"
      @force-palace-state="forcePalaceState"
      @force-climate="forceClimate"
      @activate-law="activateLaw"
      @clear-laws="clearLaws"
      @activate-curse="activateCurse"
      @clear-curses="clearCurses"
      @add-ally="addAlly"
      @remove-ally="removeAlly"
      @add-item="addItem"
      @unlock-skill="unlockSkill"
      @refresh-psyche="refreshPsyche"
    />
  </aside>
</template>

<style>
.devtools-panel {
  position: fixed;
  top: 14px;
  right: 14px;
  z-index: 9100;
  width: min(360px, calc(100vw - 28px));
  max-height: calc(100vh - 28px);
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 14px;
  border: 1px solid oklch(0.72 0.12 85 / 0.35);
  border-radius: 10px;
  background: oklch(0.15 0.03 270 / 0.96);
  box-shadow: 0 18px 60px oklch(0 0 0 / 0.45);
  color: var(--ink-2, #e8e2d5);
}

.devtools-panel__header,
.devtools-card__header,
.devtools-actions-row,
.devtools-token-row,
.devtools-inline-form {
  display: flex;
  align-items: center;
  gap: 10px;
}

.devtools-panel__header {
  justify-content: space-between;
}

.devtools-panel h2,
.devtools-panel h3,
.devtools-panel p {
  margin: 0;
}

.devtools-kicker {
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--gold, #d7b56d);
}

.devtools-close,
.devtools-btn {
  border: 1px solid oklch(0.72 0.12 85 / 0.45);
  border-radius: 6px;
  padding: 8px 10px;
  background: oklch(0.23 0.04 270 / 0.9);
  color: var(--ink-2, #e8e2d5);
  cursor: pointer;
}

.devtools-close:hover,
.devtools-btn:hover:not(:disabled) {
  background: oklch(0.3 0.05 270 / 0.95);
}

.devtools-btn:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.devtools-btn--danger {
  border-color: oklch(0.62 0.18 25 / 0.65);
  color: oklch(0.82 0.12 35);
}

.devtools-btn--ghost {
  background: transparent;
}

.devtools-card {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 12px;
  border: 1px solid oklch(0.72 0.12 85 / 0.2);
  border-radius: 8px;
  background: oklch(0.18 0.03 270 / 0.74);
}

.devtools-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.devtools-input {
  min-width: 0;
  width: 100%;
  border: 1px solid oklch(0.72 0.12 85 / 0.3);
  border-radius: 6px;
  padding: 8px 9px;
  background: oklch(0.11 0.025 270 / 0.88);
  color: var(--ink-2, #e8e2d5);
}

.devtools-input--small {
  width: 84px;
  flex: 0 0 auto;
}

.devtools-label,
.devtools-status-list div {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.devtools-status-list {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin: 0;
}

.devtools-status-list dt {
  color: var(--ink-4, #a49a88);
  font-size: 11px;
}

.devtools-status-list dd {
  margin: 0;
}

.devtools-status--available,
.devtools-message--success {
  color: oklch(0.78 0.14 145);
}

.devtools-status--unavailable,
.devtools-message--error {
  color: oklch(0.78 0.14 35);
}

.devtools-message {
  padding: 8px 10px;
  border-radius: 6px;
  background: oklch(0.1 0.02 270 / 0.8);
}

.devtools-muted {
  color: var(--ink-4, #a49a88);
  font-size: 12px;
}

.devtools-vitals-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 86px 86px auto;
  gap: 8px;
}

.devtools-vitals-grid--status {
  grid-template-columns: minmax(0, 1fr) 76px 76px auto;
}

/* Nav — the "micro-menu bis": one button per devtools window, opening a full
   PageOverlayModal instead of cramming every action into this sidebar. */
.devtools-micro-menu {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.devtools-micro-menu__btn {
  min-width: 56px;
  padding: 10px 8px;
  border: 1px solid oklch(0.72 0.12 85 / 0.35);
  border-radius: 6px;
  background: oklch(0.2 0.035 270 / 0.85);
  color: var(--ink-2, #e8e2d5);
  font-family: var(--font-caps, var(--font));
  font-size: 10px;
  letter-spacing: 0.08em;
  text-align: center;
  cursor: pointer;
  transition: border-color 0.15s, background 0.15s, color 0.15s;
}

.devtools-micro-menu__btn:hover {
  border-color: var(--gold, #d7b56d);
  color: var(--gold, #d7b56d);
}

.devtools-micro-menu__btn--active {
  border-color: var(--gold, #d7b56d);
  background: oklch(0.55 0.08 85 / 0.18);
  color: var(--gold, #d7b56d);
}

/* Windows — content rendered inside PageOverlayModal for each devtools element. */
.devtools-window {
  display: flex;
  flex-direction: column;
  gap: 18px;
  padding: 8px 30px 24px 4px;
  color: var(--ink-2, #e8e2d5);
}

.devtools-window__head h2 {
  margin: 0 0 6px;
  font-size: 22px;
  color: var(--gold, #d7b56d);
}

.devtools-window__head p {
  margin: 0;
  font-size: 12.5px;
  color: var(--ink-4, #a49a88);
}

.devtools-window__body {
  display: flex;
  flex-direction: column;
  gap: 16px;
  max-width: 640px;
}

/* Catalog browsing (Sorts/Objets/Compagnons) — a filterable grid on the left,
   a detail sheet with the full description and the action button on the right. */
.devtools-catalog-layout {
  display: grid;
  grid-template-columns: minmax(260px, 1.4fr) minmax(260px, 1fr);
  gap: 20px;
  align-items: start;
  max-width: none;
}

.devtools-catalog-toolbar {
  display: flex;
  gap: 10px;
  margin-bottom: 10px;
}

.devtools-catalog-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(170px, 1fr));
  gap: 10px;
  max-height: 54vh;
  overflow-y: auto;
  padding-right: 4px;
}

.devtools-catalog-cell {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 12px 13px;
  border: 1px solid oklch(0.72 0.12 85 / 0.2);
  border-radius: 6px;
  background: oklch(0.18 0.03 270 / 0.74);
  cursor: pointer;
  text-align: left;
  transition: border-color 0.15s, background 0.15s;
}

.devtools-catalog-cell:hover {
  border-color: oklch(0.72 0.12 85 / 0.45);
}

.devtools-catalog-cell--sel {
  border-color: var(--gold, #d7b56d);
  background: oklch(0.55 0.08 85 / 0.12);
}

.devtools-catalog-cell__name {
  font-size: 13px;
  color: var(--ink-2, #e8e2d5);
}

.devtools-catalog-cell__meta {
  font-size: 10px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--ink-4, #a49a88);
}

.devtools-catalog-sheet {
  padding: 16px 18px;
  border: 1px solid oklch(0.72 0.12 85 / 0.3);
  border-radius: 8px;
  background: oklch(0.15 0.03 270 / 0.9);
  display: flex;
  flex-direction: column;
  gap: 10px;
  position: sticky;
  top: 0;
}

.devtools-catalog-sheet__name {
  margin: 0;
  font-size: 17px;
  color: var(--gold, #d7b56d);
}

.devtools-catalog-sheet__desc {
  margin: 0;
  font-size: 12.5px;
  line-height: 1.5;
  color: var(--ink-3, #cdbfa7);
}

.devtools-catalog-sheet__facts {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.devtools-catalog-fact {
  padding: 3px 7px;
  border: 1px solid oklch(0.72 0.12 85 / 0.25);
  border-radius: 999px;
  font-size: 10px;
  font-family: var(--font-mono, monospace);
  color: var(--ink-3, #cdbfa7);
}

.devtools-catalog-empty {
  color: var(--ink-4, #a49a88);
  font-size: 12px;
  padding: 20px 0;
}

@media (max-width: 720px) {
  .devtools-panel {
    top: 0;
    right: 0;
    width: 100vw;
    max-height: 100vh;
    border-radius: 0;
  }

  .devtools-grid,
  .devtools-status-list,
  .devtools-vitals-grid,
  .devtools-vitals-grid--status {
    grid-template-columns: 1fr;
  }

  .devtools-token-row,
  .devtools-inline-form {
    align-items: stretch;
    flex-direction: column;
  }
}

@media (max-width: 860px) {
  .devtools-catalog-layout {
    grid-template-columns: 1fr;
  }
}
</style>
