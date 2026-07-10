<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';

import { useRunStore } from '../../runs/stores/runStore';
import { useCombatLogMetrics } from '../composables/useCombatLogMetrics';
import { useTurnForecast } from '../composables/useTurnForecast';
import type { CombatFeedbackEvent } from '../stores/useCombatStore';
import { useCombatStore } from '../stores/useCombatStore';
import CombatantCard from './CombatantCard.vue';
import CombatLogPanel from './CombatLogPanel.vue';
import CombatMetersPanel from './CombatMetersPanel.vue';
import CombatOutcomePanel from './CombatOutcomePanel.vue';
import SkillBar from './SkillBar.vue';

const props = defineProps<{
  runId: string;
  combatId: string;
}>();

const emit = defineEmits<{
  combatCompleted: [];
  combatFailed: [];
  leaveRun: [];
}>();

const combatStore = useCombatStore();

// Predictive ATB queue: re-simulated from live gauge/speed data on every
// render, so a speed buff/debuff immediately reshuffles the forecast.
const turnForecast = useTurnForecast(() => combatStore.allCombatants);
const runStore = useRunStore();
const { state: metricsState } = useCombatLogMetrics(
  () => combatStore.logEntries,
  () => combatStore.combat,
);

const isDamageModalOpen = ref(false);

const activeCombatant = computed(() => combatStore.currentActor);
const isPlayerTurn = computed(() => combatStore.isPlayerTurn);
const hasSelectedSkill = computed(() => Boolean(combatStore.selectedSkill));
const selectedTarget = computed(() => {
  const id = combatStore.selectedTargetIds[0];
  if (!id) return null;
  return combatStore.findCombatantById(id);
});

const roomLabel = computed(() => {
  const room = runStore.currentRoom;
  if (!room) return 'Affrontement';
  const depth = (room.currentNodeDepth ?? 0) + 1;
  return `${room.theme ?? room.roomType} · salle ${depth} — affrontement`;
});

// Same depth-scaling curve as EnemyStatScaler.DepthMultiplier (game-engine):
// room 1 = ×1.0, then +0.5 per room past 1. This is a depth-only estimate —
// active Lois du Palais can push the real in-combat multiplier higher still.
const ENEMY_DEPTH_DIFFICULTY_STEP = 0.5;

const enemyDepthMultiplier = computed(() => {
  const depth = runStore.currentRoom?.depth ?? 1;
  const effectiveDepth = Math.max(0, depth - 1);
  return 1 + effectiveDepth * ENEMY_DEPTH_DIFFICULTY_STEP;
});

const actionPreview = computed(() => {
  if (combatStore.selectedSkill) return combatStore.selectedSkill.displayName;
  if (combatStore.selectedItem) return combatStore.selectedItem.displayName;
  return 'Frappe ciblée. +50 % si la cible saigne.';
});

// Highest threat among living allies — the ally bar renders relative to this,
// and whoever holds it gets the "menace" indicator (most likely enemy target).
const maxAllyThreat = computed(() =>
  Math.max(0, ...combatStore.allies.map((a) => a.threatValue ?? 0)),
);

function canSelect(combatantId: string): boolean {
  if (combatStore.isLoading) return false;
  if (combatStore.selectedItem?.targetingType === 'SingleAlly') {
    return combatStore.itemValidTargets.some((t) => t.id === combatantId);
  }
  if (!combatStore.selectedSkill || !isPlayerTurn.value) return false;
  return combatStore.validTargets.some((t) => t.id === combatantId);
}

function isInvalidTarget(combatantId: string): boolean {
  if (combatStore.selectedItem) return false;
  if (!hasSelectedSkill.value || combatStore.isLoading || combatStore.isSelectedTarget(combatantId)) return false;
  return !combatStore.validTargets.some((t) => t.id === combatantId);
}

function isVisuallyActive(combatantId: string): boolean {
  if (combatStore.thinkingCombatantId) return combatStore.thinkingCombatantId === combatantId;
  if (combatStore.isResolvingAction) return false;
  return combatStore.isCurrentActor(combatantId);
}

function handleSelect(combatantId: string) {
  if (combatStore.selectedItem?.targetingType === 'SingleAlly') {
    const isValid = combatStore.itemValidTargets.some((t) => t.id === combatantId);
    if (isValid) combatStore.selectedTargetIds = [combatantId];
    return;
  }
  const validIds = combatStore.validTargets.map((t) => t.id);
  if (validIds.includes(combatantId)) combatStore.selectTarget(combatantId);
}

async function handleSubmit() {
  await combatStore.submitAction(props.runId, (combat) => {
    runStore.combatRuntime = combat;
  });
}

async function handleSubmitItem() {
  await combatStore.submitItemAction(props.runId, (combat) => {
    runStore.combatRuntime = combat;
  });
}
function handleClearSelection() { combatStore.clearSelection(); combatStore.clearItemSelection(); }
function handleSelectItem(itemId: string) { combatStore.selectItem(itemId); }
function handleContinue() {
  combatStore.clearCombat();
  emit('combatCompleted');
}

function floatEventsFor(combatantId: string) {
  return combatStore.feedbackEvents.filter((event) => event.combatantId === combatantId);
}

function floatLabel(event: CombatFeedbackEvent): string {
  if (event.type === 'heal') return `+${event.amount}`;
  if (event.type === 'guard') return `◇ ${event.amount}`;
  if (event.type === 'miss') return 'Manqué !';
  return event.isCritical ? `✹ -${event.amount}` : `-${event.amount}`;
}

function floatClasses(event: CombatFeedbackEvent): Record<string, boolean> {
  return {
    [`combat-float--${event.type}`]: true,
    'combat-float--magic': event.type === 'damage' && event.category === 'Magic',
    'combat-float--critical': Boolean(event.isCritical),
  };
}

watch(() => combatStore.terminalEvent, (event) => {
  if (event?.kind === 'defeat') emit('combatFailed');
});

watch(() => combatStore.canSubmit, async (isReady) => {
  if (isReady && !combatStore.isResolvingAction) { await nextTick(); await handleSubmit(); }
});

watch(() => combatStore.canSubmitItem, async (isReady) => {
  const item = combatStore.selectedItem;
  if (isReady && item?.targetingType === 'Self' && !combatStore.isResolvingAction) { await nextTick(); await handleSubmitItem(); }
});

watch(() => combatStore.selectedTargetIds, async (ids) => {
  const item = combatStore.selectedItem;
  if (item && item.targetingType === 'SingleAlly' && ids.length > 0 && !combatStore.isResolvingAction) { await nextTick(); await handleSubmitItem(); }
});

onMounted(() => {
  if (combatStore.combat?.id === props.combatId) return;
  if (runStore.combatRuntime?.id === props.combatId && runStore.combatRuntime.status === 'Active') {
    combatStore.initCombat(runStore.combatRuntime);
  } else {
    combatStore.loadCurrentCombat(props.runId);
  }
});

watch(() => props.combatId, (newId) => {
  if (!newId) return;
  if (combatStore.combat?.id === newId) return;
  combatStore.clearCombat();
  if (runStore.combatRuntime?.id === newId) combatStore.initCombat(runStore.combatRuntime);
  else combatStore.loadCurrentCombat(props.runId);
});

// One real-time clock for the whole fight. It keeps ticking while combat is
// Active; the player acts whenever their bar is full (isPlayerTurn).
watch(
  () => [combatStore.combat?.status, combatStore.combat?.id] as const,
  () => {
    if (combatStore.combat?.status === 'Active') {
      void combatStore.runCombatClock(props.runId, (c) => { runStore.combatRuntime = c; });
    }
  },
  { immediate: true },
);

</script>

<template>
  <section class="combat-scene">

    <!-- Loading / absent -->
    <template v-if="!combatStore.combat">
      <div class="combat-scene__placeholder">
        <p class="es-kicker">Confrontation</p>
        <h3 class="es-h2" v-if="combatStore.isLoading">Le seuil s'ouvre…</h3>
        <h3 class="es-h2" v-else>Confrontation indisponible</h3>
      </div>
    </template>

    <!-- Combat actif -->
    <template v-else>
      <div class="combat-scene__initiative es-plate">
        <span class="combat-scene__initiative-title">Ordre prévisionnel</span>
        <div class="combat-scene__initiative-list">
          <span
            v-for="(entry, i) in turnForecast"
            :key="entry.slot + '-' + entry.combatantId"
            class="initiative-pill"
            :class="{
              'initiative-pill--active': i === 0 && entry.combatantId === activeCombatant?.id,
              'initiative-pill--enemy': entry.side === 'Enemy',
              'initiative-pill--next': i === 0,
            }"
          >
            <span class="initiative-pill__slot">{{ i + 1 }}</span>
            <span class="initiative-pill__dot" />
            {{ entry.displayName }}
          </span>
        </div>
      </div>

      <div class="combat-scene__arena">
        <div class="combat-scene__header">
          <span class="combat-scene__header-room">{{ roomLabel }}</span>
          <span class="combat-scene__header-turn">tour {{ combatStore.combat.turnNumber }} · temps réel · les jauges ne s'arrêtent pas</span>
        </div>

        <!-- Les Manifestations (enemies) -->
        <p class="combat-scene__side-title combat-scene__side-title--foe">
          ◆ Les Manifestations · {{ combatStore.enemies.length }}
          <span
            v-if="enemyDepthMultiplier > 1"
            class="combat-scene__difficulty-badge"
            title="Estimation liée à la profondeur de la salle uniquement — les Lois du Palais actives peuvent encore l'augmenter."
          >
            stats ≈ ×{{ enemyDepthMultiplier.toFixed(1) }}
          </span>
        </p>
        <div class="combat-scene__grid">
          <CombatantCard
            v-for="combatant in combatStore.enemies"
            :key="combatant.id"
            :combatant="combatant"
            :is-current-actor="isVisuallyActive(combatant.id)"
            :is-selected-target="combatStore.isSelectedTarget(combatant.id)"
            :is-selectable="canSelect(combatant.id)"
            :is-targetable="canSelect(combatant.id)"
            :is-invalid-target="isInvalidTarget(combatant.id)"
            :is-active-player="false"
            :is-thinking="combatStore.thinkingCombatantId === combatant.id"
            :is-damaged="combatStore.recentlyDamagedIds.includes(combatant.id)"
            :is-guarded="combatStore.recentlyGuardedIds.includes(combatant.id)"
            :is-just-defeated="combatStore.recentlyDefeatedIds.includes(combatant.id)"
            :is-magic-hit="combatStore.recentlyMagicHitIds.includes(combatant.id)"
            :is-critical-hit="combatStore.recentlyCriticalHitIds.includes(combatant.id)"
            :is-missed="combatStore.recentlyMissedIds.includes(combatant.id)"
            :is-acting="combatStore.recentlyActingId === combatant.id"
            @select="handleSelect"
          >
            <span
              v-for="event in floatEventsFor(combatant.id)"
              :key="event.id"
              class="combat-float"
              :class="floatClasses(event)"
            >
              {{ floatLabel(event) }}
            </span>
          </CombatantCard>
        </div>

        <!-- Manomètre (pression du Palais) -->
        <div class="combat-scene__seuil">
          <div class="seuil-line" />
          <div class="seuil-manometer" aria-hidden="true">
            <div class="seuil-manometer__ticks" />
            <div class="seuil-manometer__needle" />
            <div class="seuil-manometer__hub" />
            <span class="seuil-manometer__label">pression</span>
          </div>
          <div class="seuil-line" />
        </div>

        <!-- Alliés -->
        <p class="combat-scene__side-title combat-scene__side-title--allies">◆ Alliés · {{ combatStore.allies.length }}</p>
        <div class="combat-scene__grid">
          <CombatantCard
            v-for="combatant in combatStore.allies"
            :key="combatant.id"
            :combatant="combatant"
            :is-current-actor="isVisuallyActive(combatant.id)"
            :is-selected-target="combatStore.isSelectedTarget(combatant.id)"
            :is-selectable="canSelect(combatant.id)"
            :is-targetable="canSelect(combatant.id)"
            :is-invalid-target="isInvalidTarget(combatant.id)"
            :is-active-player="combatant.side === 'Player' && isPlayerTurn"
            :is-thinking="combatStore.thinkingCombatantId === combatant.id"
            :is-damaged="combatStore.recentlyDamagedIds.includes(combatant.id)"
            :is-guarded="combatStore.recentlyGuardedIds.includes(combatant.id)"
            :is-just-defeated="combatStore.recentlyDefeatedIds.includes(combatant.id)"
            :is-magic-hit="combatStore.recentlyMagicHitIds.includes(combatant.id)"
            :is-critical-hit="combatStore.recentlyCriticalHitIds.includes(combatant.id)"
            :is-missed="combatStore.recentlyMissedIds.includes(combatant.id)"
            :is-acting="combatStore.recentlyActingId === combatant.id"
            :max-threat="maxAllyThreat"
            @select="handleSelect"
          >
            <span
              v-for="event in floatEventsFor(combatant.id)"
              :key="event.id"
              class="combat-float"
              :class="floatClasses(event)"
            >
              {{ floatLabel(event) }}
            </span>
          </CombatantCard>
        </div>

        <!-- Barre de skills -->
        <div class="combat-scene__compose">
          <div class="combat-scene__skills-panel es-plate">
            <div class="compose__header">
              <span class="compose__speaker-name">{{ activeCombatant?.displayName ?? '—' }} agit</span>
              <span class="compose__through">
                <template v-if="combatStore.selectedSkillKey || combatStore.selectedItemId">
                  {{ selectedTarget?.displayName ?? 'Choisir une cible' }} · {{ actionPreview }}
                </template>
                <template v-else>{{ actionPreview }}</template>
              </span>
              <button class="es-btn es-btn--ghost compose__meter-toggle" @click="isDamageModalOpen = true">
                △ Relevé des dégâts
              </button>
            </div>

            <SkillBar
              :combatant="activeCombatant"
              :selected-skill-key="combatStore.selectedSkillKey"
              :is-player-turn="isPlayerTurn"
              :is-loading="combatStore.isResolvingAction"
              :usable-battle-items="combatStore.combat?.usableBattleItems ?? []"
              :selected-item-id="combatStore.selectedItemId"
              @select-skill="combatStore.selectSkill"
              @select-item="handleSelectItem"
            />

            <button
              v-if="combatStore.selectedSkillKey || combatStore.selectedItemId || combatStore.selectedTargetIds.length > 0"
              class="es-btn es-btn--ghost compose__cancel"
              :disabled="combatStore.isResolvingAction"
              @click="handleClearSelection"
            >
              Annuler
            </button>
          </div>
        </div>
      </div>

      <CombatLogPanel :entries="combatStore.logEntries" />

      <!-- Damage meter modal -->
      <Teleport to="body">
        <div v-if="isDamageModalOpen" class="damage-modal-backdrop" @click.self="isDamageModalOpen = false">
          <div class="damage-modal">
            <button class="damage-modal__close" @click="isDamageModalOpen = false" aria-label="Fermer">✕</button>
            <CombatMetersPanel :metrics="metricsState" :combat="combatStore.combat" />
          </div>
        </div>
      </Teleport>

      <!-- Outcome overlay -->
      <CombatOutcomePanel
        v-if="combatStore.isVictory || combatStore.isDefeat"
        :is-victory="combatStore.isVictory"
        :is-loading="combatStore.isLoading"
        @continue="handleContinue"
        @leave-run="$emit('leaveRun')"
      />

      <!-- Resolving indicator -->
      <div v-if="combatStore.isResolvingAction" class="combat-scene__resolving" aria-live="polite">
        Résolution…
      </div>

      <div v-if="combatStore.error" class="combat-scene__error">
        {{ combatStore.error }}
      </div>
    </template>
  </section>
</template>

<style scoped>

.initiative-pill--next {
  border-color: var(--edge-frost);
  color: var(--frost);
}
.initiative-pill--next::after {
  content: '▸';
  margin-left: 4px;
  opacity: 0.8;
}

.combat-scene {
  display: grid;
  grid-template-rows: auto 1fr auto auto;
  grid-template-columns: 1fr;
  gap: 0;
  height: 100%;
  min-height: 0;
  position: relative;
}

.combat-scene__initiative {
  display: grid;
  grid-template-columns: auto 1fr;
  align-items: center;
  gap: var(--space-3);
  margin: 10px var(--space-4) 0;
  padding: 6px var(--space-3);
  min-height: 2.7rem;
  background: linear-gradient(180deg, oklch(0.22 0.045 60 / 0.88), oklch(0.17 0.04 60 / 0.9));
}

.combat-scene__initiative-title {
  font-family: var(--font-caps);
  font-size: 0.68rem;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: var(--ink-5);
  white-space: nowrap;
}

.combat-scene__initiative-list {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-width: 0;
  overflow-x: auto;
  scrollbar-width: thin;
}

.initiative-pill {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  flex: 0 0 auto;
  padding: 5px var(--space-3);
  border: 1px solid var(--line-soft);
  border-radius: 999px;
  font-family: var(--font-caps);
  font-size: 0.58rem;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--ink-3);
  background: oklch(0.18 0.035 60 / 0.68);
}

.initiative-pill--active {
  color: var(--gold);
  border-color: var(--edge-gold);
  box-shadow: 0 0 22px oklch(0.862 0.098 86 / 0.18);
}

.initiative-pill__slot {
  font-family: var(--font-mono);
  font-size: 0.54rem;
  color: var(--ink-5);
}

.initiative-pill--next .initiative-pill__slot {
  color: var(--gold);
}

.initiative-pill__dot {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: var(--frost);
  box-shadow: 0 0 10px color-mix(in oklch, var(--frost), transparent 40%);
}

.initiative-pill--enemy .initiative-pill__dot {
  background: var(--blood);
  box-shadow: 0 0 10px color-mix(in oklch, var(--blood), transparent 40%);
}

.combat-scene__arena {
  position: relative;
  min-height: 0;
  overflow-y: auto;
  padding: var(--space-3) var(--space-4);
}

.combat-scene__header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--space-3);
  margin-bottom: 14px;
}

.combat-scene__header-room {
  font-family: var(--font);
  font-size: 0.75rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.combat-scene__header-turn {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  letter-spacing: 0.06em;
  color: var(--ink-5);
}

.combat-scene__side-title {
  font-family: var(--font);
  font-size: 0.68rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  margin: 0 0 10px;
}

.combat-scene__side-title--foe { color: var(--blood); }
.combat-scene__side-title--allies { color: var(--frost); }

.combat-scene__difficulty-badge {
  margin-left: 8px;
  font-size: 0.62rem;
  letter-spacing: 0.08em;
  text-transform: none;
  color: var(--ink-4);
  border: 1px solid currentColor;
  border-radius: 999px;
  padding: 1px 8px;
  cursor: help;
}

.combat-scene__grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 14px;
}

.combat-scene__grid > * {
  flex: 1 1 320px;
  max-width: 380px;
}

.combat-scene__seuil {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 22px;
  margin: 26px 0 22px;
}

.seuil-line {
  flex: 1;
  height: 2px;
  filter: blur(0.4px);
  background: linear-gradient(90deg, transparent, var(--frost-dim));
}

.seuil-manometer {
  position: relative;
  flex: 0 0 auto;
  width: 96px;
  height: 96px;
  border-radius: 50%;
  border: 1px solid var(--line);
  background: radial-gradient(circle at 50% 40%, var(--panel), var(--void));
  box-shadow: inset 0 2px 10px oklch(0.08 0.015 48 / 0.75), 0 0 22px oklch(0.74 0.15 60 / 0.1);
  display: flex;
  align-items: flex-end;
  justify-content: center;
}

.seuil-manometer__ticks {
  position: absolute;
  inset: 7px;
  border-radius: 50%;
  opacity: 0.32;
  background: repeating-conic-gradient(from 215deg, var(--ink-5) 0deg 1.2deg, transparent 1.2deg 18deg);
  -webkit-mask: radial-gradient(circle, transparent 60%, #000 61%);
  mask: radial-gradient(circle, transparent 60%, #000 61%);
}

.seuil-manometer__needle {
  position: absolute;
  left: 50%;
  bottom: 50%;
  width: 2px;
  height: 34px;
  background: linear-gradient(var(--gold-hi), var(--gold-dim));
  transform-origin: bottom center;
  box-shadow: 0 0 6px var(--gold);
  animation: seuil-sweep 9s ease-in-out infinite;
}

.seuil-manometer__hub {
  position: absolute;
  left: 50%;
  bottom: 50%;
  width: 7px;
  height: 7px;
  transform: translate(-50%, 50%);
  border-radius: 50%;
  background: var(--gold);
  box-shadow: 0 0 8px var(--gold);
}

.seuil-manometer__label {
  position: relative;
  margin-bottom: 14px;
  font-family: var(--font);
  font-size: 0.5rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-5);
}

@keyframes seuil-sweep {
  0% { transform: translateX(-50%) rotate(-44deg); }
  50% { transform: translateX(-50%) rotate(40deg); }
  100% { transform: translateX(-50%) rotate(-44deg); }
}

.combat-float {
  position: absolute;
  left: 50%;
  top: 14%;
  z-index: 8;
  transform: translate(-50%, 0);
  pointer-events: none;
  font-family: var(--font-display);
  font-size: clamp(1.25rem, 2.2vw, 2rem);
  font-weight: 700;
  letter-spacing: 0.04em;
  color: var(--blood);
  text-shadow:
    0 0 10px color-mix(in oklch, var(--blood), transparent 45%),
    0 2px 0 oklch(0 0 0 / 0.55);
  animation: combat-float-rise 1200ms ease-out forwards;
}

.combat-float--heal {
  color: var(--sap);
  text-shadow:
    0 0 10px color-mix(in oklch, var(--sap), transparent 45%),
    0 2px 0 oklch(0 0 0 / 0.55);
}

.combat-float--guard {
  color: var(--gold);
  font-size: clamp(1rem, 1.8vw, 1.55rem);
  letter-spacing: 0.12em;
  text-shadow:
    0 0 10px color-mix(in oklch, var(--gold), transparent 38%),
    0 2px 0 oklch(0 0 0 / 0.55);
}

/* Magic-category hit: frost/violet tint instead of the default blood-red. */
.combat-float--magic {
  color: var(--frost);
  text-shadow:
    0 0 12px color-mix(in oklch, var(--frost), transparent 30%),
    0 0 22px color-mix(in oklch, oklch(0.62 0.19 300), transparent 55%),
    0 2px 0 oklch(0 0 0 / 0.55);
}

/* Critical hit: bigger, gold-hot, with its own punchier rise. */
.combat-float--critical {
  font-size: clamp(1.6rem, 2.8vw, 2.5rem);
  color: var(--gold);
  text-shadow:
    0 0 14px color-mix(in oklch, var(--gold), transparent 20%),
    0 0 26px color-mix(in oklch, var(--blood), transparent 45%),
    0 2px 0 oklch(0 0 0 / 0.55);
  animation: combat-float-rise 1200ms ease-out forwards, combat-float-crit-pulse 420ms ease-out;
}

/* Miss: no number, just a fading, sideways-drifting dodge notice. */
.combat-float--miss {
  color: var(--ink-4);
  font-family: var(--font-caps);
  font-size: clamp(0.7rem, 1.3vw, 0.95rem);
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  text-shadow: 0 2px 0 oklch(0 0 0 / 0.55);
  animation: combat-float-miss 1000ms ease-out forwards;
}

@keyframes combat-float-rise {
  0% { opacity: 0; transform: translate(-50%, 10px) scale(0.82); filter: blur(2px); }
  15% { opacity: 1; transform: translate(-50%, -4px) scale(1.08); filter: blur(0); }
  70% { opacity: 1; transform: translate(-50%, -34px) scale(1); }
  100% { opacity: 0; transform: translate(-50%, -58px) scale(0.94); }
}

@keyframes combat-float-crit-pulse {
  0% { transform: translate(-50%, 10px) scale(0.6); }
  40% { transform: translate(-50%, -10px) scale(1.35); }
  100% { transform: translate(-50%, -4px) scale(1); }
}

@keyframes combat-float-miss {
  0% { opacity: 0; transform: translate(-50%, 0) translateX(0); }
  20% { opacity: 1; transform: translate(-50%, -6px) translateX(-6px); }
  70% { opacity: 1; transform: translate(-50%, -18px) translateX(10px); }
  100% { opacity: 0; transform: translate(-50%, -26px) translateX(14px); }
}

.combat-scene__compose {
  position: relative;
  margin-top: 22px;
}

.combat-scene__skills-panel {
  border: 1px solid var(--edge-gold);
  background: linear-gradient(150deg, oklch(0.28 0.04 64 / 0.88), oklch(0.18 0.022 56 / 0.9));
}

.compose__header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--space-3);
  margin-bottom: 13px;
}

.compose__meter-toggle {
  margin-left: auto;
  font-size: 0.62rem;
}

.damage-modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.08 0.02 60 / 0.72);
  backdrop-filter: blur(4px);
  padding: var(--space-4);
}

.damage-modal {
  position: relative;
  width: min(960px, 100%);
  max-height: 85vh;
  overflow-y: auto;
  box-shadow: var(--shadow-deep);
}

.damage-modal__close {
  position: absolute;
  top: 14px;
  right: 14px;
  z-index: 1;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  border: 1px solid var(--edge-gold);
  background: var(--panel);
  color: var(--ink-2);
  cursor: pointer;
  font-size: 0.8rem;
}

.damage-modal__close:hover {
  border-color: var(--gold);
  color: var(--gold);
}

.compose__speaker-name {
  font-family: var(--font-display);
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--ink-2);
}

.compose__through {
  font-family: var(--font-mono);
  font-size: 0.7rem;
  color: var(--gold);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.compose__cancel {
  margin-top: 14px;
}

.combat-scene__placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  text-align: center;
  padding: var(--space-8);
}

.combat-scene__error {
  position: absolute;
  bottom: var(--space-2);
  left: 50%;
  transform: translateX(-50%);
  color: var(--blood);
  font-size: 0.78rem;
  padding: var(--space-2) var(--space-4);
  border: 1px solid color-mix(in oklch, var(--blood), transparent 50%);
  border-radius: var(--radius-sm);
  background: oklch(0.20 0.04 13 / 0.8);
  z-index: var(--z-popover);
}

.combat-scene__resolving {
  position: absolute;
  left: 50%;
  bottom: var(--space-6);
  transform: translateX(-50%);
  padding: var(--space-2) var(--space-4);
  border: 1px solid var(--edge-gold);
  border-radius: var(--radius-sm);
  background: oklch(0.20 0.04 60 / 0.85);
  color: var(--gold);
  font-family: var(--font-caps);
  font-size: 0.68rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  z-index: var(--z-popover);
  animation: breathe 1.4s ease-in-out infinite;
}

@keyframes breathe {
  0%, 100% { opacity: 0.65; }
  50% { opacity: 1; }
}

@media (max-width: 900px) {
  .combat-scene__grid > * {
    flex-basis: 100%;
    max-width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .combat-scene__resolving,
  .combat-float,
  .seuil-manometer__needle {
    animation: none;
  }
}
</style>
