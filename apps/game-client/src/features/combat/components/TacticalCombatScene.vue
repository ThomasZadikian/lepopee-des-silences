<script setup lang="ts">
/**
 * Le champ de bataille tactique.
 *
 * Terrain peint au canvas — même forge de tuiles et même projection isométrique que
 * l'exploration, parce que c'est littéralement la même salle, vidée de ses nœuds. Les
 * combattants, eux, restent des overlays DOM : ils portent des jauges, des noms et des
 * transitions CSS qu'un blit canvas rendrait pénibles à animer.
 */
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

import {
  GROUND_ANCHOR_RATIO,
  PROP_GROUND_ANCHOR_RATIO,
  TERRAIN_SPRITE_CONSTANTS,
  useTerrainSprites,
  usesPropRect,
  type RoomTheme,
} from '../../palace-map/composables/useTerrainSprites';
import { unprojectFromScreen } from '../../palace-map/composables/useTerrainDrawPlan';
import {
  battleCellKey,
  buildBattlePlan,
  isoUnit,
  manhattan,
  projectToScreen,
  reachableCellsFrom,
} from '../composables/useTacticalBattlePlan';
import { useTacticalCombatStore } from '../stores/useTacticalCombatStore';

import type { TacticalCombatantRuntimeDto } from '../types/combatContracts';

const props = defineProps<{
  runId: string;
  /** Le thème de la salle, qui donne aux tuiles leur matière peinte. */
  theme?: string;
}>();

const emit = defineEmits<{
  (event: 'combat-completed'): void;
  (event: 'combat-failed'): void;
}>();

const store = useTacticalCombatStore();
const { getSprite } = useTerrainSprites();

const canvasEl = ref<HTMLCanvasElement | null>(null);
const canvasSize = ref({ width: 0, height: 0 });
const hoveredCell = ref<{ x: number; y: number } | null>(null);

let frameHandle = 0;
let observer: ResizeObserver | null = null;

const battlefield = computed(() => store.combat?.battlefield ?? null);

const projectionParams = computed(() => ({
  canvasWidth: canvasSize.value.width,
  canvasHeight: canvasSize.value.height,
  gridWidth: battlefield.value?.width ?? 1,
  gridHeight: battlefield.value?.height ?? 1,
}));

const spriteDest = computed(() => {
  const { isoUnitX } = isoUnit(projectionParams.value);
  const destW = isoUnitX * 2.05;
  // La largeur de la toile est celle de la tuile de base ; les deux hauteurs (sol et toile
  // haute) en dérivent, exactement comme côté exploration.
  const { BASE_TILE_W, SPRITE_H, PROP_SPRITE_H } = TERRAIN_SPRITE_CONSTANTS;

  return {
    destW,
    destH: destW / (BASE_TILE_W / SPRITE_H),
    propH: destW / (BASE_TILE_W / PROP_SPRITE_H),
  };
});

/** De combien de pixels une tuile à cette élévation se soulève, à l'échelle courante. */
function elevationLiftPx(level: number): number {
  const scale = spriteDest.value.destH / TERRAIN_SPRITE_CONSTANTS.SPRITE_H;

  return level * TERRAIN_SPRITE_CONSTANTS.BASE_STEP_PX * scale;
}

const occupiedKeys = computed(
  () =>
    new Set(
      store.allCombatants
        .filter((c) => c.combatant.status !== 'Defeated')
        .map((c) => battleCellKey(c.x, c.y)),
    ),
);

/**
 * Aperçu des cases atteignables. Recalculé côté client pour que le joueur voie où il peut
 * aller <b>avant</b> de cliquer — le serveur reste seul juge, et son refus prime.
 */
const reachableCells = computed<Set<string>>(() => {
  const field = battlefield.value;
  const active = store.activeCombatant;

  if (!field || !active || !store.isPlayerTurn || active.hasMoved) return new Set();

  const occupied = new Set(occupiedKeys.value);
  occupied.delete(battleCellKey(active.x, active.y));

  return reachableCellsFrom(
    {
      gridWidth: field.width,
      gridHeight: field.height,
      elevation: field.elevation,
      walkable: field.walkable,
    },
    active,
    active.movementBudget,
    occupied,
  );
});

/**
 * Les cases que la compétence armée peut couvrir. Aperçu de portée volontairement grossier —
 * un losange autour du lanceur — là où le serveur applique en plus la ligne de vue.
 */
const targetableCells = computed<Set<string>>(() => {
  const field = battlefield.value;
  const active = store.activeCombatant;
  const skill = store.selectedSkill;

  if (!field || !active || !skill) return new Set();

  // Miroir de `TacticalRange.For` côté serveur. Voir la note d'aperçu ci-dessus.
  const range = skill.skillType === 'Heal' || skill.skillType === 'Guard'
    ? 3
    : skill.category === 'Magic' ? 4 : 1;

  const cells = new Set<string>();

  for (let y = 0; y < field.height; y += 1) {
    for (let x = 0; x < field.width; x += 1) {
      if (manhattan(active, { x, y }) <= range) cells.add(battleCellKey(x, y));
    }
  }

  return cells;
});

const drawPlan = computed(() => {
  const field = battlefield.value;
  if (!field || canvasSize.value.width === 0) return [];

  return buildBattlePlan({
    canvasWidth: canvasSize.value.width,
    canvasHeight: canvasSize.value.height,
    gridWidth: field.width,
    gridHeight: field.height,
    elevation: field.elevation,
    walkable: field.walkable,
    theme: (props.theme ?? 'Threshold') as RoomTheme,
    ambientTint: 'neutral',
    reachableCells: reachableCells.value,
    targetableCells: targetableCells.value,
    hoveredCell: hoveredCell.value,
  });
});

/** Position à l'écran d'un combattant, jeton DOM compris — même maths que le canvas. */
function tokenStyle(unit: TacticalCombatantRuntimeDto) {
  const { screenX, screenY } = projectToScreen(unit.x, unit.y, projectionParams.value);
  const field = battlefield.value;
  const elevation = field ? field.elevation[(unit.y * field.width) + unit.x] ?? 0 : 0;

  return {
    left: `${screenX}px`,
    top: `${screenY - elevationLiftPx(elevation)}px`,
  };
}

function paintCanvas() {
  const canvas = canvasEl.value;
  if (!canvas || canvasSize.value.width === 0) return;

  if (canvas.width !== canvasSize.value.width || canvas.height !== canvasSize.value.height) {
    canvas.width = canvasSize.value.width;
    canvas.height = canvasSize.value.height;
  }

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.clearRect(0, 0, canvas.width, canvas.height);

  const { destW, destH, propH } = spriteDest.value;

  for (const entry of drawPlan.value) {
    const sprite = getSprite(entry.spriteKey);
    const dx = entry.screenX - (destW / 2);

    if (usesPropRect(entry.spriteKey)) {
      // Un obstacle part du sol : son élévation est déjà cuite dans la silhouette, il ne
      // reçoit donc aucun soulèvement supplémentaire.
      ctx.drawImage(
        sprite, dx, entry.screenY - (propH * PROP_GROUND_ANCHOR_RATIO), destW, propH,
      );
      continue;
    }

    ctx.drawImage(sprite, dx, entry.screenY - (destH * GROUND_ANCHOR_RATIO), destW, destH);
  }
}

function onCanvasClick(event: MouseEvent) {
  const canvas = canvasEl.value;
  const field = battlefield.value;
  if (!canvas || !field || !store.isPlayerTurn || store.isLoading) return;

  const bounds = canvas.getBoundingClientRect();
  const cell = unprojectFromScreen(
    event.clientX - bounds.left,
    event.clientY - bounds.top,
    projectionParams.value,
  );

  if (cell.x < 0 || cell.y < 0 || cell.x >= field.width || cell.y >= field.height) return;

  // Une compétence armée détourne le clic : c'est ce qui distingue « où aller » de « quoi
  // frapper » sans exiger un second bouton.
  if (store.selectedSkillKey) {
    void store.useSkillAt(props.runId, store.selectedSkillKey, cell.x, cell.y);
    return;
  }

  void store.moveTo(props.runId, cell.x, cell.y);
}

function onCanvasMove(event: MouseEvent) {
  const canvas = canvasEl.value;
  const field = battlefield.value;
  if (!canvas || !field) return;

  const bounds = canvas.getBoundingClientRect();
  const cell = unprojectFromScreen(
    event.clientX - bounds.left,
    event.clientY - bounds.top,
    projectionParams.value,
  );

  hoveredCell.value =
    cell.x >= 0 && cell.y >= 0 && cell.x < field.width && cell.y < field.height ? cell : null;
}

function scheduleRepaint() {
  globalThis.cancelAnimationFrame(frameHandle);
  frameHandle = globalThis.requestAnimationFrame(() => paintCanvas());
}

watch(drawPlan, scheduleRepaint, { deep: false });

// Le combat s'achève côté serveur ; la page parente décide de la suite (récompense, sortie).
watch(
  () => store.combat?.status,
  (status) => {
    if (status === 'Completed') emit('combat-completed');
    if (status === 'Failed') emit('combat-failed');
  },
);

onMounted(() => {
  const canvas = canvasEl.value;
  if (!canvas?.parentElement) return;

  observer = new ResizeObserver(([entry]) => {
    canvasSize.value = {
      width: Math.round(entry.contentRect.width),
      height: Math.round(entry.contentRect.height),
    };
    scheduleRepaint();
  });

  observer.observe(canvas.parentElement);
});

onBeforeUnmount(() => {
  globalThis.cancelAnimationFrame(frameHandle);
  observer?.disconnect();
  observer = null;
});
</script>

<template>
  <section v-if="store.combat" class="tbattle">
    <header class="tbattle__header">
      <span class="tbattle__round">Round {{ store.combat.roundNumber }}</span>

      <!-- L'ordre d'action annoncé à l'avance : la contrepartie de l'abandon du tempo. -->
      <ol class="tbattle__initiative">
        <li
          v-for="(unit, index) in store.initiativeQueue"
          :key="unit.combatant.id"
          class="tbattle__initiative-entry"
          :class="{
            'tbattle__initiative-entry--active': unit.combatant.id === store.combat.activeCombatantId,
            'tbattle__initiative-entry--enemy': unit.combatant.side === 'Enemy',
          }"
          :title="unit.combatant.displayName"
        >
          <span class="tbattle__initiative-rank">{{ index + 1 }}</span>
          {{ unit.combatant.displayName }}
        </li>
      </ol>
    </header>

    <div class="tbattle__board">
      <canvas
        ref="canvasEl"
        class="tbattle__canvas"
        @click="onCanvasClick"
        @mousemove="onCanvasMove"
        @mouseleave="hoveredCell = null"
      />

      <div
        v-for="unit in store.allCombatants"
        :key="unit.combatant.id"
        class="tbattle__token"
        :class="{
          'tbattle__token--enemy': unit.combatant.side === 'Enemy',
          'tbattle__token--active': unit.combatant.id === store.combat.activeCombatantId,
          'tbattle__token--defeated': unit.combatant.status === 'Defeated',
        }"
        :style="tokenStyle(unit)"
      >
        <span class="tbattle__token-name">{{ unit.combatant.displayName }}</span>
        <span class="tbattle__token-bar">
          <span
            class="tbattle__token-bar-fill"
            :style="{
              width: `${Math.max(0, Math.round(
                (unit.combatant.currentVitality / Math.max(1, unit.combatant.maxVitality)) * 100,
              ))}%`,
            }"
          />
        </span>
      </div>
    </div>

    <footer class="tbattle__actions">
      <p v-if="!store.isPlayerTurn" class="tbattle__waiting">
        L'adversaire agit…
      </p>

      <template v-else>
        <div class="tbattle__skills">
          <button
            v-for="skill in store.activeSkills"
            :key="skill.key"
            type="button"
            class="tbattle__skill"
            :class="{ 'tbattle__skill--armed': skill.key === store.selectedSkillKey }"
            :disabled="store.activeCombatant?.hasActed || store.isLoading"
            @click="store.selectSkill(skill.key)"
          >
            {{ skill.displayName }}
          </button>
        </div>

        <button
          type="button"
          class="tbattle__end-turn"
          :disabled="store.isLoading"
          @click="store.endTurn(props.runId)"
        >
          Finir le tour
        </button>
      </template>

      <p v-if="store.error" class="tbattle__error" role="alert">{{ store.error }}</p>
    </footer>
  </section>
</template>

<style scoped>
.tbattle {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.tbattle__header {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.5rem 0.75rem;
}

.tbattle__round {
  font-variant: small-caps;
  letter-spacing: 0.08em;
  opacity: 0.8;
}

.tbattle__initiative {
  display: flex;
  gap: 0.35rem;
  margin: 0;
  padding: 0;
  list-style: none;
  overflow-x: auto;
}

.tbattle__initiative-entry {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.15rem 0.5rem;
  border: 1px solid rgb(255 255 255 / 20%);
  border-radius: 999px;
  font-size: 0.8rem;
  white-space: nowrap;
}

.tbattle__initiative-entry--enemy { border-color: var(--blood, #a33); }
.tbattle__initiative-entry--active { background: rgb(255 255 255 / 14%); }

.tbattle__initiative-rank { opacity: 0.55; }

.tbattle__board {
  position: relative;
  flex: 1;
  min-height: 0;
}

.tbattle__canvas {
  display: block;
  width: 100%;
  height: 100%;
}

.tbattle__token {
  position: absolute;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.15rem;
  /* Le jeton est ancré sur le centre de sa case, remonté de sa propre hauteur. */
  transform: translate(-50%, -100%);
  pointer-events: none;
  font-size: 0.7rem;
  text-shadow: 0 1px 2px rgb(0 0 0 / 80%);
  transition: left 220ms ease-out, top 220ms ease-out;
}

.tbattle__token--defeated { opacity: 0.3; filter: grayscale(1); }
.tbattle__token--active { text-shadow: 0 0 6px var(--gold, #d9b45b); }

.tbattle__token-bar {
  display: block;
  width: 2.5rem;
  height: 0.25rem;
  background: rgb(0 0 0 / 60%);
  border-radius: 999px;
  overflow: hidden;
}

.tbattle__token-bar-fill {
  display: block;
  height: 100%;
  background: var(--frost, #7fd);
  transition: width 220ms ease-out;
}

.tbattle__token--enemy .tbattle__token-bar-fill { background: var(--blood, #a33); }

.tbattle__actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding: 0.5rem 0.75rem;
}

.tbattle__skills { display: flex; gap: 0.4rem; flex-wrap: wrap; }

.tbattle__skill,
.tbattle__end-turn {
  padding: 0.3rem 0.7rem;
  border: 1px solid rgb(255 255 255 / 25%);
  border-radius: 4px;
  background: transparent;
  color: inherit;
  cursor: pointer;
}

.tbattle__skill--armed { background: rgb(255 255 255 / 18%); }
.tbattle__skill:disabled { opacity: 0.4; cursor: not-allowed; }

.tbattle__waiting { opacity: 0.7; font-style: italic; margin: 0; }
.tbattle__error { color: var(--blood, #c55); margin: 0; }

@media (prefers-reduced-motion: reduce) {
  .tbattle__token,
  .tbattle__token-bar-fill { transition: none; }
}
</style>
