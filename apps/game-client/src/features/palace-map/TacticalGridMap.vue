<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';

import SigilIcon from '../../shared/components/SigilIcon.vue';
import type { NodeDto, RoomDto } from '../runs/types/runTypes';
import { usePalaceTerrain } from './composables/usePalaceTerrain';
import { usePartyTokenPath } from './composables/usePartyTokenPath';
import { useNodePresentation, NODE_TILE_TONE, RISK_TIER_DISPLAY } from './composables/useNodePresentation';
import { useRoomBackdropTheme } from './composables/useRoomBackdropTheme';
import { useGridCells, type Cell } from './composables/useGridCells';
import { useTerrainSprites, GROUND_ANCHOR_RATIO, type FloorTint } from './composables/useTerrainSprites';
import { buildDrawPlan, isoUnit, projectToScreen, unprojectFromScreen } from './composables/useTerrainDrawPlan';

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
  toggleLaws: [];
}>();

// Same display name logic as GameTopBar's "Salle" segment — shown here instead in
// Tactical mode (see the room-name tab next to "Exploration tactique").
const roomName = computed(() =>
  props.room.catalogName || props.room.theme || props.room.roomType || '—');

const room = computed(() => props.room);
const grid = computed(() => props.room.grid ?? null);

const { isRevealed, nodeAt, isParty, cells, obstacleCells, nodesByCell } =
  useGridCells(room, grid);

const { displayPartyX, displayPartyY } = usePartyTokenPath(room, grid);

const { terrainHeight } = usePalaceTerrain(room, grid);

// ── Canvas terrain renderer ──────────────────────────────────────────────────────
// The grid/fog/party overlays used to be individual absolutely-positioned DOM `<button>`s
// per cell, projected via a CSS-percent isometric formula. That's replaced here by a single
// `<canvas>` painting procedurally-generated tile sprites (see useTerrainSprites.ts) at real
// pixel positions (see useTerrainDrawPlan.ts) — node icons and the party token stay DOM
// overlays layered on top (SigilIcon is SVG, no reason to duplicate it in canvas; their
// animation/transition needs don't fit a redraw-per-frame model either).
const canvasContainerEl = ref<HTMLElement | null>(null);
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

const { backdropClass, roomNuance, themeAccent } = useRoomBackdropTheme(room);
const { nodeTileToneClass, sigilKindFor, nodeTypeLabel, nodeTypeDescription } = useNodePresentation();

const TONE_VALUES = new Set(['blood', 'gold', 'frost', 'sap']);

const ambientTint = computed<FloorTint>(() => {
  const accent = themeAccent.value.replace('--', '');
  return (TONE_VALUES.has(accent) ? accent : 'neutral') as FloorTint;
});

function nodeTintFor(node: NodeDto): FloorTint {
  const tone = NODE_TILE_TONE[node.type];
  return (tone && TONE_VALUES.has(tone) ? tone : 'neutral') as FloorTint;
}

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
    elevation: g.elevation,
    obstacleCells: obstacleCells.value,
    nodesByCell: nodesByCell.value,
    nodeTintFor,
  });
});

const { getSprite, spriteAspectRatio } = useTerrainSprites();

function paintCanvas() {
  const canvas = canvasEl.value;
  const g = grid.value;
  if (!canvas || !g || canvasSize.value.width === 0) return;

  canvas.width = canvasSize.value.width;
  canvas.height = canvasSize.value.height;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  ctx.clearRect(0, 0, canvas.width, canvas.height);

  const { isoUnitX } = isoUnit(projectionParams.value);
  const destW = isoUnitX * 2.05; // slight overlap hides seams, matches the old CSS tile ratio
  const destH = destW / spriteAspectRatio;

  for (const entry of drawPlan.value) {
    const sprite = getSprite(entry.spriteKey);
    ctx.drawImage(
      sprite,
      entry.screenX - (destW / 2),
      entry.screenY - (destH * GROUND_ANCHOR_RATIO),
      destW,
      destH,
    );
  }
}

watch(drawPlan, paintCanvas, { flush: 'post' });

// ── Node markers + party token: DOM overlays positioned via the same pixel projection ──
const TERRAIN_LIFT_PX = 8;

const nodeMarkers = computed(() => {
  const g = grid.value;
  // Deliberately NOT gated on canvasSize being measured (unlike drawPlan): a marker's
  // existence only depends on grid data, not on pixel geometry. Positions are degenerate
  // (all stacked at the projection's origin) until the ResizeObserver first fires, same as
  // any other layout-dependent element on first paint — and it keeps this list testable
  // without a real ResizeObserver (e.g. under jsdom).
  if (!g) return [];

  const markers: { node: NodeDto; screenX: number; screenY: number }[] = [];

  for (const cell of cells.value) {
    const node = nodeAt(cell.x, cell.y);
    if (!node) continue;

    const { screenX, screenY } = projectToScreen(cell.x, cell.y, projectionParams.value);
    const lift = terrainHeight(cell.x, cell.y) * TERRAIN_LIFT_PX;
    markers.push({ node, screenX, screenY: screenY - lift });
  }

  return markers;
});

const partyMarkerStyle = computed(() => {
  const g = grid.value;
  if (!g) return { display: 'none' };

  const { screenX, screenY } = projectToScreen(displayPartyX.value, displayPartyY.value, projectionParams.value);
  const lift = terrainHeight(displayPartyX.value, displayPartyY.value) * TERRAIN_LIFT_PX;

  return {
    left: `${screenX}px`,
    top: `${screenY - lift}px`,
  };
});

// ── Click-to-move / hover: canvas has no per-cell DOM nodes, so hit-testing inverse-
// projects the pointer position back to a grid cell. ──────────────────────────────
function canvasPointToCell(event: MouseEvent): Cell | null {
  const canvas = canvasEl.value;
  const g = grid.value;
  if (!canvas || !g) return null;

  const rect = canvas.getBoundingClientRect();
  const { x, y } = unprojectFromScreen(
    event.clientX - rect.left,
    event.clientY - rect.top,
    { canvasWidth: rect.width, canvasHeight: rect.height, gridWidth: g.width, gridHeight: g.height },
  );

  if (x < 0 || x >= g.width || y < 0 || y >= g.height) return null;
  return { x, y };
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

// ── Hover tooltip: shows just the node type, following the cursor ──────────────────
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
  hoveredNode.value = cell ? nodeAt(cell.x, cell.y) : null;
}

function onCanvasMouseLeave() {
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
      <div
        class="tgrid__backdrop"
        :class="backdropClass"
        :style="{ '--room-nuance': roomNuance }"
        aria-hidden="true"
      />

      <canvas
        ref="canvasEl"
        class="tgrid__terrain-canvas"
        role="img"
        aria-label="Plateau d'exploration tactique"
        @click="onCanvasClick"
        @mousemove="onCanvasMouseMove"
        @mouseleave="onCanvasMouseLeave"
      />

      <SigilIcon
        v-for="marker in nodeMarkers"
        :key="marker.node.id"
        class="tgrid__node-icon"
        :class="{
          'tgrid__node-icon--resolved': marker.node.state === 'Resolved',
          'tgrid__node-icon--boss': marker.node.isBoss,
        }"
        :style="{ left: `${marker.screenX}px`, top: `${marker.screenY}px` }"
        :kind="sigilKindFor(marker.node)"
        :size="20"
      />

      <div class="tgrid__party" :style="partyMarkerStyle" aria-hidden="true">
        <span class="tgrid__party-token" />
      </div>

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

        <button type="button" class="tgrid__laws-tab" @click="emit('toggleLaws')">
          <span class="es-kicker">Lois</span>
          <span v-if="influenceCount" class="tgrid__laws-tab-count">{{ influenceCount }}</span>
        </button>
      </div>
    </div>

    <Teleport to="body">
      <div v-if="hoveredNode" class="tgrid__hover-tooltip" :style="tooltipStyle">
        {{ nodeTypeLabel(hoveredNode) }}
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

/* ── Room backdrop: thematic, coherent within a theme, lightly nuanced per room ─────
   Pure CSS (no image assets, no 3D library) — layered gradients per theme, with a
   small deterministic hue drift (--room-nuance) so two rooms sharing a theme still
   read as the same place while not being pixel-identical. Sits behind the terrain
   canvas, visible through/around the painted diamonds. */
.tgrid__backdrop {
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
  filter: hue-rotate(calc((var(--room-nuance, 50) - 50) * 0.3deg));
}

.tgrid__backdrop--default {
  background:
    radial-gradient(circle at 20% 50%, var(--wash-gold), transparent 25%),
    radial-gradient(circle at 88% 50%, var(--wash-blood), transparent 20%),
    var(--void);
}

.tgrid__backdrop--threshold {
  /* Seuil — porte liminale : colonne de lumière verticale, vignette brumeuse. */
  background:
    linear-gradient(90deg, transparent 42%, color-mix(in oklch, var(--frost), transparent 82%) 50%, transparent 58%),
    radial-gradient(ellipse at 50% 100%, color-mix(in oklch, var(--void), black 20%), transparent 70%),
    var(--void);
}

.tgrid__backdrop--memory {
  /* Mémoire — sépia chaud, fines ondulations horizontales façon page tournée. */
  background:
    repeating-linear-gradient(0deg, transparent 0 22px, color-mix(in oklch, var(--gold), transparent 92%) 22px 23px),
    radial-gradient(circle at 30% 20%, color-mix(in oklch, var(--gold), transparent 85%), transparent 60%),
    color-mix(in oklch, var(--void), var(--gold) 4%);
}

.tgrid__backdrop--forest {
  /* Forêt — vert profond, troncs verticaux répétés. */
  background:
    repeating-linear-gradient(90deg, color-mix(in oklch, var(--void), var(--sap) 6%) 0 26px, color-mix(in oklch, var(--void), black 15%) 26px 30px),
    radial-gradient(circle at 50% 0%, color-mix(in oklch, var(--sap), transparent 80%), transparent 65%),
    color-mix(in oklch, var(--void), var(--sap) 5%);
}

.tgrid__backdrop--rupture {
  /* Rupture — fractures anguleuses, croisées, teintées de sang. */
  background:
    repeating-linear-gradient(35deg, transparent 0 40px, color-mix(in oklch, var(--blood), transparent 88%) 40px 42px),
    repeating-linear-gradient(-35deg, transparent 0 55px, color-mix(in oklch, var(--blood), transparent 90%) 55px 57px),
    color-mix(in oklch, var(--void), var(--blood) 6%);
}

.tgrid__backdrop--silence {
  /* Silence — pâle, immobile, ondes concentriques comme une eau stagnante. */
  background:
    radial-gradient(circle at 50% 50%,
      transparent 0 18%, color-mix(in oklch, var(--frost), transparent 92%) 18% 19%,
      transparent 19% 34%, color-mix(in oklch, var(--frost), transparent 94%) 34% 35%,
      transparent 35%),
    color-mix(in oklch, var(--void), var(--frost) 4%);
}

.tgrid__backdrop--antechamber {
  /* Antichambre — colonnade dorée, formelle. */
  background:
    repeating-linear-gradient(90deg, color-mix(in oklch, var(--gold), transparent 90%) 0 3px, transparent 3px 64px),
    radial-gradient(ellipse at 50% -10%, color-mix(in oklch, var(--gold), transparent 82%), transparent 55%),
    color-mix(in oklch, var(--void), var(--gold) 3%);
}

.tgrid__backdrop--final {
  /* Confrontation finale — pulsation sombre et sanglante. */
  background:
    radial-gradient(circle at 50% 50%, color-mix(in oklch, var(--blood), transparent 55%), transparent 60%),
    var(--void);
  animation: tgrid-backdrop-pulse 6s ease-in-out infinite;
}

@keyframes tgrid-backdrop-pulse {
  0%, 100% { opacity: 0.75; }
  50% { opacity: 1; }
}

@keyframes tgrid-node-pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.14); }
}

.tgrid__node-icon {
  position: absolute;
  transform: translate(-50%, -50%);
  color: var(--frost);
  pointer-events: none;
  z-index: 50;
  animation: tgrid-node-pulse 1.8s ease-in-out infinite;
}

.tgrid__node-icon--resolved {
  /* Spent node — static (no pulse) and clearly desaturated. */
  animation: none;
  opacity: 0.35;
  filter: grayscale(0.9);
}

.tgrid__node-icon--boss {
  color: var(--blood);
}

@media (prefers-reduced-motion: reduce) {
  .tgrid__node-icon,
  .tgrid__info-toggle--alert,
  .tgrid__backdrop--final {
    animation: none;
  }

  .tgrid__party {
    transition: none;
  }
}

.tgrid__party {
  position: absolute;
  width: 0;
  height: 0;
  /* Matches PARTY_STEP_MS in usePartyTokenPath: one cell-to-cell glide per animation
     step, so consecutive steps hand off smoothly instead of interrupting a longer
     transition mid-flight. */
  transition: left 0.15s ease, top 0.15s ease;
  pointer-events: none;
  z-index: 100;
}

.tgrid__party-token {
  position: absolute;
  width: 1rem;
  height: 1rem;
  border-radius: 50%;
  background: radial-gradient(circle at 35% 30%, var(--gold-hi, var(--gold)), var(--gold) 70%);
  border: 1.5px solid color-mix(in oklch, white, transparent 30%);
  box-shadow:
    0 0 12px 3px color-mix(in oklch, var(--gold), transparent 25%),
    0 3px 4px oklch(0 0 0 / 0.4);
  transform: translate(-50%, -50%);
}

.tgrid__party-token::after {
  /* Ground ring under the token — anchors it visually to its tile like an FFT unit marker. */
  content: '';
  position: absolute;
  left: 50%;
  bottom: -6px;
  width: 1.3rem;
  height: 0.4rem;
  border-radius: 50%;
  border: 1.5px solid color-mix(in oklch, var(--gold), transparent 35%);
  transform: translateX(-50%);
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
