<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';

import SigilIcon from '../../shared/components/SigilIcon.vue';
import type { NodeDto, RoomDto } from '../runs/types/runTypes';
import { usePalaceTerrain } from './composables/usePalaceTerrain';
import { usePartyTokenPath } from './composables/usePartyTokenPath';
import { useNodePresentation, NODE_TILE_TONE, RISK_TIER_DISPLAY } from './composables/useNodePresentation';
import { useRoomBackdropTheme } from './composables/useRoomBackdropTheme';
import { useGridCells, type Cell } from './composables/useGridCells';
import {
  useTerrainSprites,
  drawAmbient,
  drawBackdrop,
  drawDangerAura,
  drawFireFx,
  drawRevealFx,
  drawStarFx,
  drawFogOfWar,
  visionRadius,
  GROUND_ANCHOR_RATIO,
  PROP_GROUND_ANCHOR_RATIO,
  anchorRatioAt,
  TERRAIN_SPRITE_CONSTANTS,
  type FloorTint,
} from './composables/useTerrainSprites';
import { buildDrawPlan, isoUnit, projectToScreen, screenToCell } from './composables/useTerrainDrawPlan';
import { buildMovementRange } from './composables/useMovementRange';

const props = defineProps<{
  room: RoomDto;
  /** Total count of active laws/curses/modifiers — badge on the always-visible Lois button. */
  influenceCount?: number;
}>();

const emit = defineEmits<{
  moveRequest: [x: number, y: number];
  enterNode: [nodeId: string];
  wagerNode: [nodeId: string];
  challengeBoss: [];
  search: [];
  toggleLaws: [];
}>();

// Same display name logic as GameTopBar's "Salle" segment — shown here instead in
// Tactical mode (see the room-name tab next to "Exploration tactique").
const roomName = computed(() =>
  props.room.catalogName || props.room.theme || props.room.roomType || '—');

const room = computed(() => props.room);
const grid = computed(() => props.room.grid ?? null);

const { isRevealed, nodeAt, isParty, cells, obstacleCells, isFloor, nodesByCell } =
  useGridCells(room, grid);

const { displayPartyX, displayPartyY } = usePartyTokenPath(room, grid);

const { terrainHeight } = usePalaceTerrain(room, grid);

// ── Canvas terrain renderer ──────────────────────────────────────────────────────
// A single `<canvas>` paints hand-painted isometric tiles (see useTerrainSprites.ts, backed by
// the dependency-free tilecraft engine) at real pixel positions (see useTerrainDrawPlan.ts).
// Terrain, walls, scenery, the party token and the hover highlight all live INSIDE the canvas
// so they share one depth-sorted painter's pass; only the surrounding chrome — side panel,
// tabs, tooltip, boss banner — stays a DOM overlay on top. Nodes used to also carry a floating
// SVG sigil here; the painted props say the same thing in the scene's own language, so the
// sigil now appears only in the side panel where it labels the node you are standing on.
const canvasContainerEl = ref<HTMLElement | null>(null);
/** The cell under the pointer. Declared up here because the draw plan reads it. */
const hoveredCell = ref<Cell | null>(null);
const canvasEl = ref<HTMLCanvasElement | null>(null);
// A square fallback (not 0×0) so every projection/hit-test stays meaningful before the
// ResizeObserver's first real measurement — first paint, or a test environment (e.g. jsdom)
// that doesn't implement ResizeObserver at all.
const canvasSize = ref({ width: 100, height: 100 });
let canvasObserver: ResizeObserver | null = null;

watch(canvasContainerEl, (el) => {
  canvasObserver?.disconnect();
  canvasObserver = null;
  if (!el || typeof ResizeObserver === 'undefined') return;
  canvasObserver = new ResizeObserver(([entry]) => {
    canvasSize.value = { width: entry.contentRect.width, height: entry.contentRect.height };
  });
  canvasObserver.observe(el);
});

onBeforeUnmount(() => canvasObserver?.disconnect());

// `paintedTheme` is what drives the tile materials, palette and backdrop scenery; `themeAccent`
// is the CSS token the surrounding DOM chrome borrows so panel/tab accents match the room.
const { paintedTheme, themeAccent } = useRoomBackdropTheme(room);
const { sigilKindFor, nodeTypeLabel, nodeTypeDescription } = useNodePresentation();

const TONE_VALUES = new Set(['blood', 'gold', 'frost', 'sap']);

const ambientTint = computed<FloorTint>(() => {
  const accent = themeAccent.value.replace('--', '');
  return (TONE_VALUES.has(accent) ? accent : 'neutral') as FloorTint;
});

function nodeTintFor(node: NodeDto): FloorTint {
  const tone = NODE_TILE_TONE[node.type];
  return (tone && TONE_VALUES.has(tone) ? tone : 'neutral') as FloorTint;
}

// BALANCE KNOB — opacity of a wall or raised tile that stands between the camera and the
// party (or the cell being hovered). Low enough to see through, high enough that the terrain
// still reads as solid rather than as a hole.
const OCCLUDER_ALPHA = 0.42;

// BALANCE KNOB — how much of the fog veil actually lands. Below ~0.6 the unexplored board reads
// as merely dim and the fog stops being a real barrier to knowledge; at 1 it is a black wall and
// the painted backdrop behind it is wasted. See paintFogOfWar for why this is applied here
// rather than inside the tile engine.
const FOG_VEIL_ALPHA = 0.76;

/**
 * The route the party would walk, drawn over its tiles.
 *
 * The engine's own 'path' highlight is a pale wash, and laid on top of the reachable wash —
 * which is also pale, and also blue — it simply disappeared. Amber is the one hue nothing else
 * on the board uses, so the route separates from the range at a glance instead of having to be
 * picked out of it. Painted as the tile's own top-face diamond so it hugs the terrain at
 * whatever height the cell sits at, rather than floating as a flat screen-space shape.
 */
function paintPathMarker(
  ctx: CanvasRenderingContext2D,
  dx: number, dy: number, destW: number, destH: number,
  elevationLevel: number,
  isDestination: boolean,
) {
  const cx = dx + (destW / 2);
  const cy = dy + (destH * anchorRatioAt(elevationLevel));
  const halfW = destW / 2;
  const halfH = halfW / 2; // the board's 2:1 dimetric diamond

  ctx.save();
  ctx.beginPath();
  ctx.moveTo(cx, cy - halfH);
  ctx.lineTo(cx + halfW, cy);
  ctx.lineTo(cx, cy + halfH);
  ctx.lineTo(cx - halfW, cy);
  ctx.closePath();

  // The last cell of the route reads stronger: it is the one being chosen, the rest is how
  // the party gets there.
  ctx.fillStyle = isDestination ? 'rgba(255, 176, 66, 0.42)' : 'rgba(255, 158, 51, 0.22)';
  ctx.fill();
  ctx.strokeStyle = isDestination ? 'rgba(255, 208, 130, 0.95)' : 'rgba(255, 184, 92, 0.7)';
  ctx.lineWidth = isDestination ? 2.4 : 1.6;
  ctx.stroke();
  ctx.restore();
}

/**
 * Whether `entry` is painted in front of `target` AND actually covers it. Only things that
 * genuinely rise off the ground can hide anything — a flat floor tile never does — so the test
 * is limited to walls, scenery and raised tiles, and to entries that sort later (i.e. paint on
 * top). The horizontal test uses the tile's own half-width; the vertical one only looks
 * upwards, since a sprite covers the ground behind it, never in front.
 */
function occludesTarget(
  entry: { spriteKey: { kind: string; elevation?: number }; screenX: number; screenY: number; sortKey: number },
  target: { screenX: number; screenY: number; sortKey: number },
  destW: number,
  destH: number,
): boolean {
  if (entry.sortKey <= target.sortKey) return false;

  const rises = entry.spriteKey.kind === 'obstacle'
    || entry.spriteKey.kind === 'prop'
    || (entry.spriteKey.kind === 'floor' && (entry.spriteKey.elevation ?? 0) > 0);
  if (!rises) return false;

  const dx = Math.abs(entry.screenX - target.screenX);
  const dy = target.screenY - entry.screenY;

  return dx < destW * 0.5 && dy > -destH * 0.25 && dy < destH * 1.1;
}

// BALANCE KNOB — how much slower the ambient particles drift than the engine's own default.
// They read as dust hanging in the air rather than as motion competing with the board.
const AMBIENT_TIME_SCALE = 0.45;

/**
 * Cells the player may actually click right now: revealed, real floor, not a wall, not where
 * the party already stands. Highlighted so the reachable area is readable at a glance — the
 * fog alone left the player guessing which of the dimmed cells were legal targets.
 *
 * Deliberately NOT a budget/pathfinding check: the store stays optimistic and the domain is the
 * authority on affordability (it answers with a real message). Reproducing the pathfinder here
 * would be a second source of truth free to disagree with the server.
 */
/**
 * What a move would really cost, from where the party stands. Rebuilt whenever the party moves,
 * the budget changes, or a node's state changes — a node that stops blocking reopens a route.
 */
const movementRange = computed(() => {
  const g = grid.value;
  if (!g) return null;

  const blockers = new Set<string>();
  const triggers = new Set<string>();

  for (const node of room.value?.nodes ?? []) {
    if (node.state === 'Resolved') continue;
    const key = `${node.lane},${node.row}`;
    // Mirrors MapNode.TriggersOnContact — a lock fires on contact too, it just also bars transit.
    if (node.contactBehavior === 'Blocking') {
      blockers.add(key);
      triggers.add(key);
    } else if (node.contactBehavior === 'TriggerOnEnter') {
      triggers.add(key);
    }
  }

  return buildMovementRange({
    gridWidth: g.width,
    gridHeight: g.height,
    elevation: g.elevation,
    isWalkable: (x, y) => isFloor(x, y) && !obstacleCells.value.has(`${x},${y}`),
    party: { x: g.partyX, y: g.partyY },
    transitBlockers: blockers,
    contactTriggers: triggers,
  });
});

/**
 * Cells the player can actually click. Previously this was every revealed floor cell, which
 * promised reachability the budget could not pay for: the far corner lit up, the click was
 * refused by the server, and the whole thing read as broken movement. The wash now means what
 * it says — inside it, a click always works.
 */
const reachableCells = computed(() => {
  const g = grid.value;
  const range = movementRange.value;
  if (!g || !range) return new Set<string>();

  const affordable = range.within(g.movementBudgetRemaining);
  const set = new Set<string>();

  for (const key of affordable) {
    const [x, y] = key.split(',').map(Number);
    // Fog still gates intent: the party does not walk to somewhere nobody has seen.
    if (isRevealed(x, y)) set.add(key);
  }

  return set;
});

/** The walk that clicking the hovered cell would produce — null when it is out of reach. */
const hoveredRoute = computed(() => {
  const cell = hoveredCell.value;
  const range = movementRange.value;
  if (!cell || !range) return null;
  if (!reachableCells.value.has(`${cell.x},${cell.y}`)) return null;
  return range.routeTo(cell.x, cell.y);
});

/**
 * What the move under the pointer would spend, and what would be left after. The budget is the
 * whole tension of the room, so the price has to be readable at the moment of the decision
 * rather than discovered from a counter that already dropped.
 */
const moveReadout = computed(() => {
  const route = hoveredRoute.value;
  const g = grid.value;
  if (!route || !g) return null;

  return {
    cost: route.cost,
    remaining: g.movementBudgetRemaining - route.cost,
    // The walk stops short because something fires on the way — the price is honest, the
    // destination is not what was clicked.
    truncated: route.truncated,
  };
});

/** Cells the server still reports as holding an unfound cache. */
const serverHintCells = computed(() => {
  const set = new Set<string>();
  for (const [x, y] of grid.value?.hintCells ?? []) set.add(`${x},${y}`);
  return set;
});

// Every per-frame effect in this component is gated on this — see the paint loop below.
const prefersReducedMotion = typeof window !== 'undefined'
  && typeof window.matchMedia === 'function'
  && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

// ── Reveal transition ────────────────────────────────────────────────────────────
// Searching a corner is the one moment where the room gives something back, and a cache that
// simply blinks from hollow slab to open alcove between two frames reads as a glitch. So the
// swap is held for half a beat and the dust of it is painted over the tile.
const REVEAL_MS = 500;
// The slab keeps ringing hollow until this far into the effect; past it the alcove is open
// underneath the dust. Matches the atelier's own crossover.
const REVEAL_KEY_FLIP = 0.45;

/** cellKey → rAF timestamp the effect started at; -1 until the first frame assigns one. */
const revealStarts = new Map<string, number>();
/** Cells whose sprite must still read as a hint because their effect has not crossed over. */
const revealHoldCells = ref(new Set<string>());

watch(serverHintCells, (now, before) => {
  if (prefersReducedMotion || !before) return;
  for (const cellKey of before) {
    if (now.has(cellKey) || revealStarts.has(cellKey)) continue;
    revealStarts.set(cellKey, -1);
    revealHoldCells.value = new Set(revealHoldCells.value).add(cellKey);
  }
});

/**
 * Advances every in-flight reveal and returns each one's progress, so the paint loop can both
 * flip the sprite key at the crossover and composite the dust on top.
 */
function tickReveals(timestamp: number): Map<string, number> {
  const progress = new Map<string, number>();
  if (revealStarts.size === 0) return progress;

  let held = revealHoldCells.value;
  let holdChanged = false;

  for (const [cellKey, start] of revealStarts) {
    if (start < 0) {
      revealStarts.set(cellKey, timestamp);
      progress.set(cellKey, 0);
      continue;
    }
    const t = (timestamp - start) / REVEAL_MS;
    if (t >= 1) {
      revealStarts.delete(cellKey);
      if (held.has(cellKey)) {
        if (!holdChanged) { held = new Set(held); holdChanged = true; }
        held.delete(cellKey);
      }
      continue;
    }
    progress.set(cellKey, t);
    if (t >= REVEAL_KEY_FLIP && held.has(cellKey)) {
      if (!holdChanged) { held = new Set(held); holdChanged = true; }
      held.delete(cellKey);
    }
  }

  // Reassigned rather than mutated so the draw plan recomputes — at most twice per reveal
  // (crossover, then end), not once per frame.
  if (holdChanged) revealHoldCells.value = held;
  return progress;
}

/** What the draw plan reads: the server's caches plus any whose reveal is still mid-flight. */
const hintCells = computed(() => {
  if (revealHoldCells.value.size === 0) return serverHintCells.value;
  const set = new Set(serverHintCells.value);
  for (const cellKey of revealHoldCells.value) set.add(cellKey);
  return set;
});

const projectionParams = computed(() => {
  const g = grid.value;
  return {
    canvasWidth: canvasSize.value.width,
    canvasHeight: canvasSize.value.height,
    gridWidth: g?.width ?? 0,
    gridHeight: g?.height ?? 0,
  };
});

const drawPlan = computed(() => {
  const g = grid.value;
  if (!g || canvasSize.value.width === 0 || canvasSize.value.height === 0) return [];

  return buildDrawPlan({
    cells: cells.value,
    gridWidth: g.width,
    gridHeight: g.height,
    canvasWidth: canvasSize.value.width,
    canvasHeight: canvasSize.value.height,
    ambientTint: ambientTint.value,
    theme: paintedTheme.value,
    elevation: g.elevation,
    obstacleCells: obstacleCells.value,
    isFloor,
    hintCells: hintCells.value,
    nodesByCell: nodesByCell.value,
    nodeTintFor,
    party: { x: displayPartyX.value, y: displayPartyY.value },
    reachableCells: reachableCells.value,
    pathCells: hoveredRoute.value?.path,
    hoveredCell: hoveredCell.value,
  });
});

const { getSprite, spriteAspectRatio, propAspectRatio, usesPropRect } = useTerrainSprites();

// Shared by the canvas blit loop and every DOM overlay that needs to line up with it (node
// markers) — a single source of truth for tile pixel size so nothing can drift out of sync
// with a separately-hardcoded lift constant.
//
// Painted sprites come on TWO canvas sizes with two different ground anchors: floors/party/
// highlights on the short one, walls and scenery on a much taller one (a wall silhouette rises
// well above its own tile and would be chopped flat otherwise). Blitting a wall with the floor
// anchor sinks it into the ground, so both rects are computed here and picked per sprite key.
const spriteDest = computed(() => {
  const { isoUnitX } = isoUnit(projectionParams.value);
  const destW = isoUnitX * 2.05; // slight overlap hides seams, matches the old CSS tile ratio
  return { destW, destH: destW / spriteAspectRatio, propH: destW / propAspectRatio };
});

/** How many pixels a tile at this elevation sits above ground level on screen, at the
 * canvas's current scale — matches exactly how far a terrain sprite's own top face shifts
 * for the same elevation, so a DOM overlay computing its own lift with this never drifts from
 * what's painted on canvas. Always derived from the FLOOR canvas, never the tall one. */
function elevationLiftPx(elevationLevel: number): number {
  const scale = spriteDest.value.destH / TERRAIN_SPRITE_CONSTANTS.SPRITE_H;
  return elevationLevel * TERRAIN_SPRITE_CONSTANTS.BASE_STEP_PX * scale;
}

function paintCanvas(timestamp: number) {
  const canvas = canvasEl.value;
  const g = grid.value;
  if (!canvas || !g || canvasSize.value.width === 0) return;

  if (canvas.width !== canvasSize.value.width || canvas.height !== canvasSize.value.height) {
    canvas.width = canvasSize.value.width;
    canvas.height = canvasSize.value.height;
  }

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.clearRect(0, 0, canvas.width, canvas.height);
  paintBackdrop(ctx, canvas.width, canvas.height, timestamp);

  const { destW, destH, propH } = spriteDest.value;
  const revealProgress = tickReveals(timestamp);

  // Anything painted in front of these has to let them show through, or the player loses track
  // of where they are and what they are about to click.
  const seeThroughTargets = [
    drawPlan.value.find((entry) => entry.spriteKey.kind === 'party'),
    hoveredCell.value
      ? drawPlan.value.find((e) => e.spriteKey.kind === 'highlight' && e.spriteKey.variant === 'cursor')
      : undefined,
  ].filter((entry): entry is NonNullable<typeof entry> => entry !== undefined);

  for (const entry of drawPlan.value) {
    const sprite = getSprite(entry.spriteKey);
    const dx = entry.screenX - (destW / 2);

    const occludes = seeThroughTargets.some((target) => occludesTarget(entry, target, destW, destH));
    if (occludes) ctx.globalAlpha = OCCLUDER_ALPHA;

    const key = entry.spriteKey;

    if (usesPropRect(key)) {
      // Scenery rides its tile's elevation; a wall stands ON the ground, so its own cell
      // elevation is already baked into the silhouette and it takes no extra lift.
      const lift = key.kind === 'prop' ? elevationLiftPx(entry.elevation) : 0;
      // A waiting figure breathes, and paces its own tile rather than standing to attention —
      // the sway is offset by cell so a row of them never moves in unison. Both amplitudes stay
      // well inside one tile: this reads as someone alive, not as a second movement system.
      const isFigure = key.kind === 'prop' && key.prop === 'npc';
      const bob = isFigure ? Math.sin((timestamp * 0.0016) + entry.x) * 1.6 : 0;
      const pace = isFigure
        ? Math.sin((timestamp * 0.0007) + (entry.x * 1.7) + (entry.y * 0.9)) * (destW * 0.06)
        : 0;
      const propDy = entry.screenY - (propH * PROP_GROUND_ANCHOR_RATIO) - lift;

      ctx.drawImage(sprite, dx + pace, propDy + bob, destW, propH);

      // Flame and shard are runtime effects rather than baked frames — that is what makes
      // these props read as alive rather than as furniture.
      if (key.kind === 'prop' && key.prop === 'campfire') {
        drawFireFx(ctx, dx, propDy, destW, propH, timestamp);
      }
      if (key.kind === 'prop' && key.prop === 'star') {
        drawStarFx(ctx, dx, propDy, destW, propH, timestamp);
      }
    } else {
      // The reachable wash breathes so it reads as an invitation; the cursor stays solid so
      // the cell actually under the pointer never gets lost in that breathing.
      if (key.kind === 'highlight' && key.variant === 'move') {
        ctx.globalAlpha *= 0.55 + (0.45 * Math.sin((timestamp * 0.0022) + ((entry.x + entry.y) * 0.5)));
      }
      const highlightDy = entry.screenY - (destH * GROUND_ANCHOR_RATIO);
      ctx.drawImage(sprite, dx, highlightDy, destW, destH);

      if (key.kind === 'highlight' && key.variant === 'path') {
        const last = hoveredRoute.value?.path.at(-1);
        paintPathMarker(
          ctx, dx, highlightDy, destW, destH, entry.elevation,
          last?.x === entry.x && last?.y === entry.y,
        );
      }
    }

    ctx.globalAlpha = 1;

    // Composed AFTER the tile, or the blit would paint straight over it. The tell itself is
    // baked into the sprite; only this pulse is a runtime effect, so it costs no cache variant.
    // Elevation must be passed through, otherwise the aura stays pinned at ground level and
    // rides up over the riser of a raised tile.
    if (entry.spriteKey.kind === 'floor' && (entry.spriteKey.danger ?? 'none') !== 'none') {
      drawDangerAura(
        ctx,
        dx,
        entry.screenY - (destH * GROUND_ANCHOR_RATIO),
        destW,
        destH,
        timestamp,
        entry.spriteKey.elevation,
      );
    }

    // The dust of a cache being opened, over the slab that is still ringing hollow and then
    // over the alcove underneath it — which is what carries the eye across the swap.
    if (entry.spriteKey.kind === 'floor') {
      const revealT = revealProgress.get(entry.cellKey);
      if (revealT !== undefined) {
        drawRevealFx(
          ctx,
          dx,
          entry.screenY - (destH * GROUND_ANCHOR_RATIO),
          destW,
          destH,
          revealT,
          paintedTheme.value,
          entry.spriteKey.elevation,
        );
      }
    }
  }

  paintFogOfWar(ctx, canvas.width, canvas.height, timestamp);
  drawAmbient(ctx, canvas.width, canvas.height, paintedTheme.value, timestamp * AMBIENT_TIME_SCALE);
}

/**
 * Fog of war, painted after the tiles and before the ambient particles: a veil over the whole
 * board, punched through at every cell the party has already seen. Passing all the explored
 * cells as centres of ONE call is what makes the holes melt into a single explored region
 * instead of stacking as separate discs. The holes are 2:1 ellipses, so the cleared area
 * follows the isometric grid rather than reading as a circle laid over it.
 */
function paintFogOfWar(ctx: CanvasRenderingContext2D, width: number, height: number, timestamp: number) {
  const g = grid.value;
  if (!g) return;

  const { isoUnitX } = isoUnit(projectionParams.value);
  if (isoUnitX === 0) return;

  const centers = g.revealedCells.map(([x, y]) => {
    const { screenX, screenY } = projectToScreen(x, y, projectionParams.value);
    return {
      x: screenX,
      y: screenY - elevationLiftPx(terrainHeight(x, y)),
      radius: visionRadius(1, isoUnitX),
    };
  });

  if (centers.length === 0) return;

  // The engine's veil is near-opaque (alpha .94→.97), which buried not just the unexplored
  // tiles but the painted backdrop behind them — the whole board read as black. Blitting the
  // veil at partial alpha lets the room's shape and its sky show through as a suggestion,
  // which is what the design atelier looks like. The punched holes are fully transparent in
  // the veil buffer, so scaling its alpha leaves explored ground exactly as bright as before.
  const previousAlpha = ctx.globalAlpha;
  ctx.globalAlpha = previousAlpha * FOG_VEIL_ALPHA;
  drawFogOfWar(ctx, width, height, centers, 0, paintedTheme.value, timestamp);
  ctx.globalAlpha = previousAlpha;
}

// ── Backdrop: painted, cached, theme-driven ──────────────────────────────────────
// Replaces the seven per-theme CSS gradient stacks that used to live in this component's
// <style> block. It's the single most expensive thing painted per frame and the only one whose
// content barely moves, so it's baked into an offscreen canvas and re-blitted — invalidated on
// resize or a room/theme change, never per frame.
const backdropCache = { canvas: null as HTMLCanvasElement | null, key: '' };

function paintBackdrop(ctx: CanvasRenderingContext2D, width: number, height: number, timestamp: number) {
  const key = `${width}x${height}:${paintedTheme.value}:${room.value.id}`;

  if (backdropCache.key !== key || !backdropCache.canvas) {
    const offscreen = document.createElement('canvas');
    offscreen.width = width;
    offscreen.height = height;
    const offCtx = offscreen.getContext('2d');
    if (!offCtx) {
      // jsdom and other canvas-less environments: skip the backdrop rather than crash.
      drawBackdrop(ctx, width, height, paintedTheme.value, timestamp, room.value.id, { scenery: true });
      return;
    }
    drawBackdrop(offCtx, width, height, paintedTheme.value, timestamp, room.value.id, { scenery: true });
    backdropCache.canvas = offscreen;
    backdropCache.key = key;
  }

  ctx.drawImage(backdropCache.canvas, 0, 0);
}

// ── Paint loop ───────────────────────────────────────────────────────────────────
// The ambient particle pass animates, so painting is driven by requestAnimationFrame rather
// than by a watcher on the draw plan. Under prefers-reduced-motion nothing animates, so we fall
// back to repainting only when the plan actually changes — same behaviour as before this change.
let frameHandle: number | null = null;

function renderFrame(timestamp: number) {
  paintCanvas(timestamp);
  frameHandle = requestAnimationFrame(renderFrame);
}

if (prefersReducedMotion || typeof requestAnimationFrame === 'undefined') {
  watch(drawPlan, () => paintCanvas(0), { flush: 'post' });
} else {
  onMounted(() => {
    frameHandle = requestAnimationFrame(renderFrame);
  });
  onBeforeUnmount(() => {
    if (frameHandle !== null) cancelAnimationFrame(frameHandle);
  });
}

// ── Click-to-move / hover: canvas has no per-cell DOM nodes, so hit-testing inverse-
// projects the pointer position back to a grid cell — accounting for each cell's own
// elevation lift, so an elevated tile is hit where it's actually drawn (see screenToCell). ──
function canvasPointToCell(event: MouseEvent): Cell | null {
  const canvas = canvasEl.value;
  const g = grid.value;
  if (!canvas || !g) return null;

  const rect = canvas.getBoundingClientRect();
  return screenToCell({
    screenX: event.clientX - rect.left,
    screenY: event.clientY - rect.top,
    canvasWidth: rect.width,
    canvasHeight: rect.height,
    gridWidth: g.width,
    gridHeight: g.height,
    elevation: g.elevation,
    obstacleCells: obstacleCells.value,
    isFloor,
  });
}

function onCellClick(cell: Cell) {
  if (!isRevealed(cell.x, cell.y)) return;

  if (isParty(cell.x, cell.y)) {
    // Standing on a node's cell opens the standing-node side panel — clicking the
    // cell itself is a no-op so it doesn't fight with that panel.
    return;
  }

  emit('moveRequest', cell.x, cell.y);
}

function onCanvasClick(event: MouseEvent) {
  const cell = canvasPointToCell(event);
  if (cell) onCellClick(cell);
}

// ── Hover tooltip ────────────────────────────────────────────────────────────────
// The hovered cell's own highlight is painted INTO the canvas (see the draw plan's
// 'highlight' sprite) so it hugs the tile's real diamond at its real elevation and sorts
// correctly against neighbouring tiles — `hoveredCell` itself is declared further up because
// the draw plan reads it.
const hoveredNode = ref<NodeDto | null>(null);
const mouseX = ref(0);
const mouseY = ref(0);

const tooltipStyle = computed(() => ({
  left: `${mouseX.value + 16}px`,
  top: `${mouseY.value + 16}px`,
}));

function onCanvasMouseMove(event: MouseEvent) {
  mouseX.value = event.clientX;
  mouseY.value = event.clientY;

  const cell = canvasPointToCell(event);
  hoveredCell.value = cell;
  hoveredNode.value = cell ? nodeAt(cell.x, cell.y) : null;
}

function onCanvasMouseLeave() {
  hoveredCell.value = null;
  hoveredNode.value = null;
}

const showChallengeBossBanner = computed(() =>
  Boolean(grid.value && grid.value.movementBudgetRemaining <= 0 && grid.value.canChallengeBossRemotely),
);

/** The node the party currently stands on, if any and still Available. */
const standingNode = computed<NodeDto | null>(() => {
  const g = grid.value;
  if (!g) return null;
  const node = nodeAt(g.partyX, g.partyY);
  return node && node.state === 'Available' ? node : null;
});

const COMBAT_NODE_TYPES = new Set(['Combat', 'Elite', 'Rare', 'RoomBoss', 'FinalBoss']);

const canWagerStandingNode = computed(() => {
  const node = standingNode.value;
  if (!node) return false;
  return COMBAT_NODE_TYPES.has(node.type) && node.combatRiskTier && node.combatRiskTier !== 'Fatal';
});

const standingNodeRiskDisplay = computed(() => {
  const node = standingNode.value;
  if (!node || !COMBAT_NODE_TYPES.has(node.type) || !node.combatRiskTier) return null;
  return RISK_TIER_DISPLAY[node.combatRiskTier] ?? null;
});

// ── Node side panel: opens itself when the party steps onto an available node ──────
// Opens on the side of the screen the party ISN'T standing near, so it never covers
// the ground the player is about to look at next.
const panelSide = computed<'left' | 'right'>(() => {
  const g = grid.value;
  if (!g) return 'right';
  return g.partyX < g.width / 2 ? 'right' : 'left';
});

const isPanelCollapsed = ref(false);

watch(
  () => standingNode.value?.id ?? null,
  (nodeId, previousNodeId) => {
    if (nodeId && nodeId !== previousNodeId) {
      isPanelCollapsed.value = false;
    }
  },
);

function togglePanelCollapsed() {
  isPanelCollapsed.value = !isPanelCollapsed.value;
}

// ── Top info overlay (kicker + movement budget + boss banner) ──────────────────────
// External to the map itself, so it starts folded to a small tab and only overlays
// the canvas — never pushes it down — once opened.
const isInfoCollapsed = ref(true);

function toggleInfoCollapsed() {
  isInfoCollapsed.value = !isInfoCollapsed.value;
}
</script>

<template>
  <section class="tgrid">
    <div
      v-if="grid"
      ref="canvasContainerEl"
      class="tgrid__canvas"
      :style="{ '--theme-accent': `var(${themeAccent})` }"
    >
      <canvas
        ref="canvasEl"
        class="tgrid__terrain-canvas"
        role="img"
        aria-label="Plateau d'exploration tactique"
        @click="onCanvasClick"
        @mousemove="onCanvasMouseMove"
        @mouseleave="onCanvasMouseLeave"
      />

      <div
        v-if="standingNode"
        class="tgrid__node-panel"
        :class="[`tgrid__node-panel--${panelSide}`, { 'tgrid__node-panel--collapsed': isPanelCollapsed }]"
      >
        <button
          type="button"
          class="tgrid__node-panel-toggle"
          :aria-label="isPanelCollapsed ? 'Ouvrir le panneau du nœud' : 'Réduire le panneau du nœud'"
          @click="togglePanelCollapsed"
        >
          <span v-if="panelSide === 'right'">{{ isPanelCollapsed ? '◂' : '▸' }}</span>
          <span v-else>{{ isPanelCollapsed ? '▸' : '◂' }}</span>
        </button>

        <div v-if="!isPanelCollapsed" class="tgrid__node-panel-body">
          <div class="tgrid__node-panel-header">
            <SigilIcon :kind="sigilKindFor(standingNode)" :size="22" />
            <span class="es-kicker">{{ nodeTypeLabel(standingNode) }}</span>
          </div>

          <p class="tgrid__node-panel-desc">{{ nodeTypeDescription(standingNode) }}</p>

          <div v-if="standingNodeRiskDisplay" class="tgrid__node-panel-risk">
            <span class="es-label">Danger</span>
            <span :class="['tgrid__node-panel-risk-value', standingNodeRiskDisplay.cls]">
              {{ standingNodeRiskDisplay.text }}
            </span>
          </div>

          <div class="tgrid__node-panel-actions">
            <button
              v-if="canWagerStandingNode"
              type="button"
              class="es-btn es-btn--ghost"
              @click="emit('wagerNode', standingNode.id)"
            >
              Provoquer le destin
            </button>
            <button type="button" class="es-btn" @click="emit('enterNode', standingNode.id)">
              Entrer →
            </button>
          </div>
        </div>
      </div>

      <div class="tgrid__top-tabs">
        <div
          class="tgrid__info-overlay"
          :class="{ 'tgrid__info-overlay--collapsed': isInfoCollapsed }"
        >
          <button
            type="button"
            class="tgrid__info-toggle"
            :class="{ 'tgrid__info-toggle--alert': showChallengeBossBanner }"
            :aria-label="isInfoCollapsed ? 'Afficher les informations' : 'Réduire les informations'"
            @click="toggleInfoCollapsed"
          >
            <span class="es-kicker">Exploration tactique</span>
            <span
              v-if="showChallengeBossBanner && isInfoCollapsed"
              class="tgrid__info-alert-dot"
              aria-hidden="true"
            />
            <span class="tgrid__info-chevron">{{ isInfoCollapsed ? '▾' : '▴' }}</span>
          </button>

          <div v-if="!isInfoCollapsed" class="tgrid__info-body">
            <span class="tgrid__budget">
              Déplacement <strong>{{ grid.movementBudgetRemaining }}</strong> / {{ grid.movementBudget }}
            </span>

            <button
              v-if="grid.canSearch"
              type="button"
              class="es-btn es-btn--ghost tgrid__search-btn"
              @click="emit('search')"
            >
              Fouiller les environs
            </button>

            <div v-if="showChallengeBossBanner" class="tgrid__boss-banner">
              <p class="tgrid__boss-banner-text">
                Le budget de déplacement est épuisé. Le gardien de la salle approche à grands pas.
              </p>
              <button type="button" class="es-btn es-btn--blood" @click="emit('challengeBoss')">
                Provoquer le combat de boss →
              </button>
            </div>
          </div>
        </div>

        <div class="tgrid__room-tab">
          <span class="es-kicker">{{ roomName }}</span>
        </div>

        <!-- Sits beside the room and Laws tabs and reads exactly like them, so it has to BE
             actionable: as an inert div it was the thing players clicked to search, and the
             only working control was buried inside the info panel, which starts collapsed. -->
        <button
          v-if="grid.canSearch"
          type="button"
          class="tgrid__search-tab"
          @click="emit('search')"
        >
          <span class="es-kicker">Fouiller les environs</span>
        </button>

        <button type="button" class="tgrid__laws-tab" @click="emit('toggleLaws')">
          <span class="es-kicker">Lois</span>
          <span v-if="influenceCount" class="tgrid__laws-tab-count">{{ influenceCount }}</span>
        </button>
      </div>
    </div>

    <Teleport to="body">
      <div v-if="hoveredNode || moveReadout" class="tgrid__hover-tooltip" :style="tooltipStyle">
        <span v-if="hoveredNode">{{ nodeTypeLabel(hoveredNode) }}</span>
        <span v-if="moveReadout" class="tgrid__move-readout">
          <span class="tgrid__move-readout-cost">−{{ moveReadout.cost }} PM</span>
          <span class="tgrid__move-readout-left">reste {{ moveReadout.remaining }}</span>
          <span v-if="moveReadout.truncated" class="tgrid__move-readout-warn">
            arrêt en chemin
          </span>
        </span>
      </div>
    </Teleport>
  </section>
</template>

<style scoped>
.tgrid {
  height: 100%;
  min-height: 0;
  min-width: 0;
  display: flex;
  flex-direction: column;
  padding: var(--space-3) var(--space-4);
  overflow: hidden;
}

.tgrid__budget {
  font-family: var(--font-mono);
  font-size: 0.78rem;
  color: var(--ink-3);
}

.tgrid__budget strong {
  color: var(--frost);
}

.tgrid__canvas {
  position: relative;
  flex: 1;
  min-height: 0;
  min-width: 0;
  padding: 6px;
  overflow: hidden;
  border: 1px solid color-mix(in oklch, var(--line), transparent 60%);
}

.tgrid__terrain-canvas {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
}

@keyframes tgrid-node-pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.14); }
}

@media (prefers-reduced-motion: reduce) {
  .tgrid__info-toggle--alert,
  .tgrid__search-tab {
    animation: none;
  }
}

/* ── Node side panel ─────────────────────────────────────────────────────────── */
.tgrid__node-panel {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 260px;
  max-width: 80%;
  display: flex;
  z-index: 110;
  transition: width 0.2s ease;
}

.tgrid__node-panel--right {
  right: 0;
  flex-direction: row;
}

.tgrid__node-panel--left {
  left: 0;
  flex-direction: row-reverse;
}

.tgrid__node-panel--collapsed {
  width: 26px;
}

.tgrid__node-panel-toggle {
  flex: 0 0 auto;
  width: 26px;
  border: none;
  border-left: 1px solid var(--line-soft);
  border-right: 1px solid var(--line-soft);
  cursor: pointer;
  background: color-mix(in oklch, var(--panel), var(--frost) 10%);
  color: var(--frost);
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.tgrid__node-panel-toggle:hover {
  background: color-mix(in oklch, var(--panel), var(--frost) 18%);
}

.tgrid__node-panel-body {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  background: oklch(0.22 0.04 272 / 0.92);
  border-left: 1px solid var(--line-soft);
  border-right: 1px solid var(--line-soft);
  backdrop-filter: blur(8px);
  overflow-y: auto;
}

.tgrid__node-panel-header {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--frost);
}

.tgrid__node-panel-desc {
  margin: 0;
  color: var(--ink-3);
  font-size: 0.82rem;
  line-height: 1.5;
}

.tgrid__node-panel-risk {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.tgrid__node-panel-risk-value {
  font-family: var(--font);
  font-size: 0.9rem;
}

.tgrid__risk--low { color: var(--ink-3); }
.tgrid__risk--moderate { color: var(--gold-dim); }
.tgrid__risk--high { color: var(--blood-dim); }
.tgrid__risk--critical { color: var(--blood); }
.tgrid__risk--fatal { color: var(--blood); font-weight: 600; }

.tgrid__node-panel-actions {
  margin-top: auto;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.tgrid__node-panel-actions .es-btn {
  width: 100%;
  justify-content: center;
}

/* ── Hover tooltip ────────────────────────────────────────────────────────────── */
.tgrid__hover-tooltip {
  position: fixed;
  z-index: var(--z-tooltip, 1000);
  padding: 4px 10px;
  font-family: var(--font-caps);
  font-size: 0.7rem;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--ink);
  background: oklch(0.18 0.03 272 / 0.95);
  border: 1px solid var(--line-soft);
  border-radius: 3px;
  pointer-events: none;
  white-space: nowrap;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

/* The price of the move under the pointer. Sits under the node label when there is one, so a
   cell that is both an objective and a walk reads as one thing, not two tooltips fighting. */
.tgrid__move-readout {
  display: flex;
  align-items: baseline;
  gap: var(--space-2);
}

.tgrid__move-readout-cost {
  color: var(--gold);
}

.tgrid__move-readout-left {
  color: var(--ink-3);
  text-transform: none;
  letter-spacing: 0;
}

.tgrid__move-readout-warn {
  color: var(--blood);
}

/* ── Top tabs row: info overlay, room name, always-visible Lois button ──────────── */
.tgrid__top-tabs {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 120;
  display: flex;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 3px;
  max-width: calc(100% - 2 * var(--space-3));
}

.tgrid__info-overlay {
  position: relative;
  min-width: 0;
}

.tgrid__info-toggle,
.tgrid__room-tab,
.tgrid__laws-tab {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  border: 1px solid var(--line-soft);
  border-top: none;
  border-radius: 0 0 4px 4px;
  padding: 4px 12px;
  background: oklch(0.20 0.04 272 / 0.9);
  color: var(--ink-3);
}

.tgrid__info-toggle,
.tgrid__laws-tab {
  cursor: pointer;
}

.tgrid__info-toggle:hover,
.tgrid__laws-tab:hover {
  background: oklch(0.20 0.04 272 / 1);
}

.tgrid__info-toggle--alert {
  animation: tgrid-info-alert-glow 1.6s ease-in-out infinite;
}

@keyframes tgrid-info-alert-glow {
  0%, 100% { box-shadow: 0 0 0 0 color-mix(in oklch, var(--blood), transparent 60%); }
  50% { box-shadow: 0 0 10px 3px color-mix(in oklch, var(--blood), transparent 20%); }
}

.tgrid__search-btn {
  align-self: flex-start;
}

/* A tell that a search is worth trying here — never what, never exactly where. */
.tgrid__search-tab {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  border: 1px solid color-mix(in oklch, var(--gold), transparent 40%);
  border-top: none;
  border-radius: 0 0 4px 4px;
  padding: 4px 12px;
  background: oklch(0.20 0.04 272 / 0.9);
  color: var(--gold);
  cursor: pointer;
  animation: tgrid-node-pulse 2.4s ease-in-out infinite;
}

.tgrid__room-tab {
  max-width: 220px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tgrid__laws-tab-count {
  font-family: var(--font-mono);
  font-size: 0.68rem;
  color: var(--gold);
  background: oklch(0.30 0.06 85 / 0.3);
  border-radius: 10px;
  padding: 1px 6px;
}

.tgrid__info-alert-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--blood);
  animation: tgrid-node-pulse 1.4s ease-in-out infinite;
}

.tgrid__info-chevron {
  font-size: 0.7rem;
  color: var(--ink-4);
}

.tgrid__info-body {
  /* Absolute + anchored under the toggle, out of normal flow — expanding it must
     overlay the canvas, not widen .tgrid__info-overlay and push the room/laws tabs
     sideways the way it did when this sat in flow. */
  position: absolute;
  top: 100%;
  left: 0;
  z-index: 6;
  width: max-content;
  max-width: 280px;
  padding: var(--space-2) var(--space-4) var(--space-3);
  background: oklch(0.20 0.04 272 / 0.92);
  backdrop-filter: blur(8px);
  border: 1px solid var(--line-soft);
  border-top: none;
  border-radius: 0 0 6px 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.tgrid__boss-banner {
  padding: var(--space-3) var(--space-4);
  border: 1px solid var(--blood-dim, var(--blood));
  border-radius: 4px;
  background: color-mix(in oklch, var(--panel), var(--blood) 10%);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  flex-wrap: wrap;
}

.tgrid__boss-banner-text {
  margin: 0;
  color: var(--ink-2);
  font-size: 0.85rem;
}
</style>
