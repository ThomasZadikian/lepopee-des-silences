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
  resolveRoomVisual,
  type RoomTheme,
  type RenderTheme,
} from '../../palace-map/composables/useTerrainSprites';
import { screenToCell } from '../../palace-map/composables/useTerrainDrawPlan';
import {
  drawAmbient,
  drawBackdrop,
  drawCombatGrade,
  drawUnitRing,
  drawActionPips,
  drawImpactFx,
  drawDeployFx,
  drawFireFx,
  drawStarFx,
  themeLabel,
  RISK_TIERS,
  ALLY_RIM,
  ENEMY_RIM,
} from '../../palace-map/composables/tilecraft';
import {
  battleCellKey,
  buildBattlePlan,
  hasLos,
  isoUnit,
  manhattan,
  projectToScreen,
  reachableCellsFrom,
  reachableCellsWithPathsFrom,
  type BattleCell,
} from '../composables/useTacticalBattlePlan';
import { combatantSprite, fallbackPropFor } from '../composables/useCombatantSprites';
import { FLOAT_MS, FLOAT_RISE_PX, useCombatPlayback } from '../composables/useCombatPlayback';
import { sortIdForSkillKey, useSortEffects } from '../composables/useSortEffects';
import {
  tacticalSkillProfile,
  type TacticalShape,
} from '../composables/useTacticalSkillProfile';
import { useTacticalCombatStore } from '../stores/useTacticalCombatStore';

import type { CombatantSkillRuntimeDto } from '../types/combatContracts';

const props = defineProps<{
  runId: string;
  theme?: string;
  catalogRoomKey?: string;
  roomId?: string;
}>();

const emit = defineEmits<{
  (event: 'combat-completed'): void;
  (event: 'combat-failed'): void;
}>();

const store = useTacticalCombatStore();
const { getSprite } = useTerrainSprites();
const sortEffects = useSortEffects();
const playback = useCombatPlayback();

const canvasEl = ref<HTMLCanvasElement | null>(null);
const canvasSize = ref({ width: 0, height: 0 });
const hoveredCell = ref<{ x: number; y: number } | null>(null);

const DEPLOY_DURATION_MS = 1200;
const deployStartedAt = ref<number | null>(null);

let frameHandle = 0;
let observer: ResizeObserver | null = null;

const prefersReducedMotion =
  typeof globalThis.matchMedia === 'function'
  && globalThis.matchMedia('(prefers-reduced-motion: reduce)').matches;

const battlefield = computed(() => store.combat?.battlefield ?? null);
const roomTheme = computed<RoomTheme>(() =>
  resolveRoomVisual(props.catalogRoomKey, (props.theme ?? 'Threshold') as RenderTheme),
);

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

/**
 * De combien de pixels une tuile à cette élévation se soulève, à l'échelle courante.
 */
function elevationLiftPx(level: number): number {
  const scale = spriteDest.value.destH / TERRAIN_SPRITE_CONSTANTS.SPRITE_H;

  return level * TERRAIN_SPRITE_CONSTANTS.BASE_STEP_PX * scale;
}

/**
 * Dans le bestiaire, les figures sont peintes avec `ctx.translate(0, PROP_EXTRA_H)` et
 * `base: centerY(0) + 4` — la base réelle est donc 4px plus bas que l'ancrage théorique
 * (92→96) dans l'espace SPRITE_H. Ce ratio permet de recaler l'anneau et les pip sous
 * les pieds du combattant indépendamment de l'échelle d'affichage.
 */
const FIGURE_BASE_OFFSET = 4 / TERRAIN_SPRITE_CONSTANTS.SPRITE_H; // 4/170 ≈ 0.0235

// ── Backdrop : peint, mis en cache ──────────────────────────────────────────
// Identique au cache de TacticalGridMap : le backdrop est la chose la plus chère peinte par
// frame ET celle dont le contenu ne bouge presque pas, donc on le cuit hors-écran et on le
// re-blitte — invalidé au redimensionnement ou au changement de salle, jamais par frame.
const backdropCache = { canvas: null as HTMLCanvasElement | null, key: '' };

function paintBackdrop(
  ctx: CanvasRenderingContext2D,
  width: number,
  height: number,
  timestamp: number,
) {
  const key = `${width}x${height}:${roomTheme.value}:${props.roomId ?? 'combat'}`;

  if (backdropCache.key !== key || !backdropCache.canvas) {
    const offscreen = document.createElement('canvas');
    offscreen.width = width;
    offscreen.height = height;
    const offCtx = offscreen.getContext('2d');
    if (!offCtx) {
      drawBackdrop(ctx, width, height, roomTheme.value, timestamp, props.roomId ?? 'combat', { scenery: true });
      return;
    }
    drawBackdrop(offCtx, width, height, roomTheme.value, prefersReducedMotion ? 0 : timestamp, props.roomId ?? 'combat', { scenery: true });
    backdropCache.canvas = offscreen;
    backdropCache.key = key;
  }

  ctx.drawImage(backdropCache.canvas, 0, 0);
}

// ── Bandeau d'erreur overlay ────────────────────────────────────────────────
const errorToast = ref<string | null>(null);
let errorTimer: ReturnType<typeof globalThis.setTimeout> | null = null;
watch(
  () => store.error,
  (msg) => {
    if (errorTimer) { globalThis.clearTimeout(errorTimer); errorTimer = null; }
    errorToast.value = msg || null;
    if (msg) {
      errorTimer = globalThis.setTimeout(() => { errorToast.value = null; }, 3500);
    }
  },
);
onBeforeUnmount(() => {
  if (errorTimer) globalThis.clearTimeout(errorTimer);
});

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

function skillShapeLabel(skill: CombatantSkillRuntimeDto): string {
  switch (tacticalSkillProfile(skill).shape) {
    case 'cross':
      return 'croix';
    case 'diamond':
      return 'losange';
    case 'map':
      return 'carte';
    case 'single':
    default:
      return 'case';
  }
}

function skillMeta(skill: CombatantSkillRuntimeDto): string {
  const profile = tacticalSkillProfile(skill);
  const power = skill.basePower > 0 ? ` · ${skill.basePower}` : '';

  return `P${profile.range} · ${skillShapeLabel(skill)}${power}`;
}

function isFloorCell(x: number, y: number): boolean {
  const field = battlefield.value;
  if (!field || x < 0 || y < 0 || x >= field.width || y >= field.height) return false;

  return field.floor[(y * field.width) + x] ?? false;
}

function isWalkableCell(x: number, y: number): boolean {
  const field = battlefield.value;
  if (!field || x < 0 || y < 0 || x >= field.width || y >= field.height) return false;

  return field.walkable[(y * field.width) + x] ?? false;
}

function canSeeCell(from: BattleCell, to: BattleCell): boolean {
  const field = battlefield.value;
  if (!field) return false;

  return hasLos(
    {
      gridWidth: field.width,
      gridHeight: field.height,
      elevation: field.elevation,
      walkable: field.walkable,
      floor: field.floor,
    },
    from,
    to,
  );
}

function shapeCells(shape: TacticalShape, x: number, y: number): BattleCell[] {
  const field = battlefield.value;
  if (!field) return [];

  if (shape === 'map') {
    const all: BattleCell[] = [];
    for (let yy = 0; yy < field.height; yy += 1) {
      for (let xx = 0; xx < field.width; xx += 1) {
        if (isFloorCell(xx, yy)) all.push({ x: xx, y: yy });
      }
    }

    return all;
  }

  if (shape === 'single') return isFloorCell(x, y) ? [{ x, y }] : [];

  const cells: BattleCell[] = [];
  const radius = shape === 'diamond' ? 2 : 1;

  for (let dy = -radius; dy <= radius; dy += 1) {
    for (let dx = -radius; dx <= radius; dx += 1) {
      const distance = Math.abs(dx) + Math.abs(dy);
      if (distance > radius) continue;
      if (shape === 'cross' && distance > 1) continue;

      const cx = x + dx;
      const cy = y + dy;
      if (isFloorCell(cx, cy)) cells.push({ x: cx, y: cy });
    }
  }

  return cells;
}

const reachPreview = computed(() => {
  const field = battlefield.value;
  const active = store.activeCombatant;

  if (!field || !active || !store.isPlayerTurn || active.hasMoved || store.selectedSkillKey) {
    return { cells: new Set<string>(), previous: new Map<string, BattleCell>() };
  }

  const occupied = new Set(occupiedKeys.value);
  occupied.delete(battleCellKey(active.x, active.y));

  return reachableCellsWithPathsFrom(
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
 * Aperçu des cases atteignables. Recalculé côté client pour que le joueur voie où il peut
 * aller <b>avant</b> de cliquer — le serveur reste seul juge, et son refus prime.
 */
const reachableCells = computed<Set<string>>(() => {
  return reachPreview.value.cells;
});

/**
 * Les cases que la compétence armée peut couvrir, avec la même lecture que la référence :
 * portée Manhattan, LOS pour les gestes à distance, et case `blocked` quand une crête coupe.
 */
const rangePreview = computed<Map<string, boolean>>(() => {
  const field = battlefield.value;
  const active = store.activeCombatant;
  const skill = store.selectedSkill;

  if (!field || !active || !skill || !store.isPlayerTurn || active.hasActed) return new Map();

  const profile = tacticalSkillProfile(skill);
  const cells = new Map<string, boolean>();

  for (let y = 0; y < field.height; y += 1) {
    for (let x = 0; x < field.width; x += 1) {
      if (!isWalkableCell(x, y)) continue;

      const inRange = profile.shape === 'map'
        || manhattan(active, { x, y }) <= profile.range;
      if (!inRange) continue;

      cells.set(
        battleCellKey(x, y),
        profile.requiresLineOfSight ? canSeeCell(active, { x, y }) : true,
      );
    }
  }

  return cells;
});

const targetableCells = computed<Set<string>>(() =>
  new Set([...rangePreview.value.entries()]
    .filter(([, visible]) => visible)
    .map(([key]) => key)),
);

const blockedCells = computed<Set<string>>(() =>
  new Set([...rangePreview.value.entries()]
    .filter(([, visible]) => !visible)
    .map(([key]) => key)),
);

const pathCells = computed<Set<string>>(() => {
  const active = store.activeCombatant;
  const hovered = hoveredCell.value;
  if (!active || !hovered || store.selectedSkillKey) return new Set();

  const targetKey = battleCellKey(hovered.x, hovered.y);
  if (!reachPreview.value.cells.has(targetKey)) return new Set();

  const path = new Set<string>();
  let current: BattleCell | undefined = hovered;

  while (current && !(current.x === active.x && current.y === active.y)) {
    const key = battleCellKey(current.x, current.y);
    path.add(key);
    current = reachPreview.value.previous.get(key);
  }

  return path;
});

const aoeCells = computed<Set<string>>(() => {
  const active = store.activeCombatant;
  const skill = store.selectedSkill;
  const hovered = hoveredCell.value;
  if (!active || !skill || !hovered) return new Set();

  if (rangePreview.value.get(battleCellKey(hovered.x, hovered.y)) !== true) return new Set();

  return new Set(shapeCells(tacticalSkillProfile(skill).shape, hovered.x, hovered.y)
    .map((cell) => battleCellKey(cell.x, cell.y)));
});

const heightCells = computed<Set<string>>(() => {
  const active = store.activeCombatant;
  const skill = store.selectedSkill;
  const hovered = hoveredCell.value;
  if (!active || !skill || !hovered) return new Set();

  const profile = tacticalSkillProfile(skill);
  const targetKey = battleCellKey(hovered.x, hovered.y);
  if (!profile.requiresLineOfSight || rangePreview.value.get(targetKey) !== true) return new Set();
  if (elevationAt(active.x, active.y) <= elevationAt(hovered.x, hovered.y)) return new Set();

  return new Set([battleCellKey(active.x, active.y)]);
});

const occupiedHighlightCells = computed<Set<string>>(() => new Set(occupiedKeys.value));

const threatCells = computed<Set<string>>(() => {
  const field = battlefield.value;
  if (!field) return new Set();

  const cells = new Set<string>();
  const occupied = occupiedKeys.value;
  const activeId = store.combat?.activeCombatantId;

  // Affiche la menace uniquement pour l'ennemi actif OU l'ennemi survolé.
  const hoveredEnemies = new Set<string>();
  if (hoveredCell.value) {
    const hk = battleCellKey(hoveredCell.value.x, hoveredCell.value.y);
    for (const enemy of store.combat?.enemies ?? []) {
      if (enemy.combatant.status === 'Defeated') continue;
      if (battleCellKey(enemy.x, enemy.y) === hk) hoveredEnemies.add(enemy.combatant.id);
    }
  }

  for (const enemy of store.combat?.enemies ?? []) {
    if (enemy.combatant.status === 'Defeated') continue;
    if (enemy.combatant.id !== activeId && !hoveredEnemies.has(enemy.combatant.id)) continue;

    const reach = reachableCellsFrom(
      {
        gridWidth: field.width,
        gridHeight: field.height,
        elevation: field.elevation,
        walkable: field.walkable,
      },
      enemy,
      enemy.movementBudget,
      occupied,
    );

    const origins = new Set([battleCellKey(enemy.x, enemy.y), ...reach]);

    for (const key of origins) {
      const [xStr, yStr] = key.split(',');
      const rx = parseInt(xStr, 10);
      const ry = parseInt(yStr, 10);

      const maxRange = Math.max(
        1,
        ...enemy.combatant.skills.map((skill) => tacticalSkillProfile(skill).range),
      );
      for (let dy = -maxRange; dy <= maxRange; dy += 1) {
        for (let dx = -maxRange; dx <= maxRange; dx += 1) {
          const tx = rx + dx;
          const ty = ry + dy;
          if (Math.abs(dx) + Math.abs(dy) <= maxRange && isWalkableCell(tx, ty)) {
            cells.add(battleCellKey(tx, ty));
          }
        }
      }
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
    floor: field.floor,
    theme: roomTheme.value,
    ambientTint: 'neutral',
    reachableCells: reachableCells.value,
    targetableCells: targetableCells.value,
    blockedCells: blockedCells.value,
    pathCells: pathCells.value,
    aoeCells: aoeCells.value,
    threatCells: threatCells.value,
    heightCells: heightCells.value,
    occupiedCells: occupiedHighlightCells.value,
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
        // À défaut de figure peinte, la silhouette que le Palais emploie déjà sur sa carte
        // d'exploration pour cette même créature.
        sprite: combatantSprite(
          unit.combatant.sourceKey, unit.combatant.displayName, unit.combatant.id,
        ) ?? getSprite({
          kind: 'prop',
          theme: roomTheme.value,
          prop: unit.combatant.side === 'Enemy'
            ? fallbackPropFor(unit.combatant.archetype, false)
            : 'npc',
        }),
        // La position interpolée pilote l'affichage à l'écran, mais le sortKey doit rester
        // sur des coordonnées entières (la case de destination) pour que la figure ne passe
        // jamais derrière les dalles pendant la marche.
        sortKey: ((Math.round(at.x) + Math.round(at.y)) * 4) + elevation + 0.5,
      };
    })
    .sort((a, b) => a.sortKey - b.sortKey);
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

function highlightAlpha(
  entry: ReturnType<typeof buildBattlePlan>[number],
  timestamp: number,
): number {
  if (entry.spriteKey.kind !== 'highlight') return 1;

  switch (entry.spriteKey.variant) {
    case 'move':
      return prefersReducedMotion
        ? 0.7
        : 0.5 + (0.35 * Math.sin((timestamp * 0.0022) + ((entry.x + entry.y) * 0.5)));
    case 'threat':
      return 0.9;
    case 'attack':
    case 'blocked':
      return 0.85;
    case 'occupied':
      return 0.6;
    case 'cursor':
      return 1;
    case 'path':
    case 'aoe':
    case 'height':
      return 0.95;
    default:
      return 1;
  }
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

  paintBackdrop(ctx, canvas.width, canvas.height, timestamp);

  const { destW, destH, propH } = spriteDest.value;

  const terrainPlan = drawPlan.value.filter((entry) => entry.spriteKey.kind !== 'highlight');
  const highlightPlan = drawPlan.value.filter((entry) => entry.spriteKey.kind === 'highlight');

  const enemyCount = store.combat?.enemies.filter((e) => e.combatant.status !== 'Defeated').length ?? 0;
  const tierKey = enemyCount <= 2 ? 'calm' : enemyCount <= 3 ? 'tense' : enemyCount <= 4 ? 'grim' : 'fatal';
  const tier = RISK_TIERS[tierKey] ?? RISK_TIERS.calm;

  // Vignette : peinte avant le terrain et les combattants pour qu'elle
  // assombrisse le décor sans désaturer les unités ni les barres de vie.
  drawCombatGrade(ctx, canvas.width, canvas.height, tier.grade, tier.accent);

  const deploying = deployStartedAt.value !== null
    && (timestamp - deployStartedAt.value) < DEPLOY_DURATION_MS;

  const combatantEntries = buildCombatantPlan(timestamp);

  // ── Fusion terrain + combattants, triés front→back ──────────────────────
  // Les deux passent dans LE MÊME tableau trié par sortKey : un obstacle haut
  // placé devant une unité la recouvre ; un sol plat derrière l'unité reste
  // derrière. C'est le contrat du painter's algorithm sur une grille iso.
  type TerrainItem = { kind: 'terrain'; entry: typeof terrainPlan[number]; sprite: HTMLCanvasElement };
  type CombatantItem = {
    kind: 'combatant';
    entry: (typeof combatantEntries)[number];
    isActive: boolean;
    side: 'enemy' | 'ally';
    hpRatio: number;
    downed: boolean;
  };
  type SceneItem = TerrainItem | CombatantItem;

  const items: SceneItem[] = terrainPlan.map((entry) => ({
    kind: 'terrain' as const,
    entry,
    sprite: getSprite(entry.spriteKey),
  }));

  for (const entry of combatantEntries) {
    const isActive = entry.unit.combatant.id === store.combat?.activeCombatantId;
    const hpRatio = Math.max(0, Math.min(1, entry.unit.combatant.currentVitality / Math.max(1, entry.unit.combatant.maxVitality)));

    items.push({
      kind: 'combatant',
      entry,
      isActive,
      side: entry.unit.combatant.side === 'Enemy' ? 'enemy' : 'ally',
      hpRatio,
      downed: entry.unit.combatant.status === 'Defeated',
    });
  }

  items.sort((a, b) => a.entry.sortKey - b.entry.sortKey);

  // ── Passe unique de peinture (terrain + unités) ─────────────────────────
  for (const item of items) {
    if (item.kind === 'terrain') {
      const { entry, sprite } = item;
      const dx = entry.screenX - (destW / 2);

      if (usesPropRect(entry.spriteKey)) {
        const lift = entry.spriteKey.kind === 'prop' ? elevationLiftPx(entry.elevation) : 0;
        const propDy = entry.screenY - (propH * PROP_GROUND_ANCHOR_RATIO) - lift;

        ctx.drawImage(sprite, dx, propDy, destW, propH);

        if (entry.spriteKey.kind === 'prop' && entry.spriteKey.prop === 'campfire') {
          drawFireFx(ctx, dx, propDy, destW, propH, timestamp);
        }
        if (entry.spriteKey.kind === 'prop' && entry.spriteKey.prop === 'star') {
          drawStarFx(ctx, dx, propDy, destW, propH, timestamp);
        }
        continue;
      }

      ctx.drawImage(sprite, dx, entry.screenY - (destH * GROUND_ANCHOR_RATIO), destW, destH);
    } else {
      // ── Combattant ──
      const { entry, isActive, side, hpRatio, downed } = item;
      const lift = elevationLiftPx(entry.elevation);
      const groundY = entry.screenY - lift;

      if (deploying && deployStartedAt.value !== null) {
        const deployProgress = (timestamp - deployStartedAt.value) / DEPLOY_DURATION_MS;
        const color = entry.unit.combatant.side === 'Enemy' ? ENEMY_RIM : ALLY_RIM;

        drawDeployFx(
          ctx,
          entry.screenX - (destW / 2),
          groundY - (propH * PROP_GROUND_ANCHOR_RATIO),
          destW,
          propH,
          deployProgress,
          color,
          entry.elevation,
        );
      }

      if (downed) ctx.globalAlpha = 0.32;

      const ringDy = groundY + spriteDest.value.destH * FIGURE_BASE_OFFSET
        - (propH * GROUND_ANCHOR_RATIO);

      drawUnitRing(
        ctx,
        entry.screenX - (destW / 2),
        ringDy,
        destW,
        propH,
        { side, hp: hpRatio, active: isActive, downed, elevation: 0 },
        prefersReducedMotion ? 0 : timestamp,
      );

      const bob = !prefersReducedMotion && !deploying
        ? Math.sin((timestamp * 0.0016) + entry.unit.x) * 1.6
        : 0;
      const pace = !prefersReducedMotion && !deploying
        ? Math.sin((timestamp * 0.0007) + (entry.unit.x * 1.7) + (entry.unit.y * 0.9)) * (destW * 0.06)
        : 0;

      ctx.drawImage(
        entry.sprite,
        entry.screenX - (destW / 2) + pace,
        groundY - (propH * PROP_GROUND_ANCHOR_RATIO) + bob,
        destW,
        propH,
      );

      if (isActive && store.isPlayerTurn) {
        drawActionPips(
          ctx,
          entry.screenX,
          groundY - (propH * 0.30),
          entry.unit.hasMoved,
          entry.unit.hasActed,
          prefersReducedMotion ? 0 : timestamp,
        );
      }

      paintCombatantChrome(ctx, entry, groundY - (propH * 0.35), destW);

      ctx.globalAlpha = 1;
    }
  }

  // ── Highlights (semi-transparents, toujours par-dessus le terrain) ──────
  for (const entry of highlightPlan) {
    const sprite = getSprite(entry.spriteKey);
    const dx = entry.screenX - (destW / 2);

    ctx.save();
    ctx.globalAlpha = highlightAlpha(entry, timestamp);
    ctx.drawImage(sprite, dx, entry.screenY - (destH * GROUND_ANCHOR_RATIO), destW, destH);
    ctx.restore();
  }

  // ── Ambiance / FX / chiffres ─────────────────────────────────────────────
  if (!prefersReducedMotion) {
    ctx.save();
    drawAmbient(ctx, canvas.width, canvas.height, roomTheme.value, timestamp);
    ctx.restore();
  }

  paintImpacts(ctx, timestamp);
  paintSorts(ctx);
  paintFloatingNumbers(ctx, timestamp);
  paintCombatCartouche(ctx, tier);
}

function paintImpacts(ctx: CanvasRenderingContext2D, timestamp: number) {
  store.playback.pruneImpacts(timestamp);

  const { destW, propH } = spriteDest.value;
  const IMPACT_MS = 800;

  for (const impact of store.playback.impacts) {
    const progress = (timestamp - impact.bornAt) / IMPACT_MS;
    if (progress > 1) continue;

    const { screenX, screenY } = projectToScreen(impact.x, impact.y, projectionParams.value);
    const lift = elevationLiftPx(elevationAt(impact.x, impact.y));

    drawImpactFx(
      ctx,
      screenX - (destW / 2),
      screenY - lift - (propH * PROP_GROUND_ANCHOR_RATIO),
      destW,
      propH,
      progress,
      impact.color,
      elevationAt(impact.x, impact.y),
    );
  }
}

function paintSorts(ctx: CanvasRenderingContext2D) {
  const field = battlefield.value;
  if (!field) return;

  const pending = store.playback.consumeSorts();
  for (const sort of pending) {
    const sortId = sortIdForSkillKey(sort.skillKey);
    if (!sortId) continue;

    sortEffects.launchSort(
      sortId,
      sort.x,
      sort.y,
      {
        width: field.width,
        height: field.height,
        elevation: field.elevation,
        floor: field.floor,
      },
      projectionParams.value,
      sort.casterX,
      sort.casterY,
    );
  }

  sortEffects.renderSorts(ctx);
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

function paintCombatCartouche(
  ctx: CanvasRenderingContext2D,
  tier: (typeof RISK_TIERS)[string],
) {
  ctx.save();
  ctx.font = "500 19px ui-monospace, 'JetBrains Mono', monospace";
  ctx.fillStyle = 'rgba(233, 230, 245, 0.5)';
  ctx.fillText(themeLabel(roomTheme.value).toUpperCase(), 28, 42);

  ctx.font = "400 16px ui-monospace, 'JetBrains Mono', monospace";
  ctx.fillStyle = tier.accent;
  ctx.fillText(`PALIER ${tier.label.toUpperCase()} · ${tier.enemies} ENNEMIS`, 28, 68);
  ctx.restore();
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

// Fin de tour automatique quand le joueur a dépensé ses deux actions (déplacement + compétence).
let autoEndedThisTurn = false;
let lastActiveId: string | null = null;
watch(
  () => {
    const active = store.activeCombatant;
    if (!active || !store.isPlayerTurn) return null;
    return `${active.hasMoved}|${active.hasActed}|${active.combatant.id}`;
  },
  (val) => {
    if (!val) { autoEndedThisTurn = false; lastActiveId = null; return; }
    if (store.isLoading) return;

    const active = store.activeCombatant;
    if (!active || !store.isPlayerTurn) return;

    // Nouveau tour → reset
    if (active.combatant.id !== lastActiveId) {
      autoEndedThisTurn = false;
      lastActiveId = active.combatant.id;
    }

    if (autoEndedThisTurn) return;

    if (active.hasMoved && active.hasActed) {
      autoEndedThisTurn = true;
      void store.endTurn(props.runId);
    }
  },
  { immediate: true },
);

watch(
  () => store.combat?.id,
  (newId) => {
    if (newId) {
      deployStartedAt.value = performance.now();
      sortEffects.reset();
    }
  },
  { immediate: true },
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
  <section v-if="store.combat" class="tbattle" :class="{ 'tbattle--transitioning': playback.isTransitioning }">
    <!-- ── Initiative rail (left) ── -->
    <aside class="tbattle__initiative-rail">
      <span class="tbattle__round">Round {{ store.combat.roundNumber }}</span>
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
    </aside>

    <!-- ── Central board ── -->
    <div class="tbattle__board">
      <Transition name="tbattle-banner">
        <p v-if="store.playback.actionBanner" class="tbattle__banner">
          {{ store.playback.actionBanner }}
        </p>
      </Transition>

      <Transition name="tbattle-toast">
        <p v-if="errorToast" class="tbattle__toast" role="alert">
          {{ errorToast }}
        </p>
      </Transition>

      <canvas
        ref="canvasEl"
        class="tbattle__canvas"
        @click="onCanvasClick"
        @mousemove="onCanvasMove"
        @mouseleave="hoveredCell = null"
      />
    </div>

    <!-- ── Action log (right) ── -->
    <aside class="tbattle__log-rail">
      <div class="tbattle__log">
        <p
          v-for="(entry, index) in store.logEntries.slice(-10)"
          :key="index"
          class="tbattle__log-entry"
        >
          {{ entry.message }}
        </p>
      </div>
    </aside>

    <!-- ── Bottom bar: portraits + skills ── -->
    <footer class="tbattle__bottom">
      <!-- Portrait cards : toujours visibles -->
      <div class="tbattle__portraits">
        <div
          v-for="unit in store.initiativeQueue"
          :key="unit.combatant.id"
          class="tbattle__portrait"
          :class="{
            'tbattle__portrait--active': unit.combatant.id === store.combat.activeCombatantId,
            'tbattle__portrait--enemy': unit.combatant.side === 'Enemy',
            'tbattle__portrait--downed': unit.combatant.status === 'Defeated',
          }"
        >
          <div class="tbattle__portrait-frame">
            <span class="tbattle__portrait-initial">
              {{ unit.combatant.displayName.charAt(0) }}
            </span>
          </div>
          <span class="tbattle__portrait-name">{{ unit.combatant.displayName }}</span>
          <div class="tbattle__portrait-hp">
            <div class="tbattle__portrait-hp-bar">
              <div
                class="tbattle__portrait-hp-fill"
                :class="{ 'tbattle__portrait-hp-fill--low': unit.combatant.currentVitality / Math.max(1, unit.combatant.maxVitality) < 0.3 }"
                :style="{ width: `${Math.max(0, Math.min(100, (unit.combatant.currentVitality / Math.max(1, unit.combatant.maxVitality)) * 100))}%` }"
              />
            </div>
            <span class="tbattle__portrait-hp-text">
              {{ unit.combatant.currentVitality }}/{{ unit.combatant.maxVitality }}
            </span>
          </div>
        </div>
      </div>

      <!-- Skills + controls : toujours visibles, grisés hors tour joueur -->
      <div class="tbattle__controls">
        <div class="tbattle__active-info">
          <strong class="tbattle__active-name">
            {{ store.activeCombatant?.combatant?.displayName ?? '—' }}
          </strong>
          <span class="tbattle__active-stat">
            PP {{ store.activeCombatant?.combatant?.mana ?? '—' }}
          </span>
          <span class="tbattle__active-stat" :class="{ 'tbattle__active-stat--spent': store.activeCombatant?.hasMoved }">
            {{
              store.activeCombatant
                ? (store.activeCombatant.hasMoved ? 'Déplacé' : `Déplacement ${store.activeCombatant.movementBudget}`)
                : '—'
            }}
          </span>
          <span class="tbattle__active-stat" :class="{ 'tbattle__active-stat--spent': store.activeCombatant?.hasActed }">
            {{
              store.activeCombatant
                ? (store.activeCombatant.hasActed ? 'A agi' : 'Action')
                : '—'
            }}
          </span>
        </div>

        <div class="tbattle__skills">
          <template v-if="store.isPlayerTurn">
            <button
              v-for="skill in store.activeSkills"
              :key="skill.key"
              type="button"
              class="tbattle__skill"
              :class="{ 'tbattle__skill--armed': skill.key === store.selectedSkillKey }"
              :disabled="
                (store.activeCombatant?.hasActed ?? true)
                  || store.isLoading
                  || store.combat?.usedOnceSkillKeys.includes(skill.key)
              "
              :title="`${skill.displayName} — ${skill.category === 'Magic' ? 'magique' : 'physique'}, ${skillMeta(skill)}, PP ${skill.manaCost}`"
              @click="store.selectSkill(skill.key)"
            >
              <span class="tbattle__skill-name">{{ skill.displayName }}</span>
              <span class="tbattle__skill-meta">{{ skillMeta(skill) }}</span>
            </button>
          </template>
          <span v-else class="tbattle__skills-placeholder" />
        </div>

        <button
          type="button"
          class="tbattle__end-turn"
          :disabled="store.isLoading || !store.isPlayerTurn"
          @click="store.endTurn(props.runId)"
        >
          Finir le tour
        </button>
      </div>

      <p v-if="store.isPlayerTurn && store.selectedSkillKey" class="tbattle__hint">
        Clique une case dans la zone rouge pour lancer.
      </p>
      <p v-else-if="store.isPlayerTurn" class="tbattle__hint">
        Clique une case en surbrillance pour t’y rendre.
      </p>
      <p v-else class="tbattle__waiting">L'adversaire agit…</p>
    </footer>
  </section>
</template>

<style scoped>
.tbattle {

.tbattle--transitioning {
  opacity: 0.5;
  transition: opacity 300ms ease-in-out;
}
  display: grid;
  grid-template-columns: 170px 1fr 180px;
  grid-template-rows: 1fr auto;
  grid-template-areas:
    "initiative board log"
    "bottom     bottom bottom";
  height: 100%;
  min-height: 0;
  gap: 0;
}

/* ── Initiative rail (left) ──────────────────────────────────────────────── */
.tbattle__initiative-rail {
  grid-area: initiative;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.6rem 0.5rem;
  border-right: 1px solid rgb(255 255 255 / 10%);
  background: rgb(9 11 22 / 55%);
  overflow-y: auto;
}

.tbattle__round {
  font-family: var(--font-caps, monospace);
  font-variant: small-caps;
  letter-spacing: 0.08em;
  font-size: 0.82rem;
  color: #e6c273;
  white-space: nowrap;
  padding-bottom: 0.35rem;
  border-bottom: 1px solid rgb(255 255 255 / 8%);
}

.tbattle__initiative {
  display: flex;
  flex-direction: column;
  gap: 3px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.tbattle__initiative-entry {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.2rem 0.35rem;
  border: 1px solid rgb(255 255 255 / 15%);
  border-radius: 3px;
  font-size: 0.72rem;
  white-space: nowrap;
  transition: background 120ms ease;
}

.tbattle__initiative-entry--enemy { border-color: #8e2b32; }

.tbattle__initiative-entry--active {
  background: rgb(230 194 115 / 18%);
  border-color: #e6c273;
}

.tbattle__initiative-rank { opacity: 0.45; width: 1.1em; font-size: 0.65rem; }
.tbattle__initiative-name { flex: 1; overflow: hidden; text-overflow: ellipsis; }
.tbattle__initiative-hp { opacity: 0.5; font-variant-numeric: tabular-nums; font-size: 0.65rem; }

/* ── Central board ──────────────────────────────────────────────────────── */
.tbattle__board {
  grid-area: board;
  position: relative;
  min-height: 0;
  overflow: hidden;
}

.tbattle__canvas {
  display: block;
  width: 100%;
  height: 100%;
}

.tbattle__banner {
  position: absolute;
  top: 5%;
  left: 50%;
  transform: translateX(-50%);
  margin: 0;
  padding: 0.35rem 1rem;
  border: 1px solid rgb(230 194 115 / 40%);
  border-radius: 999px;
  background: rgb(9 11 22 / 85%);
  color: #f4f1ff;
  font-variant: small-caps;
  letter-spacing: 0.05em;
  font-size: 0.85rem;
  white-space: nowrap;
  pointer-events: none;
  z-index: 2;
}

.tbattle-banner-enter-active,
.tbattle-banner-leave-active { transition: opacity 160ms ease-out; }
.tbattle-banner-enter-from,
.tbattle-banner-leave-to { opacity: 0; }

@media (prefers-reduced-motion: reduce) {
  .tbattle-banner-enter-active,
  .tbattle-banner-leave-active { transition: none; }
}

/* ── Error toast (flottant en bas du canvas) ────────────────────────────── */
.tbattle__toast {
  position: absolute;
  bottom: 4%;
  left: 50%;
  transform: translateX(-50%);
  margin: 0;
  padding: 0.35rem 1rem;
  border: 1px solid rgb(224 96 94 / 45%);
  border-radius: 999px;
  background: rgb(14 5 7 / 90%);
  color: #f0a0a0;
  font-size: 0.82rem;
  white-space: nowrap;
  pointer-events: none;
  z-index: 3;
}

.tbattle-toast-enter-active,
.tbattle-toast-leave-active { transition: opacity 180ms ease-out; }
.tbattle-toast-enter-from,
.tbattle-toast-leave-to { opacity: 0; }

@media (prefers-reduced-motion: reduce) {
  .tbattle-toast-enter-active,
  .tbattle-toast-leave-active { transition: none; }
}

/* ── Log rail (right) ───────────────────────────────────────────────────── */
.tbattle__log-rail {
  grid-area: log;
  display: flex;
  flex-direction: column;
  padding: 0.6rem 0.5rem;
  border-left: 1px solid rgb(255 255 255 / 10%);
  background: rgb(9 11 22 / 55%);
  overflow-y: auto;
}

.tbattle__log {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  margin-top: auto;
}

.tbattle__log-entry {
  margin: 0;
  font-size: 0.68rem;
  opacity: 0.55;
  line-height: 1.3;
}

/* ── Bottom bar: portraits + skills ─────────────────────────────────────── */
.tbattle__bottom {
  grid-area: bottom;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.45rem 0.75rem;
  border-top: 1px solid rgb(255 255 255 / 10%);
  background: rgb(9 11 22 / 70%);
  backdrop-filter: blur(6px);
}

.tbattle__portraits {
  display: flex;
  gap: 0.5rem;
  overflow-x: auto;
}

.tbattle__portrait {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  min-width: 72px;
  padding: 0.3rem 0.4rem;
  border: 1px solid rgb(255 255 255 / 12%);
  border-radius: 4px;
  background: rgb(255 255 255 / 4%);
  transition: border-color 120ms ease, background 120ms ease;
}

.tbattle__portrait--active {
  border-color: #e6c273;
  background: rgb(230 194 115 / 10%);
}

.tbattle__portrait--enemy { border-color: rgb(142 43 50 / 50%); }
.tbattle__portrait--downed { opacity: 0.35; }

.tbattle__portrait-frame {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1.5px solid rgb(255 255 255 / 20%);
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgb(255 255 255 / 6%);
}

.tbattle__portrait--enemy .tbattle__portrait-frame {
  border-color: rgb(224 96 94 / 40%);
  background: rgb(224 96 94 / 10%);
}

.tbattle__portrait-initial {
  font-size: 0.85rem;
  font-weight: 600;
  opacity: 0.8;
}

.tbattle__portrait-name {
  font-size: 0.62rem;
  opacity: 0.7;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 72px;
}

.tbattle__portrait-hp {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1px;
  width: 100%;
}

.tbattle__portrait-hp-bar {
  width: 100%;
  height: 3px;
  background: rgb(255 255 255 / 10%);
  border-radius: 2px;
  overflow: hidden;
}

.tbattle__portrait-hp-fill {
  height: 100%;
  background: #86dcb4;
  border-radius: 2px;
  transition: width 200ms ease;
}

.tbattle__portrait-hp-fill--low {
  background: #e0605e;
}

.tbattle__portrait-hp-text {
  font-size: 0.58rem;
  opacity: 0.5;
  font-variant-numeric: tabular-nums;
}

.tbattle__controls {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-wrap: wrap;
}

.tbattle__active-info {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
}

.tbattle__active-name { color: #e6c273; font-size: 0.85rem; }

.tbattle__active-stat {
  font-size: 0.75rem;
  opacity: 0.7;
  font-variant-numeric: tabular-nums;
}

.tbattle__active-stat--spent { opacity: 0.35; text-decoration: line-through; }

.tbattle__skills { display: flex; gap: 0.50rem; flex-wrap: wrap; align-items: center; min-height: calc(0.5rem + 0.78rem + 0.62rem + 0.1rem); }

.tbattle__skill,
.tbattle__end-turn {
  padding: 0.25rem 0.6rem;
  border: 1px solid rgb(255 255 255 / 22%);
  border-radius: 4px;
  background: transparent;
  color: inherit;
  cursor: pointer;
  font-size: 0.78rem;
}

.tbattle__skill {
  display: inline-flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.1rem;
}

.tbattle__skill--armed { background: rgb(224 96 94 / 22%); border-color: #e0605e; }
.tbattle__skill:disabled { opacity: 0.35; cursor: not-allowed; }
.tbattle__skill-name { font-weight: 600; }
.tbattle__skill-meta { font-size: 0.62rem; opacity: 0.55; font-variant-numeric: tabular-nums; }

.tbattle__end-turn {
  font-weight: 600;
  color: #e6c273;
  border-color: rgb(230 194 115 / 35%);
}

.tbattle__end-turn:hover:not(:disabled) {
  background: rgb(230 194 115 / 12%);
}

.tbattle__hint { margin: 0; font-size: 0.72rem; opacity: 0.45; }
.tbattle__waiting { opacity: 0.6; font-style: italic; margin: 0; font-size: 0.8rem; }
.tbattle__error { color: #e0605e; margin: 0; font-size: 0.8rem; }
</style>
