<script setup lang="ts">
/**
 * Le champ de bataille tactique.
 *
 * Terrain, décor et figures sont peints au canvas — même forge de tuiles, même projection
 * isométrique et même bestiaire que l'exploration, parce que c'est littéralement la même salle,
 * vidée de ses nœuds. Seuls le bandeau d'initiative et la barre d'action restent du DOM : ce
 * sont des commandes, pas de la scène.
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
import { screenToCell } from '../../palace-map/composables/useTerrainDrawPlan';
import { drawAmbient, drawBackdrop } from '../../palace-map/composables/tilecraft';
import {
  battleCellKey,
  buildBattlePlan,
  isoUnit,
  manhattan,
  projectToScreen,
  reachableCellsFrom,
} from '../composables/useTacticalBattlePlan';
import { combatantSprite } from '../composables/useCombatantSprites';
import { FLOAT_MS, FLOAT_RISE_PX } from '../composables/useCombatPlayback';
import { useTacticalCombatStore } from '../stores/useTacticalCombatStore';

const props = defineProps<{
  runId: string;
  /** Le thème de la salle, qui donne aux tuiles leur matière peinte. */
  theme?: string;
  /** Sert de graine au décor de fond, pour qu'une salle garde le sien. */
  roomId?: string;
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

const prefersReducedMotion =
  typeof globalThis.matchMedia === 'function'
  && globalThis.matchMedia('(prefers-reduced-motion: reduce)').matches;

const battlefield = computed(() => store.combat?.battlefield ?? null);
const roomTheme = computed<RoomTheme>(() => (props.theme ?? 'Threshold') as RoomTheme);

const projectionParams = computed(() => ({
  canvasWidth: canvasSize.value.width,
  canvasHeight: canvasSize.value.height,
  gridWidth: battlefield.value?.width ?? 1,
  gridHeight: battlefield.value?.height ?? 1,
}));

const spriteDest = computed(() => {
  const { isoUnitX } = isoUnit(projectionParams.value);
  const destW = isoUnitX * 2.05;
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

function elevationAt(x: number, y: number): number {
  const field = battlefield.value;
  if (!field) return 0;

  return field.elevation[(y * field.width) + x] ?? 0;
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
  if (store.selectedSkillKey) return new Set();

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
    theme: roomTheme.value,
    ambientTint: 'neutral',
    reachableCells: reachableCells.value,
    targetableCells: targetableCells.value,
    hoveredCell: hoveredCell.value,
  });
});

/**
 * Les combattants, dans l'ordre du peintre — mêmes clés de profondeur que le terrain, pour
 * qu'une figure devant une tuile haute ne passe pas derrière elle.
 */
function buildCombatantPlan(now: number) {
  if (!battlefield.value || canvasSize.value.width === 0) return [];

  return store.allCombatants
    .map((unit) => {
      // La position affichée n'est pas celle du serveur tant que la chronologie se joue : une
      // figure en marche est interpolée entre deux cases, pas posée sur sa destination.
      const at = store.playback.positionOf(unit.combatant.id, { x: unit.x, y: unit.y }, now);
      const elevation = elevationAt(Math.round(at.x), Math.round(at.y));
      const { screenX, screenY } = projectToScreen(at.x, at.y, projectionParams.value);

      return {
        unit,
        elevation,
        screenX,
        screenY,
        sprite: combatantSprite(
          unit.combatant.sourceKey, unit.combatant.displayName, unit.combatant.id,
        ),
        // Le +0,5 place la figure entre sa propre tuile et la suivante : elle couvre le sol
        // sur lequel elle se tient sans masquer ce qui est peint devant elle.
        sortKey: ((at.x + at.y) * 4) + elevation + 0.5,
      };
    })
    .sort((a, b) => a.sortKey - b.sortKey);
}

/** Une silhouette de repli, pour un combattant que le bestiaire peint ne couvre pas encore. */
function paintPlaceholderFigure(
  ctx: CanvasRenderingContext2D,
  x: number, baseY: number, width: number, hostile: boolean,
) {
  const w = width * 0.28;
  const h = width * 0.5;

  ctx.save();
  ctx.fillStyle = hostile ? '#8e2b32' : '#6a6fb0';
  ctx.strokeStyle = '#efedf7';
  ctx.lineWidth = Math.max(1, width * 0.012);
  ctx.beginPath();
  ctx.ellipse(x, baseY - h * 0.15, w / 2, h * 0.42, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
  ctx.restore();
}

/** Jauge de vitalité + nom, peints au-dessus d'une figure. */
function paintCombatantChrome(
  ctx: CanvasRenderingContext2D,
  entry: ReturnType<typeof buildCombatantPlan>[number],
  topY: number,
  width: number,
) {
  const { combatant } = entry.unit;
  const hostile = combatant.side === 'Enemy';
  const ratio = Math.max(0, Math.min(1, combatant.currentVitality / Math.max(1, combatant.maxVitality)));

  const barW = width * 0.46;
  const barH = Math.max(3, width * 0.035);
  const barX = entry.screenX - (barW / 2);
  const barY = topY;

  ctx.save();

  ctx.fillStyle = 'rgba(0, 0, 0, 0.62)';
  ctx.fillRect(barX, barY, barW, barH);
  ctx.fillStyle = hostile ? '#e0605e' : '#86dcb4';
  ctx.fillRect(barX, barY, barW * ratio, barH);

  if (combatant.id === store.combat?.activeCombatantId) {
    ctx.strokeStyle = '#e6c273';
    ctx.lineWidth = Math.max(1, width * 0.014);
    ctx.strokeRect(barX - 1, barY - 1, barW + 2, barH + 2);
  }

  ctx.font = `${Math.max(9, Math.round(width * 0.1))}px ui-sans-serif, system-ui, sans-serif`;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'bottom';
  ctx.lineWidth = Math.max(2, width * 0.025);
  ctx.strokeStyle = 'rgba(0, 0, 0, 0.85)';
  ctx.fillStyle = '#efedf7';
  ctx.strokeText(combatant.displayName, entry.screenX, barY - 3);
  ctx.fillText(combatant.displayName, entry.screenX, barY - 3);

  ctx.restore();
}

function paintCanvas(timestamp: number) {
  const canvas = canvasEl.value;
  if (!canvas || canvasSize.value.width === 0) return;

  if (canvas.width !== canvasSize.value.width || canvas.height !== canvasSize.value.height) {
    canvas.width = canvasSize.value.width;
    canvas.height = canvasSize.value.height;
  }

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.clearRect(0, 0, canvas.width, canvas.height);

  // Ciel, source lumineuse, silhouettes de décor, brume, vignette — le champ de bataille se
  // tient dans une salle, pas dans le vide.
  drawBackdrop(
    ctx, canvas.width, canvas.height, roomTheme.value,
    prefersReducedMotion ? 0 : timestamp, props.roomId ?? 'combat',
    { scenery: true },
  );

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

    // La surbrillance de déplacement respire pour se lire comme une invitation ; le curseur
    // reste franc, pour que la case sous le pointeur ne se perde pas dans ce battement.
    if (entry.spriteKey.kind === 'highlight' && entry.spriteKey.variant === 'move'
        && !prefersReducedMotion) {
      ctx.globalAlpha = 0.55 + (0.45 * Math.sin((timestamp * 0.0022) + ((entry.x + entry.y) * 0.5)));
    }

    ctx.drawImage(sprite, dx, entry.screenY - (destH * GROUND_ANCHOR_RATIO), destW, destH);
    ctx.globalAlpha = 1;
  }

  for (const entry of buildCombatantPlan(timestamp)) {
    const lift = elevationLiftPx(entry.elevation);
    const groundY = entry.screenY - lift;

    if (entry.unit.combatant.status === 'Defeated') ctx.globalAlpha = 0.32;

    if (entry.sprite) {
      // Les figures sont cuites sur la toile haute : même ancre que les décors.
      ctx.drawImage(
        entry.sprite,
        entry.screenX - (destW / 2),
        groundY - (propH * PROP_GROUND_ANCHOR_RATIO),
        destW,
        propH,
      );
      paintCombatantChrome(ctx, entry, groundY - (propH * 0.42), destW);
    } else {
      paintPlaceholderFigure(ctx, entry.screenX, groundY, destW, entry.unit.combatant.side === 'Enemy');
      paintCombatantChrome(ctx, entry, groundY - (destW * 0.58), destW);
    }

    ctx.globalAlpha = 1;
  }

  if (!prefersReducedMotion) {
    drawAmbient(ctx, canvas.width, canvas.height, roomTheme.value, timestamp);
  }

  paintFloatingNumbers(ctx, timestamp);
}

/**
 * Les chiffres montent en dernier, en espace écran : ni un mur ni une figure ne doit pouvoir
 * cacher ce qu'un coup vient de coûter.
 */
function paintFloatingNumbers(ctx: CanvasRenderingContext2D, timestamp: number) {
  store.playback.pruneFloats(timestamp);

  const { destW } = spriteDest.value;

  for (const float of store.playback.floats) {
    const progress = (timestamp - float.bornAt) / FLOAT_MS;
    const { screenX, screenY } = projectToScreen(float.x, float.y, projectionParams.value);
    const lift = elevationLiftPx(elevationAt(float.x, float.y));

    ctx.save();
    ctx.globalAlpha = Math.max(0, 1 - progress);
    ctx.font = `600 ${Math.max(13, Math.round(destW * 0.17))}px ui-monospace, monospace`;
    ctx.textAlign = 'center';
    ctx.lineWidth = 3.5;
    ctx.strokeStyle = 'rgba(4, 5, 10, 0.9)';

    const y = screenY - lift - (destW * 0.5) - (progress * FLOAT_RISE_PX);
    ctx.strokeText(float.text, screenX, y);
    ctx.fillStyle = float.color;
    ctx.fillText(float.text, screenX, y);
    ctx.restore();
  }
}

/**
 * La case sous un point de l'écran.
 *
 * `screenToCell` teste le losange réellement peint de chaque tuile, élévation comprise — là
 * où l'inverse plat de la projection ne connaît que la grille au niveau zéro. Sur un terrain
 * en relief, l'inverse plat décale visiblement la sélection sous la tuile visée : c'est
 * exactement ce que ce chemin évite.
 */
function cellAtPointer(event: MouseEvent): { x: number; y: number } | null {
  const canvas = canvasEl.value;
  const field = battlefield.value;
  if (!canvas || !field) return null;

  const bounds = canvas.getBoundingClientRect();

  return screenToCell({
    // Le canvas est dimensionné en pixels CSS ; si sa taille d'affichage diffère de sa toile
    // (marges, zoom), ce rapport remet le pointeur dans le repère de ce qui est peint.
    screenX: (event.clientX - bounds.left) * (canvas.width / bounds.width),
    screenY: (event.clientY - bounds.top) * (canvas.height / bounds.height),
    gridWidth: field.width,
    gridHeight: field.height,
    canvasWidth: canvasSize.value.width,
    canvasHeight: canvasSize.value.height,
    elevation: field.elevation,
    // Une case impraticable est peinte en obstacle : `screenToCell` la déclasse au profit du
    // sol libre, ce qui est précisément le comportement voulu ici aussi.
    obstacleCells: new Set(
      field.walkable
        .map((walkable, index) =>
          walkable ? null : `${index % field.width},${Math.floor(index / field.width)}`)
        .filter((key): key is string => key !== null),
    ),
  });
}

function onCanvasClick(event: MouseEvent) {
  if (!store.isPlayerTurn || store.isLoading) return;

  const cell = cellAtPointer(event);
  if (!cell) return;

  // Une compétence armée détourne le clic : c'est ce qui distingue « où aller » de « quoi
  // frapper » sans exiger un second bouton.
  if (store.selectedSkillKey) {
    void store.useSkillAt(props.runId, store.selectedSkillKey, cell.x, cell.y);
    return;
  }

  void store.moveTo(props.runId, cell.x, cell.y);
}

function onCanvasMove(event: MouseEvent) {
  hoveredCell.value = cellAtPointer(event);
}

function renderLoop(timestamp: number) {
  // La frame suivante est demandée quoi qu'il arrive : sans ce garde-fou, une seule erreur de
  // peinture arrête la boucle définitivement et fige tout le champ de bataille, ce qui est
  // toujours pire que d'avoir sauté une image.
  try {
    paintCanvas(timestamp);
  } catch (error) {
    console.error('[combat tactique] frame ignorée', error);
  }

  frameHandle = globalThis.requestAnimationFrame(renderLoop);
}

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
  });

  observer.observe(canvas.parentElement);
  frameHandle = globalThis.requestAnimationFrame(renderLoop);
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
        >
          <span class="tbattle__initiative-rank">{{ index + 1 }}</span>
          <span class="tbattle__initiative-name">{{ unit.combatant.displayName }}</span>
          <span class="tbattle__initiative-hp">
            {{ unit.combatant.currentVitality }}/{{ unit.combatant.maxVitality }}
          </span>
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
    </div>

    <footer class="tbattle__actions">
      <template v-if="store.activeCombatant && store.isPlayerTurn">
        <div class="tbattle__active">
          <strong class="tbattle__active-name">{{ store.activeCombatant.combatant.displayName }}</strong>
          <span class="tbattle__active-stat">
            PV {{ store.activeCombatant.combatant.currentVitality }}/{{ store.activeCombatant.combatant.maxVitality }}
          </span>
          <span class="tbattle__active-stat">
            PP {{ store.activeCombatant.combatant.mana }}
          </span>
          <span class="tbattle__active-stat" :class="{ 'tbattle__active-stat--spent': store.activeCombatant.hasMoved }">
            {{ store.activeCombatant.hasMoved ? 'Déplacé' : `Déplacement ${store.activeCombatant.movementBudget}` }}
          </span>
          <span class="tbattle__active-stat" :class="{ 'tbattle__active-stat--spent': store.activeCombatant.hasActed }">
            {{ store.activeCombatant.hasActed ? 'A agi' : 'Action disponible' }}
          </span>
        </div>

        <div class="tbattle__skills">
          <button
            v-for="skill in store.activeSkills"
            :key="skill.key"
            type="button"
            class="tbattle__skill"
            :class="{ 'tbattle__skill--armed': skill.key === store.selectedSkillKey }"
            :disabled="store.activeCombatant.hasActed || store.isLoading"
            :title="`${skill.displayName} — ${skill.category === 'Magic' ? 'magique' : 'physique'}, PP ${skill.manaCost}`"
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

        <p class="tbattle__hint">
          {{ store.selectedSkillKey
            ? 'Clique une case dans la zone rouge pour lancer.'
            : 'Clique une case en surbrillance pour t’y rendre.' }}
        </p>
      </template>

      <p v-else class="tbattle__waiting">L'adversaire agit…</p>

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
  white-space: nowrap;
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
  gap: 0.35rem;
  padding: 0.15rem 0.55rem;
  border: 1px solid rgb(255 255 255 / 20%);
  border-radius: 999px;
  font-size: 0.8rem;
  white-space: nowrap;
}

.tbattle__initiative-entry--enemy { border-color: #8e2b32; }

.tbattle__initiative-entry--active {
  background: rgb(230 194 115 / 18%);
  border-color: #e6c273;
}

.tbattle__initiative-rank { opacity: 0.5; }
.tbattle__initiative-hp { opacity: 0.6; font-variant-numeric: tabular-nums; }

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

.tbattle__actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
  padding: 0.5rem 0.75rem;
}

.tbattle__active {
  display: flex;
  align-items: baseline;
  gap: 0.6rem;
  flex-wrap: wrap;
}

.tbattle__active-name { color: #e6c273; }

.tbattle__active-stat {
  font-size: 0.82rem;
  opacity: 0.78;
  font-variant-numeric: tabular-nums;
}

.tbattle__active-stat--spent { opacity: 0.38; text-decoration: line-through; }

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

.tbattle__skill--armed { background: rgb(224 96 94 / 22%); border-color: #e0605e; }
.tbattle__skill:disabled { opacity: 0.4; cursor: not-allowed; }

.tbattle__hint { margin: 0; font-size: 0.78rem; opacity: 0.55; }
.tbattle__waiting { opacity: 0.7; font-style: italic; margin: 0; }
.tbattle__error { color: #e0605e; margin: 0; }
</style>
