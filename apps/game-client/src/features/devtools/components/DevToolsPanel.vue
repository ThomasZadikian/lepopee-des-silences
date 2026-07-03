<script setup lang="ts">
import { onMounted, ref } from 'vue';

import { useCombatStore } from '../../combat/stores/useCombatStore';
import type { CombatRuntimeDto } from '../../combat/types/combatContracts';
import { demoPlayerId, useRunStore } from '../../runs/stores/runStore';
import { skillsApi } from '../../party/api/skillsApi';
import { usePlayerStore } from '../../party/stores/playerStore';
import type { SkillDefinitionView } from '../../party/types/skillTypes';
import { devToolsApi } from '../api/devToolsApi';
import { useDevTools } from '../composables/useDevTools';
import type { DevToolsRunPsycheResponse, PalaceRoomStateKey, RoomClimateKey } from '../types/devToolsTypes';
import CombatDevToolsSection from './CombatDevToolsSection.vue';
import DevToolsTokenGate from './DevToolsTokenGate.vue';
import PlayerDevToolsSection from './PlayerDevToolsSection.vue';
import PsycheDevToolsSection from './PsycheDevToolsSection.vue';
import RunDevToolsSection from './RunDevToolsSection.vue';

const props = defineProps<{
  runId: string;
  combat: CombatRuntimeDto | null;
}>();

const emit = defineEmits<{
  close: [];
}>();

const runStore = useRunStore();
const combatStore = useCombatStore();
const playerStore = usePlayerStore();
const devTools = useDevTools();
const psyche = ref<DevToolsRunPsycheResponse | null>(null);
const allSkills = ref<SkillDefinitionView[]>([]);

onMounted(() => {
  if (devTools.hasToken.value) {
    void devTools.checkStatus();
    void reloadPsyche();
  }
  void playerStore.loadProfile();
  void loadAllSkills();
});

async function loadAllSkills() {
  try {
    const response = await skillsApi.listActive();
    allSkills.value = response.skills;
  } catch {
    // best-effort : la section joueur affiche juste une liste vide en cas d'échec
  }
}

async function refreshServerState() {
  await runStore.loadRun(props.runId);

  if (runStore.currentRun?.activeCombatId) {
    await combatStore.loadCurrentCombat(props.runId);
  } else {
    combatStore.clearCombat();
  }

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

function addAlly() {
  void execute((token) => devToolsApi.addAlly(token, props.runId), 'Allié ajouté (prochain combat).');
}

function removeAlly() {
  void execute((token) => devToolsApi.removeAlly(token, props.runId), 'Allié retiré (prochain combat).');
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

function killEnemies() {
  void execute((token) => devToolsApi.killEnemies(token, props.runId), 'Enemies neutralises.');
}

function killEnemy(combatantId: string) {
  void execute((token) => devToolsApi.killEnemy(token, props.runId, combatantId), 'Enemy neutralise.');
}

function setVitals(combatantId: string, vitality: number, guard: number) {
  void execute(
    (token) => devToolsApi.setVitals(token, props.runId, combatantId, vitality, guard),
    'Vitals appliquees.',
  );
}

function applyStatus(combatantId: string, statusKey: string, stacks: number, duration: number) {
  void execute(
    (token) => devToolsApi.applyStatus(token, props.runId, combatantId, statusKey, stacks, duration),
    'Status applique.',
  );
}

function unlockSkill(characterId: string, skillKey: string) {
  void execute(
    (token) => devToolsApi.unlockSkill(token, demoPlayerId, characterId, skillKey),
    'Sort debloque.',
  );
}

function awardStatPoints(amount: number) {
  void execute(
    (token) => devToolsApi.awardStatPoints(token, demoPlayerId, amount),
    `${amount} point(s) de competence accorde(s).`,
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

    <RunDevToolsSection
      :disabled="!devTools.hasToken.value"
      :is-loading="devTools.isLoading.value"
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
    />

    <PsycheDevToolsSection
      :psyche="psyche"
      :disabled="!devTools.hasToken.value"
      :is-loading="devTools.isLoading.value"
      @refresh="refreshPsyche"
    />

    <CombatDevToolsSection
      :combat="props.combat"
      :disabled="!devTools.hasToken.value"
      :is-loading="devTools.isLoading.value"
      @kill-enemies="killEnemies"
      @kill-enemy="killEnemy"
      @set-vitals="setVitals"
      @apply-status="applyStatus"
    />

    <PlayerDevToolsSection
      :disabled="!devTools.hasToken.value"
      :is-loading="devTools.isLoading.value"
      :characters="playerStore.profile?.characters ?? []"
      :all-skills="allSkills"
      @unlock-skill="unlockSkill"
      @award-stat-points="awardStatPoints"
    />
  </aside>
</template>

<style>
.devtools-panel {
  position: fixed;
  top: 14px;
  right: 14px;
  z-index: 9100;
  width: min(520px, calc(100vw - 28px));
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
</style>
