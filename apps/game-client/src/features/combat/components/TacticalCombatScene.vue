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
import StatusEffectToken from '../../../shared/components/StatusEffectToken.vue';
import SkillDetailModal from '../../../shared/components/SkillDetailModal.vue';
import { skillsApi } from '../../party/api/skillsApi';
import type { SkillDefinitionView } from '../../party/types/skillTypes';
import { combatantSprite, fallbackPropFor } from '../composables/useCombatantSprites';
import { FLOAT_MS, FLOAT_RISE_PX, useCombatPlayback } from '../composables/useCombatPlayback';
import { sortIdForSkillKey, useSortEffects } from '../composables/useSortEffects';
import { ticksToTurns } from '../constants/combatTime';
import {
  tacticalSkillProfile,
  type TacticalShape,
} from '../composables/useTacticalSkillProfile';
import { useTacticalCombatStore } from '../stores/useTacticalCombatStore';

import type {
  CombatantRuntimeDto,
  CombatantSkillRuntimeDto,
  CombatantStatusEffectDto,
  CombatUsableItemDto,
  EmotionalType,
  TacticalCombatantRuntimeDto,
} from '../types/combatContracts';
import type { NodeDto } from '../../runs/types/runTypes';

const props = defineProps<{
  runId: string;
  theme?: string;
  catalogRoomKey?: string;
  roomId?: string;
  /** The room's own nodes — used only to keep unresolved encounters/scenery visible as decor
   * on the battlefield (see buildBattlePlan's nodesByCell), never for click/interaction. */
  nodes?: NodeDto[];
}>();

const emit = defineEmits<{
  (event: 'combat-completed'): void;
  (event: 'combat-failed'): void;
  (event: 'combat-escaped'): void;
}>();

const store = useTacticalCombatStore();
const { getSprite } = useTerrainSprites();
const sortEffects = useSortEffects();
const playback = useCombatPlayback();

const canvasEl = ref<HTMLCanvasElement | null>(null);
const battleEl = ref<HTMLElement | null>(null);
const panelEl = ref<HTMLElement | null>(null);
const canvasSize = ref({ width: 0, height: 0 });
const hoveredCell = ref<{ x: number; y: number } | null>(null);
const panelPosition = ref({ x: 190, y: 24 });
const panelWasDragged = ref(false);

// Journal : clic sur une entrée liée à un sort ouvre son descriptif complet.
const skillCatalog = ref<Map<string, SkillDefinitionView>>(new Map());
const inspectedSkill = ref<SkillDefinitionView | null>(null);

function openSkillDetail(skillKey: string | null) {
  if (!skillKey) return;
  inspectedSkill.value = skillCatalog.value.get(skillKey) ?? null;
}

// Hover description for a skill button — teleported to <body> (like the loot popover) because
// the action bar lives inside the draggable panel, which clips overflow on both axes.
const hoveredSkillKey = ref<string | null>(null);
const skillTooltipStyle = ref<{ left: string; bottom: string }>({ left: '0px', bottom: '0px' });
const hoveredSkill = computed<CombatantSkillRuntimeDto | null>(() =>
  store.activeSkills.find((s) => s.key === hoveredSkillKey.value) ?? null,
);
const hoveredSkillDescription = computed(() =>
  hoveredSkillKey.value ? skillCatalog.value.get(hoveredSkillKey.value)?.description ?? null : null,
);

const SKILL_TOOLTIP_WIDTH = 260;
const SKILL_TOOLTIP_GAP = 10;
const SKILL_TOOLTIP_MARGIN = 8;

function showSkillTooltip(skillKey: string, target: EventTarget | null) {
  const el = target instanceof HTMLElement ? target : null;
  if (!el) return;

  const rect = el.getBoundingClientRect();
  const left = Math.max(
    SKILL_TOOLTIP_MARGIN,
    Math.min(
      rect.left + (rect.width / 2) - (SKILL_TOOLTIP_WIDTH / 2),
      globalThis.innerWidth - SKILL_TOOLTIP_WIDTH - SKILL_TOOLTIP_MARGIN,
    ),
  );
  const bottom = Math.max(SKILL_TOOLTIP_MARGIN, globalThis.innerHeight - rect.top + SKILL_TOOLTIP_GAP);

  skillTooltipStyle.value = { left: `${left}px`, bottom: `${bottom}px` };
  hoveredSkillKey.value = skillKey;
}

function hideSkillTooltip() {
  hoveredSkillKey.value = null;
}

const DEPLOY_DURATION_MS = 1200;
const deployStartedAt = ref<number | null>(null);

let frameHandle = 0;
let observer: ResizeObserver | null = null;
let stopPanelDrag: (() => void) | null = null;

const prefersReducedMotion =
  typeof globalThis.matchMedia === 'function'
  && globalThis.matchMedia('(prefers-reduced-motion: reduce)').matches;

const battlefield = computed(() => store.combat?.battlefield ?? null);
const roomTheme = computed<RoomTheme>(() =>
  resolveRoomVisual(props.catalogRoomKey, (props.theme ?? 'Threshold') as RenderTheme),
);

// Unresolved room nodes, by cell — decor only (see buildBattlePlan's nodesByCell), never
// interactive here, unlike exploration's own nodesByCell (useGridCells).
const battlefieldNodesByCell = computed(() => {
  const map = new Map<string, NodeDto>();
  for (const node of props.nodes ?? []) {
    map.set(battleCellKey(node.lane, node.row), node);
  }
  return map;
});

const projectionParams = computed(() => ({
  canvasWidth: canvasSize.value.width,
  canvasHeight: canvasSize.value.height,
  gridWidth: battlefield.value?.width ?? 1,
  gridHeight: battlefield.value?.height ?? 1,
}));

const panelStyle = computed(() => ({
  left: `${panelPosition.value.x}px`,
  top: `${panelPosition.value.y}px`,
}));

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

function placePanelNearActive(force = false) {
  if (panelWasDragged.value && !force) return;
  const host = battleEl.value;
  const active = store.activeCombatant;
  if (!host || !active || canvasSize.value.width <= 0) return;

  const projected = projectToScreen(active.x, active.y, projectionParams.value);
  const panelWidth = Math.min(720, Math.max(360, host.clientWidth - 380));
  const panelHeight = panelEl.value?.offsetHeight ?? 190;
  const desiredX = 170 + projected.screenX + 32;
  const desiredY = projected.screenY - (panelHeight / 2);

  panelPosition.value = {
    x: clamp(desiredX, 178, Math.max(178, host.clientWidth - panelWidth - 188)),
    y: clamp(desiredY, 12, Math.max(12, host.clientHeight - panelHeight - 12)),
  };
  panelWasDragged.value = false;
}

function beginPanelDrag(event: PointerEvent) {
  if (event.button !== 0 || !battleEl.value || !panelEl.value) return;
  event.preventDefault();

  const hostBounds = battleEl.value.getBoundingClientRect();
  const panelBounds = panelEl.value.getBoundingClientRect();
  const offsetX = event.clientX - panelBounds.left;
  const offsetY = event.clientY - panelBounds.top;

  const move = (next: PointerEvent) => {
    if (!battleEl.value || !panelEl.value) return;
    panelPosition.value = {
      x: clamp(
        next.clientX - hostBounds.left - offsetX,
        0,
        Math.max(0, battleEl.value.clientWidth - panelEl.value.offsetWidth),
      ),
      y: clamp(
        next.clientY - hostBounds.top - offsetY,
        0,
        Math.max(0, battleEl.value.clientHeight - panelEl.value.offsetHeight),
      ),
    };
    panelWasDragged.value = true;
  };
  const end = () => {
    globalThis.removeEventListener('pointermove', move);
    globalThis.removeEventListener('pointerup', end);
    stopPanelDrag = null;
  };

  stopPanelDrag?.();
  stopPanelDrag = end;
  globalThis.addEventListener('pointermove', move);
  globalThis.addEventListener('pointerup', end, { once: true });
}

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

/**
 * Élévation lissée à une position fractionnaire (unité en cours de marche) : interpole
 * bilinéairement entre les 4 cases voisines, comme la position écran elle-même l'est déjà.
 * Sans ça, franchir la frontière entre deux cases de hauteurs différentes fait sauter
 * l'altitude d'un coup à mi-pas alors que le glissement horizontal reste continu — la
 * figure semble s'enfoncer dans le sol. `elevationAt` reste la source pour le sortKey
 * (toujours ancré sur la case de destination entière, voir buildCombatantPlan).
 */
function elevationAtFractional(x: number, y: number): number {
  const x0 = Math.floor(x);
  const y0 = Math.floor(y);
  const fx = x - x0;
  const fy = y - y0;

  const e00 = elevationAt(x0, y0);
  const e10 = elevationAt(x0 + 1, y0);
  const e01 = elevationAt(x0, y0 + 1);
  const e11 = elevationAt(x0 + 1, y0 + 1);

  const top = e00 + ((e10 - e00) * fx);
  const bottom = e01 + ((e11 - e01) * fx);
  return top + ((bottom - top) * fy);
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

function effectiveManaCost(skill: CombatantSkillRuntimeDto): number {
  return skill.effectiveManaCost ?? skill.manaCost;
}

function remainingCooldown(skill: CombatantSkillRuntimeDto): number {
  return store.activeCombatant?.skillCooldowns[skill.key] ?? 0;
}

function missingMana(skill: CombatantSkillRuntimeDto): number {
  return Math.max(0, effectiveManaCost(skill) - (store.activeCombatant?.combatant.mana ?? 0));
}

function vitalitySacrifice(skill: CombatantSkillRuntimeDto): number {
  return missingMana(skill) * 2;
}

function skillCostMeta(skill: CombatantSkillRuntimeDto): string {
  const parts = [`${effectiveManaCost(skill)} Mana`];
  if (skill.chargeCost > 0) parts.push(`${skill.chargeCost} Charge`);
  const cooldown = remainingCooldown(skill);
  if (cooldown > 0) parts.push(`Recharge ${cooldown}`);
  return parts.join(' · ');
}

function skillUnavailable(skill: CombatantSkillRuntimeDto): boolean {
  const actor = store.activeCombatant;
  return (actor?.hasActed ?? true)
    || store.isLoading
    || (store.combat?.usedOnceSkillKeys.includes(skill.key) ?? false)
    || remainingCooldown(skill) > 0
    || (actor?.combatant.charge ?? 0) < skill.chargeCost;
}

function facingLabel(facing: 'North' | 'East' | 'South' | 'West' | undefined): string {
  switch (facing) {
    case 'North': return 'nord';
    case 'East': return 'est';
    case 'South': return 'sud';
    case 'West': return 'ouest';
    default: return '—';
  }
}

function riskTierLabel(tier: string | undefined): string {
  if (tier === 'Dangereux' || tier === 'Perilleux') return 'Sombre';
  return tier ?? 'Calme';
}

const STAT_MODIFIER_LABELS: Record<string, string> = {
  AttackPower: 'Attaque', Defense: 'Défense', Speed: 'Vitesse', Movement: 'Déplacement',
  Focus: 'Focus', Evasion: 'Évasion', MagicAttack: 'Attaque magique', MagicDefense: 'Défense magique',
  MagicDamageBonus: 'Dégâts magiques', MagicDamageReduction: 'Réduction dégâts magiques',
  CriticalChanceBonus: 'Chance de critique', DotDamageBonus: 'Dégâts continus infligés',
  SkillCostReductionPercent: 'Coût des sorts', HealingBonus: 'Soins prodigués',
  PhysicalDamageBonus: 'Dégâts physiques', FireDamageBonus: 'Dégâts de feu',
  FlatManaCostBonus: 'Coût en mana',
};

function statusTitle(status: CombatantStatusEffectDto): string {
  if (status.kind === 'StatModifier' && status.stat && status.stat !== 'None') {
    const label = STAT_MODIFIER_LABELS[status.stat] ?? status.stat;
    const sign = status.magnitude >= 0 ? '+' : '';
    const unit = status.isMagnitudePercentOfBaseStat ? '%' : '';
    return `${status.displayName} · ${label} ${sign}${status.magnitude}${unit}`;
  }
  return `${status.displayName} · magnitude ${status.magnitude}`;
}

/**
 * La vitalité à afficher pour un combattant, telle que la mise en scène en cours l'a déjà
 * révélée — jamais le total final avant que le coup qui l'explique n'ait atterri à l'écran.
 */
function displayedVitality(unit: TacticalCombatantRuntimeDto): number {
  return store.playback.vitalsOf(unit.combatant.id, unit.combatant.currentVitality);
}

/**
 * Les états actifs à afficher pour ce combattant, tels que la mise en scène en cours les a déjà
 * révélés — jamais un stack ou un affaiblissement tout neuf avant que le geste qui l'applique
 * n'ait atterri à l'écran (voir `useCombatPlayback.statusEffectsOf`).
 */
function displayedStatusEffects(unit: TacticalCombatantRuntimeDto): CombatantStatusEffectDto[] {
  return store.playback.statusEffectsOf(unit.combatant.id, unit.combatant.statusEffects ?? []);
}

function itemShape(item: CombatUsableItemDto): TacticalShape {
  return item.tacticalAreaShape.toLowerCase() as TacticalShape;
}

function itemMeta(item: CombatUsableItemDto): string {
  return `P${item.tacticalRange} · ${itemShape(item)} · ×${item.quantity}`;
}

function itemEffectLabel(effectType: string): string {
  if (effectType.includes('Heal')) return 'Soin';
  if (effectType.includes('Mana')) return 'Mana';
  if (effectType.includes('Charge')) return 'Charge';
  if (effectType.includes('Guard')) return 'Garde';
  if (effectType.includes('Revive')) return 'Réanimation';
  return effectType;
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

  if (
    !field || !active || !store.isPlayerTurn || active.hasMoved
      || store.selectedSkillKey || store.selectedItemId
  ) {
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
  const item = store.selectedItem;

  if (!field || !active || (!skill && !item) || !store.isPlayerTurn || active.hasActed) {
    return new Map();
  }

  const profile = skill
    ? tacticalSkillProfile(skill)
    : {
        range: item!.tacticalRange,
        shape: itemShape(item!),
        requiresLineOfSight: item!.requiresLineOfSight,
      };
  const cells = new Map<string, boolean>();

  for (let y = 0; y < field.height; y += 1) {
    for (let x = 0; x < field.width; x += 1) {
      if (!isWalkableCell(x, y)) continue;

      const elevationCost = Math.abs(elevationAt(active.x, active.y) - elevationAt(x, y));
      const inRange = profile.shape === 'map'
        || manhattan(active, { x, y }) + elevationCost <= profile.range;
      if (!inRange) continue;

      cells.set(
        battleCellKey(x, y),
        profile.requiresLineOfSight
          ? elevationAt(active.x, active.y) > elevationAt(x, y)
            || canSeeCell(active, { x, y })
          : true,
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
  if (!active || !hovered || store.selectedSkillKey || store.selectedItemId) return new Set();

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
  const item = store.selectedItem;
  const hovered = hoveredCell.value;
  if (!active || (!skill && !item) || !hovered) return new Set();

  if (rangePreview.value.get(battleCellKey(hovered.x, hovered.y)) !== true) return new Set();

  const shape = skill ? tacticalSkillProfile(skill).shape : itemShape(item!);
  return new Set(shapeCells(shape, hovered.x, hovered.y)
    .map((cell) => battleCellKey(cell.x, cell.y)));
});

type AttackArc = 'Face' | 'Flanc' | 'Dos';

function attackArc(
  attacker: { x: number; y: number },
  target: { x: number; y: number; facing: 'North' | 'East' | 'South' | 'West' },
): AttackArc {
  const dx = Math.sign(attacker.x - target.x);
  const dy = Math.sign(attacker.y - target.y);
  const direction = target.facing === 'North' ? [0, -1]
    : target.facing === 'East' ? [1, 0]
      : target.facing === 'South' ? [0, 1]
        : [-1, 0];
  const dot = (direction[0] * dx) + (direction[1] * dy);
  return dot > 0 ? 'Face' : dot < 0 ? 'Dos' : 'Flanc';
}

function cellsOnLine(from: BattleCell, to: BattleCell): BattleCell[] {
  const steps = Math.max(Math.abs(to.x - from.x), Math.abs(to.y - from.y));
  if (steps === 0) return [from];
  return Array.from({ length: steps + 1 }, (_, index) => {
    const ratio = index / steps;
    return {
      x: Math.round(from.x + ((to.x - from.x) * ratio)),
      y: Math.round(from.y + ((to.y - from.y) * ratio)),
    };
  });
}

function affinityLabel(
  emotionalType: EmotionalType | null | undefined,
  target: CombatantRuntimeDto,
): { label: string; multiplier: number } {
  if (!emotionalType || emotionalType === 'Neutral') return { label: 'Neutre', multiplier: 1 };
  if (target.immuneTo?.includes(emotionalType)) return { label: 'Immunité', multiplier: 0 };
  if (target.weakTo?.includes(emotionalType)) return { label: 'Faiblesse', multiplier: 1.5 };
  if (target.resistantTo?.includes(emotionalType)) return { label: 'Résistance', multiplier: 0.75 };
  return { label: 'Neutre', multiplier: 1 };
}

const targetPreview = computed(() => {
  const actor = store.activeCombatant;
  const skill = store.selectedSkill;
  const item = store.selectedItem;
  const hovered = hoveredCell.value;
  if (
    !actor || (!skill && !item) || !hovered
      || !targetableCells.value.has(battleCellKey(hovered.x, hovered.y))
  ) {
    return null;
  }

  const profile = skill
    ? tacticalSkillProfile(skill)
    : {
        shape: itemShape(item!),
        requiresLineOfSight: item!.requiresLineOfSight,
      };
  const intended = store.occupantAt(hovered.x, hovered.y)
    ?? (item?.targetingType === 'DefeatedAlly'
      ? store.allCombatants.find(
          (unit) =>
            unit.x === hovered.x
              && unit.y === hovered.y
              && unit.combatant.status === 'Defeated',
        ) ?? null
      : null);
  let interceptor = null as typeof intended;
  const origin = { x: actor.x, y: actor.y };
  const hasPlungingSight = elevationAt(actor.x, actor.y) > elevationAt(hovered.x, hovered.y);

  if (profile.requiresLineOfSight && profile.shape !== 'map' && !hasPlungingSight) {
    for (const cell of cellsOnLine(origin, hovered).slice(1, -1)) {
      const occupant = store.occupantAt(cell.x, cell.y);
      if (occupant) {
        interceptor = occupant;
        break;
      }
    }
  }

  const recipient = interceptor ?? intended;
  if (!recipient) {
    return {
      target: 'Case vide',
      interception: null,
      affinity: '—',
      position: '—',
      accuracy: null,
      critical: null,
      damage: null,
      effect: null,
      height: hasPlungingSight,
    };
  }

  const arc = attackArc(actor, recipient);
  const accuracyArcBonus = arc === 'Dos' ? 20 : arc === 'Flanc' ? 10 : 0;
  const damageArcMultiplier = arc === 'Dos' ? 1.10 : arc === 'Flanc' ? 1.05 : 1;
  const critArcBonus = arc === 'Dos' ? 4 : arc === 'Flanc' ? 2 : 0;
  const rollTarget = intended ?? recipient;
  const accuracy = clamp(
    90
      + (actor.combatant.hitChanceBonusPercent ?? 0)
      - (rollTarget.combatant.evasion ?? 0)
      + accuracyArcBonus,
    0,
    100,
  );
  const height = elevationAt(actor.x, actor.y) > elevationAt(recipient.x, recipient.y);
  const critical = clamp(
    (actor.combatant.focus ?? 0)
      + (actor.combatant.criticalChanceBonusPercent ?? 0)
      + critArcBonus
      + (height ? 4 : 0),
    0,
    50,
  );
  const affinity = affinityLabel(skill?.emotionalType, recipient.combatant);
  const attack = skill?.category === 'Magic'
    ? (actor.combatant.magicAttack ?? 0)
    : (actor.combatant.attackPower ?? 0);
  const defense = skill?.category === 'Magic'
    ? (recipient.combatant.magicDefense ?? 0)
    : (recipient.combatant.defense ?? 0);
  const situational = affinity.multiplier * damageArcMultiplier * (height ? 1.05 : 1);
  const minimumBase = defense <= 0
    ? (skill?.basePower ?? 0) * 1.15
    : (skill?.basePower ?? 0) * (attack / defense) * 0.85;
  const maximumBase = defense <= 0
    ? (skill?.basePower ?? 0) * 1.15
    : (skill?.basePower ?? 0) * (attack / defense) * 1.15;
  const damage = skill?.effectType === 'Damage'
    ? `${Math.max(0, Math.round(minimumBase * situational))}–${Math.max(0, Math.round(maximumBase * situational))}`
    : null;
  const percentItemEffect = item?.effectType.toLowerCase().includes('percent') ?? false;
  const itemAmount = item
    ? percentItemEffect
      ? Math.max(1, Math.round(recipient.combatant.maxVitality * item.effectAmount / 100))
      : item.effectAmount
    : 0;
  const effect = item
    ? item.effectType === 'RevivePercent'
      ? `Réanimation à ${item.effectAmount} %`
      : `${itemEffectLabel(item.effectType)} ${itemAmount}`
    : skill && skill.effectType !== 'Damage'
      ? `${skill.effectType} ${skill.basePower}`
      : null;

  return {
    target: recipient.combatant.displayName,
    interception: interceptor
      ? `${interceptor.combatant.displayName} intercepte ${intended?.combatant.displayName ?? 'la case'}`
      : null,
    affinity: affinity.label,
    position: arc,
    accuracy: skill ? accuracy : null,
    critical: skill ? critical : null,
    damage,
    effect,
    height,
  };
});

const heightCells = computed<Set<string>>(() => {
  const active = store.activeCombatant;
  const skill = store.selectedSkill;
  const item = store.selectedItem;
  const hovered = hoveredCell.value;
  if (!active || (!skill && !item) || !hovered) return new Set();

  const profile = skill
    ? tacticalSkillProfile(skill)
    : { requiresLineOfSight: item!.requiresLineOfSight };
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

/**
 * La portée de déplacement de l'allié survolé.
 *
 * L'ennemi survolé montre déjà son emprise complète (déplacement + portée) via
 * <c>threatCells</c> ; un allié, lui, ne montrait rien tant qu'il n'était pas l'unité active.
 * On répond ici à la même question posée dans l'autre sens — « jusqu'où celui-là peut-il aller
 * quand son tour viendra ? » — sans reprendre la hachure rouge, qui ne veut dire que menace.
 * L'unité active en est exclue : sa portée est déjà peinte en permanence.
 */
const hoveredAllyCells = computed<Set<string>>(() => {
  const field = battlefield.value;
  const hovered = hoveredCell.value;
  if (!field || !hovered) return new Set();
  // Une compétence armée détourne toute la lecture du plateau vers sa propre zone.
  if (store.selectedSkillKey || store.selectedItemId) return new Set();

  const ally = (store.combat?.allies ?? []).find(
    (unit) => unit.x === hovered.x
      && unit.y === hovered.y
      && unit.combatant.status !== 'Defeated',
  );

  if (!ally || ally.combatant.id === store.combat?.activeCombatantId) return new Set();
  // Un allié qui a déjà bougé ne va plus nulle part ; l'annoncer autrement serait faux.
  if (ally.hasMoved) return new Set();

  const occupied = new Set(occupiedKeys.value);
  occupied.delete(battleCellKey(ally.x, ally.y));

  return reachableCellsFrom(
    {
      gridWidth: field.width,
      gridHeight: field.height,
      elevation: field.elevation,
      walkable: field.walkable,
    },
    ally,
    ally.movementBudget,
    occupied,
  );
});

/**
 * Les cases qu'un geste adverse annoncé s'apprête à couvrir, le temps de l'annonce.
 * Un trajet se lit comme un trajet, une action comme un impact.
 */
const telegraphMoveCells = computed<Set<string>>(() => {
  const announced = store.playback.telegraph;
  if (!announced || announced.kind !== 'Move') return new Set();

  return new Set(announced.cells.map((cell) => battleCellKey(cell.x, cell.y)));
});

const telegraphImpactCells = computed<Set<string>>(() => {
  const announced = store.playback.telegraph;
  if (!announced || announced.kind === 'Move') return new Set();

  return new Set(announced.cells.map((cell) => battleCellKey(cell.x, cell.y)));
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
    // La portée de l'allié survolé emprunte la même surbrillance que le déplacement du
    // combattant actif : c'est la même information, lue à l'avance sur quelqu'un d'autre.
    reachableCells: new Set([...reachableCells.value, ...hoveredAllyCells.value]),
    targetableCells: targetableCells.value,
    blockedCells: blockedCells.value,
    // Un trajet annoncé se peint comme un trajet — la même surbrillance que l'aperçu de
    // déplacement du joueur, pour qu'il n'y ait qu'une seule grammaire à apprendre.
    pathCells: new Set([...pathCells.value, ...telegraphMoveCells.value]),
    aoeCells: new Set([...aoeCells.value, ...telegraphImpactCells.value]),
    threatCells: threatCells.value,
    heightCells: heightCells.value,
    occupiedCells: occupiedHighlightCells.value,
    hoveredCell: hoveredCell.value,
    nodesByCell: battlefieldNodesByCell.value,
  });
});

/**
 * Les combattants, dans l'ordre du peintre — mêmes clés de profondeur que le terrain, pour
 * qu'une figure devant une tuile haute ne passe pas derrière elle.
 */
function buildCombatantPlan(now: number) {
  if (!battlefield.value || canvasSize.value.width === 0) return [];

  return store.allCombatants
    // À zéro Vitalité l'unité quitte immédiatement la grille. Elle reste dans le runtime
    // et l'initiative afin qu'un objet ou une compétence puisse la réanimer.
    .filter((unit) => unit.combatant.status !== 'Defeated')
    .map((unit) => {
      // La position affichée n'est pas celle du serveur tant que la chronologie se joue : une
      // figure en marche est interpolée entre deux cases, pas posée sur sa destination.
      const at = store.playback.positionOf(unit.combatant.id, { x: unit.x, y: unit.y }, now);
      const elevation = elevationAt(Math.round(at.x), Math.round(at.y));
      // Hauteur affichée : lissée en continu le long du même chemin fractionnaire que
      // screenX/screenY, pour ne jamais sauter d'un palier à l'autre à mi-pas.
      const liftElevation = elevationAtFractional(at.x, at.y);
      const { screenX, screenY } = projectToScreen(at.x, at.y, projectionParams.value);

      return {
        unit,
        elevation,
        liftElevation,
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
  const displayedVitality = store.playback.vitalsOf(combatant.id, combatant.currentVitality);
  const ratio = Math.max(0, Math.min(1, displayedVitality / Math.max(1, combatant.maxVitality)));

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

function paintFacingArrow(
  ctx: CanvasRenderingContext2D,
  entry: ReturnType<typeof buildCombatantPlan>[number],
  groundY: number,
  width: number,
) {
  const delta = entry.unit.facing === 'North' ? { x: 0, y: -1 }
    : entry.unit.facing === 'East' ? { x: 1, y: 0 }
      : entry.unit.facing === 'South' ? { x: 0, y: 1 }
        : { x: -1, y: 0 };
  const origin = projectToScreen(entry.unit.x, entry.unit.y, projectionParams.value);
  const destination = projectToScreen(
    entry.unit.x + delta.x,
    entry.unit.y + delta.y,
    projectionParams.value,
  );
  const dx = destination.screenX - origin.screenX;
  const dy = destination.screenY - origin.screenY;
  const length = Math.max(1, Math.hypot(dx, dy));
  const ux = dx / length;
  const uy = dy / length;
  const radius = width * 0.34;
  const tipX = entry.screenX + (ux * radius);
  const tipY = groundY + (uy * radius * 0.55);
  const sideX = -uy * width * 0.055;
  const sideY = ux * width * 0.055;

  ctx.save();
  ctx.fillStyle = entry.unit.combatant.side === 'Enemy' ? '#e0605e' : '#86dcb4';
  ctx.strokeStyle = 'rgba(0, 0, 0, 0.75)';
  ctx.lineWidth = Math.max(1, width * 0.012);
  ctx.beginPath();
  ctx.moveTo(tipX, tipY);
  ctx.lineTo(
    entry.screenX + (ux * radius * 0.62) + sideX,
    groundY + (uy * radius * 0.34) + sideY,
  );
  ctx.lineTo(
    entry.screenX + (ux * radius * 0.62) - sideX,
    groundY + (uy * radius * 0.34) - sideY,
  );
  ctx.closePath();
  ctx.fill();
  ctx.stroke();
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

function paintEscape(ctx: CanvasRenderingContext2D, destW: number, destH: number, timestamp: number) {
  const exit = store.combat?.escape;
  if (!exit) return;

  const { screenX, screenY } = projectToScreen(exit.x, exit.y, projectionParams.value);
  const lift = elevationLiftPx(elevationAt(exit.x, exit.y));
  const pulse = prefersReducedMotion ? 1 : 0.88 + (Math.sin(timestamp * 0.003) * 0.12);

  ctx.save();
  ctx.translate(screenX, screenY - lift);
  ctx.scale(pulse, pulse);
  ctx.strokeStyle = '#b9e7d1';
  ctx.fillStyle = 'rgba(75, 176, 130, .24)';
  ctx.shadowColor = '#70d4a8';
  ctx.shadowBlur = destW * 0.16;
  ctx.lineWidth = Math.max(2, destW * 0.025);
  ctx.beginPath();
  ctx.ellipse(0, 0, destW * 0.32, destH * 0.18, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
  ctx.font = `600 ${Math.max(10, Math.round(destW * 0.11))}px ui-monospace, monospace`;
  ctx.textAlign = 'center';
  ctx.fillStyle = '#e3fff1';
  ctx.fillText('SORTIE', 0, -(destH * 0.18));
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

  paintBackdrop(ctx, canvas.width, canvas.height, timestamp);

  const { destW, destH, propH } = spriteDest.value;

  const terrainPlan = drawPlan.value.filter((entry) => entry.spriteKey.kind !== 'highlight');
  const highlightPlan = drawPlan.value.filter((entry) => entry.spriteKey.kind === 'highlight');

  const tierKey = {
    Calme: 'calm',
    Tendu: 'tense',
    Dangereux: 'grim',
    Perilleux: 'grim',
    Fatal: 'fatal',
  }[store.combat?.riskTier ?? 'Calme'];
  const tier = RISK_TIERS[tierKey] ?? RISK_TIERS.calm;

  // Ambiance (particules) : peinte ICI, avant la vignette et avant le terrain, pour qu'elle
  // fasse partie du "fond" désaturé — comme le ciel/décor, mais jamais les dalles ni les unités.
  if (!prefersReducedMotion) {
    ctx.save();
    drawAmbient(ctx, canvas.width, canvas.height, roomTheme.value, timestamp);
    ctx.restore();
  }

  // Le voile de désaturation (drawCombatGrade) a été retiré : même désaturé seulement en arrière-
  // plan, il lisait comme un écart de qualité avec l'exploration. La difficulté du palier de
  // risque doit se lire ailleurs (particules, teintes d'ambiance...) plutôt que par un filtre
  // global sur l'image — voir `tier.accent`/`tier.ambient`, déjà utilisés par drawAmbient.

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
    const displayedVitality = store.playback.vitalsOf(entry.unit.combatant.id, entry.unit.combatant.currentVitality);
    const hpRatio = Math.max(0, Math.min(1, displayedVitality / Math.max(1, entry.unit.combatant.maxVitality)));

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
      const lift = elevationLiftPx(entry.liftElevation);
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
      paintFacingArrow(ctx, entry, groundY, destW);

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

  paintEscape(ctx, destW, destH, timestamp);

  // ── FX / chiffres ─────────────────────────────────────────────────────────
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
    const skill = store.selectedSkill;
    if (!skill || skillUnavailable(skill)) return;

    const sacrifice = vitalitySacrifice(skill);
    if (sacrifice > 0) {
      const actorVitality = store.activeCombatant?.combatant.currentVitality ?? 0;
      const lethalNotice = sacrifice >= actorVitality
        ? ' Le lanceur mourra après la résolution et la compétence gagnera 50 % de puissance.'
        : '';
      const accepted = globalThis.confirm(
        `Mana insuffisante : « ${skill.displayName} » consommera exactement `
        + `${sacrifice} Vitalité (${missingMana(skill)} Mana manquante).`
        + `${lethalNotice}\n\nConfirmer le sacrifice ?`,
      );
      if (!accepted) return;
    }

    void store.useSkillAt(
      props.runId,
      store.selectedSkillKey,
      cell.x,
      cell.y,
      sacrifice > 0,
    );
    return;
  }

  if (store.selectedItemId) {
    const defeated = store.allCombatants.find(
      (unit) =>
        unit.x === cell.x
          && unit.y === cell.y
          && unit.combatant.status === 'Defeated',
    );
    void store.useItemAt(
      props.runId,
      store.selectedItemId,
      cell.x,
      cell.y,
      defeated?.combatant.id,
    );
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
    if (status === 'Escaped') emit('combat-escaped');
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

// N'auto-replace le panneau que si le joueur ne l'a pas déplacé lui-même
// (placePanelNearActive s'auto-garde déjà sur panelWasDragged) — un déplacement
// ou une fin de tour ne doit jamais annuler un positionnement manuel.
watch(
  [
    () => store.combat?.activeCombatantId,
    () => store.activeCombatant?.x,
    () => store.activeCombatant?.y,
    () => canvasSize.value.width,
    () => canvasSize.value.height,
  ],
  () => {
    globalThis.requestAnimationFrame(() => placePanelNearActive());
  },
);

onMounted(() => {
  skillsApi.listActive()
    .then((response) => {
      skillCatalog.value = new Map(response.skills.map((skill) => [skill.key, skill]));
    })
    .catch(() => {
      // Best-effort: a failed catalog fetch just disables the journal's skill links.
    });

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
  stopPanelDrag?.();
  observer?.disconnect();
  observer = null;
});
</script>

<template>
  <section
    v-if="store.combat"
    ref="battleEl"
    class="tbattle"
    :class="{ 'tbattle--transitioning': playback.isTransitioning }"
  >
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
            {{ displayedVitality(unit) }}/{{ unit.combatant.maxVitality }}
          </span>
        </li>
      </ol>
    </aside>

    <!-- ── Central board ── -->
    <div class="tbattle__board">
      <!-- Deux temps dans le même bandeau : « X prépare Y » pendant que sa zone s'allume, puis
           « X — Y » pendant que le coup retombe. L'annonce prime, c'est elle qui est urgente. -->
      <Transition name="tbattle-banner">
        <p
          v-if="store.playback.telegraph || store.playback.actionBanner"
          class="tbattle__banner"
          :class="{ 'tbattle__banner--telegraph': store.playback.telegraph }"
        >
          {{ store.playback.telegraph?.label ?? store.playback.actionBanner }}
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
          <button
            v-if="entry.skillKey"
            type="button"
            class="tbattle__log-skill-link"
            title="Voir le descriptif du sort"
            @click="openSkillDetail(entry.skillKey)"
          >
            ⓘ
          </button>
        </p>
      </div>
    </aside>

    <SkillDetailModal :skill="inspectedSkill" @close="inspectedSkill = null" />

    <!-- ── Bottom bar: portraits + skills ── -->
    <footer ref="panelEl" class="tbattle__bottom" :style="panelStyle">
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
                :class="{ 'tbattle__portrait-hp-fill--low': displayedVitality(unit) / Math.max(1, unit.combatant.maxVitality) < 0.3 }"
                :style="{ width: `${Math.max(0, Math.min(100, (displayedVitality(unit) / Math.max(1, unit.combatant.maxVitality)) * 100))}%` }"
              />
            </div>
            <span class="tbattle__portrait-hp-text">
              {{ displayedVitality(unit) }}/{{ unit.combatant.maxVitality }}
            </span>
          </div>
          <div
            v-if="displayedStatusEffects(unit).length"
            class="tbattle__portrait-statuses"
          >
            <StatusEffectToken
              v-for="status in displayedStatusEffects(unit)"
              :key="status.key"
              :kind="status.kind"
              :magnitude="status.magnitude"
              :stat="status.stat"
              :is-magnitude-percent-of-base-stat="status.isMagnitudePercentOfBaseStat"
              :stacks="status.stacks"
              :px="20"
              teleport-bubble
              :per-tick-amount="status.perTickAmount"
              :ticks-remaining="status.ticksRemaining"
              :is-permanent="status.isPermanent"
            />
          </div>
        </div>
      </div>

      <!-- Skills + controls : toujours visibles, grisés hors tour joueur -->
      <div class="tbattle__controls">
        <div
          class="tbattle__active-info"
          title="Faire glisser le panneau"
          @pointerdown="beginPanelDrag"
        >
          <span class="tbattle__drag-handle" aria-hidden="true">⠿</span>
          <strong class="tbattle__active-name">
            {{ store.activeCombatant?.combatant?.displayName ?? '—' }}
          </strong>
          <span class="tbattle__active-stat">
            Mana {{ store.activeCombatant?.combatant?.mana ?? '—' }}
          </span>
          <span class="tbattle__active-stat">
            Charge {{ store.activeCombatant?.combatant?.charge ?? '—' }} / 5
          </span>
          <span class="tbattle__active-stat">
            Face {{ facingLabel(store.activeCombatant?.facing) }}
          </span>
          <span class="tbattle__risk-tier">
            Palier {{ riskTierLabel(store.combat?.riskTier) }}
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
          <button
            v-if="panelWasDragged"
            type="button"
            class="tbattle__panel-reset"
            title="Replacer près de l’unité active"
            @pointerdown.stop
            @click="placePanelNearActive(true)"
          >
            Recentrer
          </button>
        </div>

        <div
          v-if="store.activeCombatant && displayedStatusEffects(store.activeCombatant).length"
          class="tbattle__statuses"
          aria-label="États actifs"
        >
          <span
            v-for="status in displayedStatusEffects(store.activeCombatant!)"
            :key="status.key"
            class="tbattle__status"
            :title="statusTitle(status)"
          >
            {{ status.displayName }}
            <b v-if="status.stacks > 1">×{{ status.stacks }}</b>
            <small v-if="status.ticksRemaining !== null">
              {{ ticksToTurns(status.ticksRemaining) }} t.
            </small>
            <small v-else>permanent</small>
          </span>
        </div>

        <div class="tbattle__skills">
          <template v-if="store.isPlayerTurn">
            <button
              v-for="skill in store.activeSkills"
              :key="skill.key"
              type="button"
              class="tbattle__skill"
              :class="{
                'tbattle__skill--armed': skill.key === store.selectedSkillKey,
                'tbattle__skill--ultimate': skill.isUltimate,
                'tbattle__skill--sacrifice': vitalitySacrifice(skill) > 0,
              }"
              :disabled="skillUnavailable(skill)"
              @mouseenter="showSkillTooltip(skill.key, $event.currentTarget)"
              @mouseleave="hideSkillTooltip"
              @focus="showSkillTooltip(skill.key, $event.currentTarget)"
              @blur="hideSkillTooltip"
              @click="store.selectSkill(skill.key)"
            >
              <span class="tbattle__skill-name">
                {{ skill.isUltimate ? 'Ultime · ' : '' }}{{ skill.displayName }}
              </span>
              <span class="tbattle__skill-meta">{{ skillMeta(skill) }}</span>
              <span class="tbattle__skill-cost">{{ skillCostMeta(skill) }}</span>
              <span v-if="vitalitySacrifice(skill) > 0" class="tbattle__skill-warning">
                −{{ vitalitySacrifice(skill) }} Vitalité
              </span>
            </button>
            <button
              v-for="item in store.usableItems"
              :key="item.itemId"
              type="button"
              class="tbattle__skill tbattle__item"
              :class="{ 'tbattle__item--armed': item.itemId === store.selectedItemId }"
              :disabled="(store.activeCombatant?.hasActed ?? true) || store.isLoading"
              :title="`${item.displayName} — ${itemMeta(item)}`"
              @click="store.selectItem(item.itemId)"
            >
              <span class="tbattle__skill-name">{{ item.displayName }}</span>
              <span class="tbattle__skill-meta">{{ itemMeta(item) }}</span>
            </button>
          </template>
          <span v-else class="tbattle__skills-placeholder" />
        </div>

        <Teleport to="body">
          <div
            v-if="hoveredSkill"
            class="tbattle__skill-tooltip"
            role="tooltip"
            :style="{ left: skillTooltipStyle.left, bottom: skillTooltipStyle.bottom }"
          >
            <div class="tbattle__skill-tooltip-title">
              {{ hoveredSkill.isUltimate ? 'Ultime · ' : '' }}{{ hoveredSkill.displayName }}
            </div>
            <p v-if="hoveredSkillDescription" class="tbattle__skill-tooltip-desc">
              {{ hoveredSkillDescription }}
            </p>
            <div class="tbattle__skill-tooltip-meta">
              {{ hoveredSkill.category === 'Magic' ? 'Magique' : 'Physique' }}
              · {{ skillMeta(hoveredSkill) }} · {{ skillCostMeta(hoveredSkill) }}
            </div>
            <div v-if="vitalitySacrifice(hoveredSkill) > 0" class="tbattle__skill-tooltip-warning">
              Sacrifice : −{{ vitalitySacrifice(hoveredSkill) }} Vitalité
            </div>
          </div>
        </Teleport>

        <button
          type="button"
          class="tbattle__end-turn"
          :disabled="store.isLoading || !store.isPlayerTurn"
          @click="store.endTurn(props.runId)"
        >
          Finir le tour
        </button>
      </div>

      <p
        v-if="targetPreview"
        class="tbattle__preview"
        aria-live="polite"
      >
        <strong>{{ targetPreview.target }}</strong>
        <span v-if="targetPreview.interception" class="tbattle__preview-warning">
          {{ targetPreview.interception }}
        </span>
        <span>Position {{ targetPreview.position }}</span>
        <span v-if="targetPreview.height">Hauteur +5 % dégâts · +4 critique</span>
        <span>Affinité {{ targetPreview.affinity }}</span>
        <span v-if="targetPreview.accuracy !== null">Précision {{ targetPreview.accuracy }} %</span>
        <span v-if="targetPreview.critical !== null">Critique {{ targetPreview.critical }} %</span>
        <span v-if="targetPreview.damage">Dégâts {{ targetPreview.damage }}</span>
        <span v-if="targetPreview.effect">{{ targetPreview.effect }}</span>
      </p>

      <p
        v-if="store.isPlayerTurn && (store.selectedSkillKey || store.selectedItemId)"
        class="tbattle__hint"
      >
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
  position: relative;
  display: grid;
  grid-template-columns: 170px 1fr 180px;
  grid-template-rows: 1fr;
  grid-template-areas:
    "initiative board log";
  height: 100%;
  min-height: 0;
  gap: 0;
}

.tbattle--transitioning {
  opacity: 0.5;
  transition: opacity 300ms ease-in-out;
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

/* L'annonce se distingue du compte rendu : bord rouge, elle prévient au lieu de raconter. */
.tbattle__banner--telegraph {
  border-color: rgb(224 96 94 / 65%);
  color: #ffd8d0;
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

.tbattle__log-skill-link {
  all: unset;
  cursor: pointer;
  margin-left: 4px;
  font-size: 0.72rem;
  color: var(--gold);
  opacity: 0.8;
}

.tbattle__log-skill-link:hover {
  opacity: 1;
  text-decoration: underline;
}

/* ── Bottom bar: portraits + skills ─────────────────────────────────────── */
.tbattle__bottom {
  position: absolute;
  z-index: 8;
  width: min(720px, calc(100% - 380px));
  max-height: min(46vh, 330px);
  overflow: auto;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.45rem 0.75rem;
  border: 1px solid rgb(230 194 115 / 28%);
  border-radius: 8px;
  background: rgb(9 11 22 / 90%);
  backdrop-filter: blur(12px);
  box-shadow: 0 14px 44px rgb(0 0 0 / 45%);
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

.tbattle__portrait-statuses {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 2px;
  margin-top: 2px;
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
  cursor: grab;
  user-select: none;
  touch-action: none;
}

.tbattle__active-info:active { cursor: grabbing; }
.tbattle__drag-handle { color: #e6c273; opacity: 0.65; }

.tbattle__panel-reset {
  border: 0;
  background: transparent;
  color: #e6c273;
  cursor: pointer;
  font-size: 0.65rem;
  margin-left: auto;
}

.tbattle__active-name { color: #e6c273; font-size: 0.85rem; }

.tbattle__active-stat {
  font-size: 0.75rem;
  opacity: 0.7;
  font-variant-numeric: tabular-nums;
}

.tbattle__active-stat--spent { opacity: 0.35; text-decoration: line-through; }

.tbattle__risk-tier {
  padding: 0.12rem 0.38rem;
  border: 1px solid rgb(230 194 115 / 35%);
  border-radius: 999px;
  color: #e6c273;
  font-size: 0.65rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.tbattle__statuses {
  display: flex;
  flex-wrap: wrap;
  gap: 0.3rem;
  width: 100%;
}

.tbattle__status {
  display: inline-flex;
  align-items: baseline;
  gap: 0.2rem;
  padding: 0.15rem 0.35rem;
  border: 1px solid rgb(230 194 115 / 30%);
  border-radius: 999px;
  background: rgb(230 194 115 / 8%);
  color: #eee9dc;
  font-size: 0.65rem;
}

.tbattle__status small { opacity: 0.6; }

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
.tbattle__skill--ultimate { border-color: rgb(230 194 115 / 55%); }
.tbattle__skill--sacrifice:not(:disabled) { border-color: rgb(224 96 94 / 70%); }
.tbattle__item { border-color: rgb(102 185 171 / 45%); }
.tbattle__item--armed { background: rgb(102 185 171 / 22%); border-color: #66b9ab; }
.tbattle__skill:disabled { opacity: 0.35; cursor: not-allowed; }
.tbattle__skill-name { font-weight: 600; }
.tbattle__skill-meta { font-size: 0.62rem; opacity: 0.55; font-variant-numeric: tabular-nums; }
.tbattle__skill-cost { font-size: 0.62rem; color: #e6c273; font-variant-numeric: tabular-nums; }
.tbattle__skill-warning { font-size: 0.62rem; color: #e0605e; font-variant-numeric: tabular-nums; }

.tbattle__end-turn {
  font-weight: 600;
  color: #e6c273;
  border-color: rgb(230 194 115 / 35%);
}

.tbattle__end-turn:hover:not(:disabled) {
  background: rgb(230 194 115 / 12%);
}

.tbattle__hint { margin: 0; font-size: 0.72rem; opacity: 0.45; }
.tbattle__preview {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem 0.75rem;
  margin: 0;
  padding: 0.35rem 0.5rem;
  border-radius: 4px;
  background: rgb(255 255 255 / 5%);
  color: rgb(244 241 255 / 72%);
  font-size: 0.68rem;
}
.tbattle__preview strong { color: #f4f1ff; }
.tbattle__preview-warning { color: #e0605e; }
.tbattle__waiting { opacity: 0.6; font-style: italic; margin: 0; font-size: 0.8rem; }
.tbattle__error { color: #e0605e; margin: 0; font-size: 0.8rem; }
</style>

<!-- Unscoped: teleported to <body>, outside this component's scoped-style DOM subtree — the
     draggable action panel (.tbattle__bottom) clips overflow on both axes, so this tooltip has
     to live outside it entirely rather than merely escape it visually. -->
<style>
.tbattle__skill-tooltip {
  position: fixed;
  z-index: 300;
  width: 260px;
  padding: 10px 12px;
  border-radius: 6px;
  border: 1px solid rgb(230 194 115 / 35%);
  background: rgb(9 11 22 / 96%);
  backdrop-filter: blur(12px);
  box-shadow: 0 14px 44px rgb(0 0 0 / 45%);
  color: rgb(244 241 255 / 88%);
  font-family: var(--font, serif);
  pointer-events: none;
}

.tbattle__skill-tooltip-title {
  font-weight: 600;
  font-size: 0.85rem;
  color: #e6c273;
  margin-bottom: 4px;
}

.tbattle__skill-tooltip-desc {
  margin: 0 0 6px;
  font-size: 0.75rem;
  line-height: 1.45;
  color: rgb(244 241 255 / 72%);
}

.tbattle__skill-tooltip-meta {
  font-size: 0.68rem;
  letter-spacing: 0.02em;
  color: rgb(244 241 255 / 55%);
}

.tbattle__skill-tooltip-warning {
  margin-top: 4px;
  font-size: 0.68rem;
  color: #e0605e;
}
</style>
