<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';

import { useRunStore } from '../../runs/stores/runStore';
import { useCombatLogMetrics } from '../composables/useCombatLogMetrics';
import { useCombatStore } from '../stores/useCombatStore';
import AtbGauge from './AtbGauge.vue';
import CombatantCard from './CombatantCard.vue';
import CombatLogPanel from './CombatLogPanel.vue';
import CombatMetersPanel from './CombatMetersPanel.vue';
import CombatOutcomePanel from './CombatOutcomePanel.vue';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
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

const nextCombatantId = computed<string | null>(() => {
  const activeId = activeCombatant.value?.id;
  const candidates = combatStore.allCombatants.filter(
    (c) => c.status !== 'Defeated' && c.id !== activeId,
  );
  if (candidates.length === 0) return null;
  return candidates.reduce((top, c) => ((c.atbGauge ?? 0) > (top.atbGauge ?? 0) ? c : top)).id;
});

const combatStore = useCombatStore();
const runStore = useRunStore();
const { state: metricsState } = useCombatLogMetrics(
  () => combatStore.logEntries,
  () => combatStore.combat,
);
const isDamageDrawerOpen = ref(false);
const hoveredEnemyId = ref<string | null>(null);

const activeCombatant = computed(() => combatStore.currentActor);
const isPlayerTurn = computed(() => combatStore.isPlayerTurn);
const hasSelectedSkill = computed(() => Boolean(combatStore.selectedSkill));
const selectedTarget = computed(() => {
  const id = combatStore.selectedTargetIds[0];
  if (!id) return null;
  return combatStore.findCombatantById(id);
});
const hoveredTarget = computed(() => {
  if (!hoveredEnemyId.value) return null;
  return combatStore.findCombatantById(hoveredEnemyId.value);
});
const visualTarget = computed(() => hoveredTarget.value ?? selectedTarget.value);
const activeAllyIndex = computed(() => {
  const id = activeCombatant.value?.id;
  if (!id) return 0;
  return Math.max(0, combatStore.allies.findIndex((combatant) => combatant.id === id));
});
const visualEnemyIndex = computed(() => {
  const id = visualTarget.value?.id;
  if (!id) return -1;
  return combatStore.enemies.findIndex((combatant) => combatant.id === id);
});

const targetThreadEnd = computed(() => {
  if (visualEnemyIndex.value < 0) return null;
  const targets = [
    { x: 56.0, y: 30.0 }, // 1 — front gauche
    { x: 76.0, y: 30.0 }, // 2 — front droite
    { x: 50.0, y: 18.0 }, // 3 — arrière gauche
    { x: 84.0, y: 18.0 }, // 4 — arrière droite
  ];
  return targets[visualEnemyIndex.value % targets.length];
});

const targetThreadPath = computed(() => {
  if (visualEnemyIndex.value < 0) return '';
  const starts = [
    { x: 30.9, y: 11.3 },
    { x: 30.9, y: 21.2 },
    { x: 30.9, y: 31.3 },
    { x: 30.9, y: 41.4 },
    { x: 30.9, y: 51.4 },
  ];
  const controls = [
    { lift: 7.0, bend: 0.52 },
    { lift: 6.2, bend: 0.55 },
    { lift: 8.0, bend: 0.52 },
    { lift: 6.6, bend: 0.56 },
  ];

  const start = starts[Math.min(activeAllyIndex.value, starts.length - 1)];
  const end = targetThreadEnd.value;
  if (!end) return '';
  const control = controls[visualEnemyIndex.value % controls.length];
  const cx = start.x + (end.x - start.x) * control.bend;
  const cy = Math.min(start.y, end.y) - control.lift;
  return `M ${start.x} ${start.y} Q ${cx} ${cy} ${end.x} ${end.y}`;
});

const actionPreview = computed(() => {
  if (combatStore.selectedSkill) return combatStore.selectedSkill.displayName;
  if (combatStore.selectedItem) return combatStore.selectedItem.displayName;
  return 'Frappe ciblée. +50 % si la cible saigne.';
});

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

function getEnemyJitter(index: number): string {
  const offsets = [
    'translate(4px, -6px) rotate(-0.5deg)',
    'translate(-3px, 8px) rotate(0.3deg)',
    'translate(6px, 2px) rotate(-0.8deg)',
    'translate(-5px, -4px) rotate(0.6deg)',
    'translate(2px, 10px) rotate(-0.2deg)',
    'translate(-7px, -3px) rotate(0.4deg)',
  ];
  return offsets[index % offsets.length];
}

function hpRatio(current: number, max: number): number {
  return max > 0 ? current / max : 0;
}

function enemyPositionClass(index: number): string {
  return `enemy-figure--${(index % 4) + 1}`;
}

function floatEventsFor(combatantId: string) {
  return combatStore.feedbackEvents.filter((event) => event.combatantId === combatantId);
}

function floatLabel(type: string, amount: number): string {
  if (type === 'heal') return `+${amount}`;
  if (type === 'guard') return `◇ ${amount}`;
  return `-${amount}`;
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
        <span class="combat-scene__initiative-title">MARKOV_STYLE_FIGHT</span>
        <div class="combat-scene__initiative-list">
          <span
            v-for="combatant in [...combatStore.enemies, ...combatStore.allies]"
            :key="combatant.id"
            class="initiative-pill"
            :class="{
              'initiative-pill--active': combatant.id === activeCombatant?.id,
              'initiative-pill--enemy': combatant.side === 'Enemy',
              'initiative-pill--next': combatant.id === nextCombatantId,
            }"
          >
            <span class="initiative-pill__dot" />
            {{ combatant.displayName }}
          </span>
        </div>
        <button class="damage-toggle" @click="isDamageDrawerOpen = !isDamageDrawerOpen">
          △ Relevé des dégâts ▸
        </button>
      </div>

      <!-- Main: face-à-face -->
      <div class="combat-scene__arena">
        <svg
          v-if="targetThreadPath"
          class="combat-scene__target-thread"
          :class="{ 'combat-scene__target-thread--hover': hoveredTarget }"
          viewBox="-1.1 5 100 120"
          preserveAspectRatio="none"
          aria-hidden="true"
        >
          <path :d="targetThreadPath" />
        </svg>

        <!-- Les Voix (allies) -->
        <div class="combat-scene__side combat-scene__side--voix">
          <p class="combat-scene__side-title">Alliés · {{ combatStore.allies.length }}</p>
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
            :is-acting="combatStore.recentlyActingId === combatant.id"
            @select="handleSelect"
          >
            <span
              v-for="event in floatEventsFor(combatant.id)"
              :key="event.id"
              class="combat-float"
              :class="`combat-float--${event.type}`"
            >
              {{ floatLabel(event.type, event.amount) }}
            </span>
          </CombatantCard>

        </div>

        <!-- Seuil (central divider) -->
        <div class="combat-scene__seuil">
          <div class="seuil-line" />
          <span class="seuil-glyph">◈</span>
          <div class="seuil-line" />
        </div>

        <!-- Les Manifestations (enemies) -->
        <div class="combat-scene__side combat-scene__side--manifestations">
          <p class="combat-scene__side-title combat-scene__side-title--foe">PLACEHOLDER_ENEMIES_GROUP_TYPE · {{ combatStore.enemies.length }}</p>
          <button
            v-for="(combatant, idx) in combatStore.enemies"
            :key="combatant.id"
            class="enemy-figure"
            :class="[
              enemyPositionClass(idx),
              {
                'enemy-figure--selected': combatStore.isSelectedTarget(combatant.id),
                'enemy-figure--selectable': canSelect(combatant.id),
                'enemy-figure--invalid': isInvalidTarget(combatant.id),
                'enemy-figure--active': isVisuallyActive(combatant.id),
                'enemy-figure--thinking': combatStore.thinkingCombatantId === combatant.id,
                'enemy-figure--defeated': combatant.status === 'Defeated',
              },
            ]"
            :style="{ '--enemy-jitter': getEnemyJitter(idx) }"
            :disabled="combatant.status === 'Defeated' || !canSelect(combatant.id)"
            @mouseenter="hoveredEnemyId = combatant.id"
            @mouseleave="hoveredEnemyId = null"
            @click="handleSelect(combatant.id)"
          >
            <span class="enemy-figure__shape" />
            <span
              v-for="event in floatEventsFor(combatant.id)"
              :key="event.id"
              class="combat-float combat-float--enemy"
              :class="`combat-float--${event.type}`"
            >
              {{ floatLabel(event.type, event.amount) }}
            </span>
            <span v-if="combatStore.isSelectedTarget(combatant.id) || hoveredEnemyId === combatant.id" class="enemy-figure__target">◎ cible</span>
            <span class="enemy-figure__name">{{ combatant.displayName }}</span>
            <span class="enemy-figure__archetype">{{ combatant.archetype }}</span>
            <span class="enemy-figure__type">
              <EmotionalTypeBadge :type="combatant.attackType ?? 'Neutral'" />
            </span>
            <span class="enemy-figure__substats">
              ⚔ {{ combatant.attackPower ?? 0 }} · ⛨ {{ combatant.defense ?? 0 }} · ⚡ {{ combatant.speed ?? 0 }} · ◎ {{ combatant.focus ?? 0 }}
            </span>
            <span v-if="combatant.guard > 0">Garde</span>
            <span class="enemy-figure__tags">
            </span>
            <AtbGauge class="enemy-figure__atb" :gauge="combatant.atbGauge ?? 0" :fill-per-tick="combatant.atbFillPerTick ?? 10" :active="isVisuallyActive(combatant.id)" />            
          <span class="enemy-figure__hp">
              <span class="enemy-figure__hp-bar">
                <span :style="{ width: hpRatio(combatant.currentVitality, combatant.maxVitality) * 100 + '%' }" />
              </span>
              <span v-if="combatant.guard > 0" class="enemy-figure__guard-value">◇ {{ combatant.guard }}</span>
              <span class="enemy-figure__hp-value">{{ combatant.currentVitality }} / {{ combatant.maxVitality }}</span>
            </span>
          </button>
        </div>
      </div>

      <div v-if="isDamageDrawerOpen" class="damage-drawer es-plate">
        <div class="damage-drawer__header">
          <span class="es-kicker">Relevé des dégâts</span>
          <button class="es-btn es-btn--ghost" @click="isDamageDrawerOpen = false">Fermer</button>
        </div>
        <CombatMetersPanel :metrics="metricsState" :combat="combatStore.combat" />
      </div>

      <div class="combat-scene__compose es-plate">
        <span class="es-corner es-corner--frost tl" />
        <span class="es-corner es-corner--frost tr" />
        <span class="es-corner es-corner--frost bl" />
        <span class="es-corner es-corner--frost br" />

        <div class="compose__speaker">
          <span class="es-label">La voix qui parle</span>
          <span class="compose__speaker-name">{{ activeCombatant?.displayName ?? '—' }}</span>
        </div>

        <div class="compose__main">
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

          <div class="compose__through">
            <span class="es-label">À travers la faille</span>
            <strong>{{ selectedTarget?.displayName ?? 'Choisir une cible' }}</strong>
            <span>{{ actionPreview }}</span>
          </div>
        </div>

        <button
          v-if="combatStore.selectedSkillKey || combatStore.selectedItemId || combatStore.selectedTargetIds.length > 0"
          class="es-btn es-btn--ghost compose__cancel"
          :disabled="combatStore.isResolvingAction"
          @click="handleClearSelection"
        >
          Annuler
        </button>
      </div>

      <CombatLogPanel :entries="combatStore.logEntries" />

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

.enemy-figure__atb {
  margin: 5px auto 0;
  width: 70%;
}

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
  grid-template-columns: auto 1fr minmax(14rem, 0.22fr);
  align-items: center;
  gap: var(--space-3);
  margin: 10px var(--space-4) 0;
  padding: 6px var(--space-3);
  min-height: 2.7rem;
  background: linear-gradient(180deg, oklch(0.22 0.045 272 / 0.88), oklch(0.17 0.04 272 / 0.9));
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
  background: oklch(0.18 0.035 272 / 0.68);
}

.initiative-pill--active {
  color: var(--gold);
  border-color: var(--edge-gold);
  box-shadow: 0 0 22px oklch(0.862 0.098 86 / 0.18);
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

.damage-toggle {
  height: 100%;
  border: 1px solid var(--line);
  border-radius: var(--radius-sm);
  background: oklch(0.17 0.035 272 / 0.55);
  color: var(--ink-4);
  font-family: var(--font-caps);
  font-size: 0.68rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  cursor: pointer;
}

.damage-toggle:hover {
  color: var(--ink-2);
  border-color: var(--edge-frost);
}

.combat-scene__arena {
  position: relative;
  display: grid;
  grid-template-columns: minmax(20rem, 0.78fr) auto minmax(32rem, 1.72fr);
  gap: 0;
  min-height: 0;
  overflow: hidden;
  padding: var(--space-2) var(--space-4) var(--space-3);
}

.combat-scene__target-thread {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  z-index: 3;
  filter: drop-shadow(0 0 5px color-mix(in oklch, var(--frost), transparent 56%));
  opacity: 0.5;
  pointer-events: none;
  transition: opacity 0.18s ease, filter 0.18s ease, border-color 0.18s ease;
}

.combat-scene__target-thread path {
  fill: none;
  stroke: color-mix(in oklch, var(--frost), transparent 28%);
  stroke-width: 0.18;
  stroke-dasharray: 0.75 1.15;
  stroke-linecap: round;
}

.combat-scene__target-thread circle {
  fill: var(--frost);
  opacity: 0.75;
  filter: drop-shadow(0 0 4px color-mix(in oklch, var(--frost), transparent 20%));
}

.combat-scene__target-thread--hover {
  opacity: 1;
  filter: drop-shadow(0 0 14px color-mix(in oklch, var(--frost), transparent 18%));
}

.combat-scene__target-thread--hover path {
  stroke: var(--frost);
  stroke-width: 0.24;
}

.combat-scene__side {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding: var(--space-1);
  min-width: 0;
}

.combat-scene__side-title {
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: var(--ink-4);
  margin: 0 0 var(--space-1);
}

.combat-scene__side-title--foe {
  grid-column: 1 / -1;
  text-align: right;
  color: var(--blood-dim);
}

.combat-scene__side--voix {
  align-items: stretch;
}

.combat-scene__side--manifestations {
  position: relative;
  display: block;
  min-height: 29rem;
  overflow: hidden;
}

.combat-scene__seuil {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-2);
  padding: 0 var(--space-4);
  align-self: stretch;
}

.seuil-line {
  flex: 1;
  width: 1px;
  background: linear-gradient(to bottom, transparent, var(--line-strong), transparent);
}

.seuil-glyph {
  font-size: 1.2rem;
  color: var(--ink-4);
  opacity: 0.5;
}

.enemy-figure {
  --enemy-width: 8.5rem;
  --enemy-height: 15rem;
  position: absolute;
  left: var(--enemy-x);
  top: var(--enemy-y);
  width: var(--enemy-width);
  min-height: calc(var(--enemy-height) + 5.8rem);
  transform: translate(-50%, 0) var(--enemy-jitter, none);
  border: none;
  background: transparent;
  color: var(--ink-3);
  font-family: inherit;
  text-align: center;
  cursor: default;
  padding: 0;
  transition: opacity 0.18s ease, filter 0.18s ease;
}

.enemy-figure--selectable { cursor: pointer; }
.enemy-figure--invalid { opacity: 0.36; }
.enemy-figure--defeated { opacity: 0.2; filter: grayscale(1); }

.enemy-figure__shape {
  display: block;
  width: var(--enemy-width);
  height: var(--enemy-height);
  margin: 0 auto var(--space-1);
  border-radius: 46% 46% 54% 54% / 14% 14% 88% 88%;
  background:
    radial-gradient(circle at 38% 20%, oklch(0.48 0.04 272 / 0.28), transparent 30%),
    linear-gradient(180deg, oklch(0.25 0.045 272 / 0.9), oklch(0.08 0.025 272 / 0.92));
  border: 1px solid oklch(0.55 0.07 272 / 0.18);
  box-shadow:
    inset 0 20px 35px oklch(0.65 0.07 272 / 0.08),
    0 18px 40px oklch(0 0 0 / 0.36);
}

.enemy-figure--selected .enemy-figure__shape,
.enemy-figure:hover .enemy-figure__shape {
  border-color: var(--frost);
  box-shadow:
    inset 0 20px 35px oklch(0.65 0.07 272 / 0.14),
    0 0 0 1px color-mix(in oklch, var(--frost), transparent 20%),
    0 0 45px color-mix(in oklch, var(--frost), transparent 78%);
}

.enemy-figure--thinking .enemy-figure__shape,
.enemy-figure--active .enemy-figure__shape {
  border-color: var(--gold);
  outline: 1px solid color-mix(in oklch, var(--gold), transparent 28%);
  outline-offset: 7px;
  box-shadow:
    inset 0 20px 35px oklch(0.65 0.07 272 / 0.12),
    0 0 0 1px color-mix(in oklch, var(--gold), transparent 28%),
    0 0 54px color-mix(in oklch, var(--gold), transparent 76%);
  animation: enemy-thinking-frame 1.4s ease-in-out infinite;
}

.enemy-figure__target {
  position: absolute;
  top: -1rem;
  left: 50%;
  transform: translateX(-50%);
  font-family: var(--font-caps);
  font-size: 0.54rem;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--frost);
}

.enemy-figure__name {
  display: block;
  font-family: var(--font-display);
  font-size: clamp(1rem, 1.7vw, 1.45rem);
  color: var(--ink-2);
  line-height: 1.05;
  text-shadow: 0 2px 10px oklch(0 0 0 / 0.4);
}

.enemy-figure__archetype {
  display: block;
  margin-top: 2px;
  font-family: var(--font-caps);
  font-size: 0.6rem;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--ink-5);
}

.enemy-figure__type {
  display: flex;
  justify-content: center;
  margin-top: 3px;
}

.enemy-figure__substats {
  display: block;
  margin-top: 3px;
  font-family: var(--font-mono);
  font-size: 0.54rem;
  letter-spacing: 0.04em;
  color: var(--ink-5);
}

.enemy-figure__tags {
  display: flex;
  justify-content: center;
  gap: 4px;
  min-height: 1.1rem;
  margin-top: 2px;
}

.enemy-figure__tags span {
  border: 1px solid var(--edge-gold);
  border-radius: 3px;
  padding: 1px 7px;
  font-family: var(--font-caps);
  font-size: 0.56rem;
  letter-spacing: 0.12em;
  color: var(--gold);
  background: oklch(0.862 0.098 86 / 0.08);
}

.enemy-figure__hp {
  display: grid;
  grid-template-columns: 2.2rem minmax(3.2rem, 1fr) auto auto;
  gap: 7px;
  align-items: center;
  margin-top: 4px;
}

.enemy-figure__guard-value {
  font-family: var(--font-mono);
  font-size: 0.58rem;
  color: var(--frost-dim);
  white-space: nowrap;
}

.enemy-figure__hp-chip,
.enemy-figure__hp-bar {
  height: 3px;
  background: oklch(0.05 0.01 272 / 0.92);
  border-radius: 999px;
  overflow: hidden;
}

.enemy-figure__hp-chip::before,
.enemy-figure__hp-bar span {
  content: '';
  display: block;
  height: 100%;
  border-radius: inherit;
}

.enemy-figure__hp-bar span { background: var(--blood); }

.enemy-figure__hp-chip::before {
  width: 48%;
  background: linear-gradient(90deg, var(--gold-dim), var(--gold));
  animation: enemy-atb 2.8s ease-in-out infinite alternate;
}

.enemy-figure__hp-value {
  font-family: var(--font-mono);
  font-size: 0.58rem;
  color: var(--ink-5);
  white-space: nowrap;
}

.enemy-figure--1 { --enemy-x: 32%; --enemy-y: 9rem;   --enemy-width: 9.4rem; --enemy-height: 16.5rem; }
.enemy-figure--2 { --enemy-x: 66%; --enemy-y: 9rem;   --enemy-width: 9.4rem; --enemy-height: 16.5rem; }
.enemy-figure--3 { --enemy-x: 18%; --enemy-y: 2.5rem; --enemy-width: 7.6rem; --enemy-height: 13.5rem; }
.enemy-figure--4 { --enemy-x: 82%; --enemy-y: 2.5rem; --enemy-width: 7.6rem; --enemy-height: 13.5rem; }

.damage-drawer {
  position: absolute;
  left: var(--space-4);
  right: var(--space-4);
  bottom: 6.6rem;
  z-index: var(--z-popover);
  padding: var(--space-3);
  background: oklch(0.15 0.04 272 / 0.96);
  box-shadow: var(--shadow-deep);
}

.damage-drawer__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  margin-bottom: var(--space-2);
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

.combat-float--enemy {
  top: 42%;
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

@keyframes combat-float-rise {
  0% { opacity: 0; transform: translate(-50%, 10px) scale(0.82); filter: blur(2px); }
  15% { opacity: 1; transform: translate(-50%, -4px) scale(1.08); filter: blur(0); }
  70% { opacity: 1; transform: translate(-50%, -34px) scale(1); }
  100% { opacity: 0; transform: translate(-50%, -58px) scale(0.94); }
}

@keyframes enemy-thinking-frame {
  0%, 100% { outline-offset: 6px; filter: brightness(1); }
  50% { outline-offset: 11px; filter: brightness(1.1); }
}

@keyframes enemy-atb {
  0% { width: 24%; opacity: 0.55; }
  100% { width: 78%; opacity: 1; }
}

.combat-scene__compose {
  position: relative;
  display: grid;
  grid-template-columns: minmax(7rem, auto) 1fr auto;
  gap: var(--space-3);
  align-items: stretch;
  margin: 0 var(--space-4) var(--space-2);
  padding: var(--space-2) var(--space-3);
}

.compose__speaker {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 2px;
  min-width: 0;
}

.compose__speaker-name {
  font-family: var(--font-display);
  font-size: 1.05rem;
  font-weight: 600;
  color: var(--ink);
}

.compose__main {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(12rem, 0.65fr);
  gap: var(--space-3);
  min-width: 0;
}

.compose__through {
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 2px;
  min-width: 0;
  padding-left: var(--space-3);
  border-left: 1px solid var(--line-soft);
}

.compose__through strong {
  font-family: var(--font);
  font-size: 0.86rem;
  color: var(--ink-2);
  font-weight: 500;
}

.compose__through span:last-child {
  font-size: 0.7rem;
  color: var(--ink-4);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.compose__cancel {
  align-self: center;
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
  background: oklch(0.20 0.04 272 / 0.85);
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
  .combat-scene {
    grid-template-rows: auto 1fr auto auto;
    grid-template-columns: 1fr;
  }

  .combat-scene__arena {
    grid-template-columns: 1fr;
    grid-template-rows: auto auto auto;
  }

  .combat-scene__seuil {
    flex-direction: row;
    padding: var(--space-2) 0;
  }

  .combat-scene__side--manifestations {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
    gap: var(--space-3);
    min-height: auto;
    overflow: visible;
  }

  .enemy-figure {
    position: relative;
    left: auto;
    top: auto;
    width: 100%;
    min-height: auto;
    transform: none;
  }

  .enemy-figure__shape {
    max-width: 8rem;
    height: 11rem;
  }

  .combat-scene__compose,
  .compose__main {
    grid-template-columns: 1fr;
  }

  .compose__through {
    padding-left: 0;
    border-left: none;
    border-top: 1px solid var(--line-soft);
    padding-top: var(--space-2);
  }

  .seuil-line {
    height: 1px;
    width: auto;
    flex: 1;
    background: linear-gradient(to right, transparent, var(--line-strong), transparent);
  }
}

@media (prefers-reduced-motion: reduce) {
  .combat-scene__resolving,
  .combat-float,
  .enemy-figure--thinking .enemy-figure__shape,
  .enemy-figure__hp-chip::before {
    animation: none;
  }
}
</style>
