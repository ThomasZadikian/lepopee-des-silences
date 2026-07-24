<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';

import SigilIcon from '../../shared/components/SigilIcon.vue';
import type { NodeDto, RoomDto } from '../runs/types/runTypes';
import { hashSeed, usePalaceTerrain } from './composables/usePalaceTerrain';
import { usePartyTokenPath } from './composables/usePartyTokenPath';
import { useNodePresentation, RISK_TIER_DISPLAY } from './composables/useNodePresentation';
import { useRoomBackdropTheme } from './composables/useRoomBackdropTheme';
import { useGridCells, type Cell } from './composables/useGridCells';

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

const { isRevealed, nodeAt, isParty, cells } = useGridCells(room, grid);

const { displayPartyX, displayPartyY } = usePartyTokenPath(room, grid);

const { terrainHeight } = usePalaceTerrain(room, grid);

// ── Fake isometry (2.5D projection) ─────────────────────────────────────────────
// No 3D library: cells are laid out with plain absolute positioning, projected onto
// a diamond via the classic isometric formula, instead of a literal CSS Grid. Height
// then reads as a per-cell vertical offset (translateY) layered on top of that.
//
// Positions are expressed in % of the canvas, but a % point isn't the same number of
// pixels on both axes unless the canvas happens to be square — and the game board
// never is (it's the full remaining viewport). Left uncorrected, the diamond comes
// out squashed flat. canvasSize (measured live via ResizeObserver) lets the vertical
// unit compensate for the canvas's actual aspect ratio so tiles read as a true ~2:1
// isometric diamond regardless of window size. Falls back to a fixed 0.5 ratio when
// unmeasured (first paint, or jsdom in tests, which has no ResizeObserver).
const canvasEl = ref<HTMLElement | null>(null);
const canvasSize = ref({ width: 0, height: 0 });
let canvasObserver: ResizeObserver | null = null;

watch(canvasEl, (el) => {
  canvasObserver?.disconnect();
  canvasObserver = null;
  if (!el || typeof ResizeObserver === 'undefined') return;
  canvasObserver = new ResizeObserver(([entry]) => {
    canvasSize.value = { width: entry.contentRect.width, height: entry.contentRect.height };
  });
  canvasObserver.observe(el);
});

onBeforeUnmount(() => canvasObserver?.disconnect());

// Shrinks the whole diamond inward so that even the outermost tile's own half-width/
// half-height never reaches the canvas edge — without this, corner tiles clip against
// (or visually touch) the canvas border on wide/narrow windows.
const ISO_FIT = 0.82;
// The board's vertical center sits a bit below the canvas's true center: the top-tabs
// chrome (kicker/room name/Lois button) overlays the top-left corner, so leaving more
// headroom there than at the bottom keeps the diamond clear of it.
const ISO_V_CENTER = 56;

const ISO_UNIT_X = computed(() => {
  const g = grid.value;
  if (!g) return 0;
  return (100 / (g.width + g.height)) * ISO_FIT;
});
const ISO_ASPECT_CORRECTION = computed(() => {
  const { width, height } = canvasSize.value;
  return width > 0 && height > 0 ? width / height : 1;
});
const ISO_UNIT_Y = computed(() => (ISO_UNIT_X.value / 2) * ISO_ASPECT_CORRECTION.value);
const ISO_TILE_W = computed(() => ISO_UNIT_X.value * 2.05); // slight overlap hides seams
const ISO_TILE_H = computed(() => ISO_UNIT_Y.value * 2.05);

function isoLeft(x: number, y: number): number {
  // Mirrored from the first pass (x increasing now sweeps toward the bottom-left,
  // y toward the bottom-right) — flipped per feedback that the original diagonal
  // direction read wrong.
  return 50 - (x - y) * ISO_UNIT_X.value;
}

function isoTop(x: number, y: number): number {
  const g = grid.value;
  if (!g) return ISO_V_CENTER;
  const maxSpan = g.width - 1 + (g.height - 1);
  return ISO_V_CENTER + (x + y - maxSpan / 2) * ISO_UNIT_Y.value;
}

function cellStyle(cell: Cell) {
  // Per-cell jitter for the fog cloud layout (see .tgrid__cell--fog::before) — without
  // it, every fog tile draws the exact same gradient positions and the mist reads as
  // a repeating scalloped texture instead of an irregular cloud.
  const fogSeed = hashSeed(`${props.room.id}:fog:${cell.x}:${cell.y}`);
  const fogSeed2 = hashSeed(`${props.room.id}:fog2:${cell.x}:${cell.y}`);
  return {
    left: `${isoLeft(cell.x, cell.y)}%`,
    top: `${isoTop(cell.x, cell.y)}%`,
    width: `${ISO_TILE_W.value}%`,
    height: `${ISO_TILE_H.value}%`,
    zIndex: String(cell.x + cell.y),
    '--terrain-height': terrainHeight(cell.x, cell.y),
    '--fog-jx': (fogSeed % 41) - 20,
    '--fog-jy': (Math.floor(fogSeed / 41) % 41) - 20,
    // Varies each cloud's own size, not just its position — sub-tile position jitter
    // alone still reads as a regular texture at the tile-pitch frequency; letting
    // neighboring clouds be genuinely bigger/smaller breaks that rhythm.
    '--fog-scale': 0.75 + (fogSeed2 % 100) / 100 * 0.65,
  };
}

const partyStyle = computed(() => {
  if (!grid.value) return {};

  return {
    left: `${isoLeft(displayPartyX.value, displayPartyY.value)}%`,
    top: `${isoTop(displayPartyX.value, displayPartyY.value)}%`,
    '--terrain-height': terrainHeight(displayPartyX.value, displayPartyY.value),
  };
});

const { backdropClass, roomNuance, themeAccent } = useRoomBackdropTheme(room);
const { nodeTileToneClass, sigilKindFor, nodeTypeLabel, nodeTypeDescription } = useNodePresentation();

function cellClass(cell: Cell) {
  const revealed = isRevealed(cell.x, cell.y);
  const node = nodeAt(cell.x, cell.y);
  // Available nodes are sent by the backend regardless of fog (see RoomDto.FromDomain),
  // so a node can appear on an otherwise-unexplored cell — shown as a dimmed marker
  // (tgrid__cell--fog-marker) rather than the fully-lit look of a revealed cell.
  const fogMarker = !revealed && Boolean(node);

  return {
    'tgrid__cell--revealed': revealed,
    'tgrid__cell--fog': !revealed,
    'tgrid__cell--fog-marker': fogMarker,
    'tgrid__cell--node': Boolean(node),
    'tgrid__cell--boss-node': node?.isBoss ?? false,
    'tgrid__cell--resolved-node': node?.state === 'Resolved',
    'tgrid__cell--party': isParty(cell.x, cell.y),
  };
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

// ── Hover tooltip: shows just the node type, following the cursor ──────────────────
const hoveredNode = ref<NodeDto | null>(null);
const mouseX = ref(0);
const mouseY = ref(0);

const tooltipStyle = computed(() => ({
  left: `${mouseX.value + 16}px`,
  top: `${mouseY.value + 16}px`,
}));

function onCellMouseEnter(cell: Cell) {
  hoveredNode.value = nodeAt(cell.x, cell.y);
}

function onCellMouseMove(event: MouseEvent) {
  mouseX.value = event.clientX;
  mouseY.value = event.clientY;
}

function onCellMouseLeave() {
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
      ref="canvasEl"
      class="tgrid__canvas"
      :style="{ '--theme-accent': `var(${themeAccent})` }"
    >
      <div
        class="tgrid__backdrop"
        :class="backdropClass"
        :style="{ '--room-nuance': roomNuance }"
        aria-hidden="true"
      />

      <button
        v-for="cell in cells"
        :key="`${cell.x}-${cell.y}`"
        type="button"
        class="tgrid__cell"
        :class="[cellClass(cell), nodeTileToneClass(nodeAt(cell.x, cell.y))]"
        :style="cellStyle(cell)"
        :aria-disabled="!isRevealed(cell.x, cell.y)"
        :aria-label="`Case ${cell.x},${cell.y}`"
        @click="onCellClick(cell)"
        @mouseenter="onCellMouseEnter(cell)"
        @mousemove="onCellMouseMove"
        @mouseleave="onCellMouseLeave"
      >
        <SigilIcon
          v-if="nodeAt(cell.x, cell.y)"
          class="tgrid__node-icon"
          :class="{
            'tgrid__node-icon--ghost': !isRevealed(cell.x, cell.y),
            'tgrid__node-icon--resolved': nodeAt(cell.x, cell.y)!.state === 'Resolved',
          }"
          :kind="sigilKindFor(nodeAt(cell.x, cell.y)!)"
          :size="20"
        />
      </button>

      <div class="tgrid__party" :style="partyStyle" aria-hidden="true">
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

/* ── Room backdrop: thematic, coherent within a theme, lightly nuanced per room ─────
   Pure CSS (no image assets, no 3D library) — layered gradients per theme, with a
   small deterministic hue drift (--room-nuance) so two rooms sharing a theme still
   read as the same place while not being pixel-identical. */
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

.tgrid__cell {
  position: absolute;
  min-height: 0;
  min-width: 0;
  display: grid;
  place-items: center;
  border: none;
  border-radius: 2px;
  padding: 0;
  cursor: pointer;
  clip-path: polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%);
  transform: translate(-50%, calc(-50% - var(--terrain-height, 0) * 8px));
  transition: filter 0.15s ease, transform 0.15s ease;
}

.tgrid__cell[aria-disabled='true'] {
  cursor: default;
}

.tgrid__cell--fog {
  /* Real mist, not a flat dark diamond: unclipped, blurred, drifting cloud layered
     in ::before — a hard-edged tinted shape read as "a bad gradient", not fog. Height
     is also suppressed here (transform below drops the terrain-height offset) since
     an unrevealed tile shouldn't hint at its elevation before being seen. */
  clip-path: none;
  background: transparent;
  transform: translate(-50%, -50%);
}

.tgrid__cell--fog::before {
  content: '';
  position: absolute;
  inset: -55%;
  border-radius: 100%;
  /* Purely decorative — never intercepts clicks. The overshoot (inset: -55%) means
     this box extends well past its own fog tile and over neighboring cells; without
     pointer-events: none, that overshoot silently ate clicks meant for an adjacent
     available tile whenever the mist drifted over it. */
  pointer-events: none;
  /* Each blob's center is nudged by the cell's own --fog-jx/--fog-jy (set in
     cellStyle()) so neighboring tiles don't all draw the same cloud shape — without
     that, the mist reads as an obviously repeating scalloped texture. */
  background:
    radial-gradient(circle at calc(15% + var(--fog-jx, 0) * 0.7%) calc(34% + var(--fog-jy, 0) * 0.7%), color-mix(in oklch, var(--ink-3), transparent 35%), transparent 55%),
    radial-gradient(circle at calc(60% - var(--fog-jy, 0) * 0.8%) calc(26% + var(--fog-jx, 0) * 0.6%), color-mix(in oklch, var(--ink-4), transparent 42%), transparent 58%),
    radial-gradient(circle at calc(30% + var(--fog-jy, 0) * 0.6%) calc(74% - var(--fog-jx, 0) * 0.8%), color-mix(in oklch, var(--ink-5), transparent 32%), transparent 62%),
    color-mix(in oklch, var(--void), black 42%);
  filter: blur(15px);
  opacity: 0.2;
  animation: tgrid-fog-drift 18s ease-in-out infinite;
  animation-delay: calc(var(--fog-jx, 0) * -0.15s);
  transform: scale(var(--fog-scale, 1));
}

.tgrid__cell--fog-marker::before {
  /* Known objective through the fog — a thinner mist so the ghost icon underneath
     still reads, but still clearly distinct from a revealed cell. */
  opacity: 0.1;
}

@keyframes tgrid-fog-drift {
  0%, 100% { transform: translate(0, 0) scale(var(--fog-scale, 1)); }
  33% { transform: translate(8%, -8%) scale(calc(var(--fog-scale, 1) * 1.04)); }
  66% { transform: translate(-12%, 8%) scale(calc(var(--fog-scale, 1) * 1.02)); }
}

.tgrid__cell--revealed {
  /* Lightly tinted by the room's theme accent (a tile "belongs" to its room without
     drowning out node tiles below), and lit — not shadowed — by height: a taller tile
     catches more light, which reads more legibly as elevation than darkening did.
     --tile-tint/--tile-tint-pct/--tile-edge are the knobs every variant below
     overrides, so the paint formula itself lives in one place. Node tiles push
     --tile-tint-pct much higher and add a bright --tile-edge border on top of this
     same base — that gap is deliberate: a node must always read as a clear focal
     point, even in a room whose ambient theme shares its tone (e.g. a Combat node's
     blood tint on a Rupture room, which is itself blood-tinted). */
  --tile-tint: var(--theme-accent, var(--gold));
  --tile-tint-pct: calc(14% + var(--terrain-height, 0) * 6%);
  --tile-edge: color-mix(in oklch, var(--tile-tint), var(--line-strong, var(--line)) 55%);
  background: color-mix(in oklch, var(--panel), var(--tile-tint) var(--tile-tint-pct));
  /* A flat (height 0) tile still needs to read as a discrete block, not melt into its
     neighbors — so the border/top-highlight/drop-shadow all have a real baseline
     here, with height only adding on top of that rather than being the sole source
     of definition. The drop shadow in particular scales hard with height (5px→17px
     blur) so a raised tile visibly casts onto the ground below it, not just sits a
     few pixels higher. */
  box-shadow:
    inset 0 0 0 1.5px var(--tile-edge),
    inset 0 1px 0 color-mix(in oklch, white, transparent 68%),
    0 calc(3px + var(--terrain-height, 0) * 4px) calc(5px + var(--terrain-height, 0) * 4px)
      oklch(0 0 0 / calc(0.24 + var(--terrain-height, 0) * 0.13));
}

.tgrid__cell--revealed::after {
  /* The "riser": a darker twin of the same diamond, left at ground level while the
     tile above it lifts by --terrain-height — reads as the block's shaded side face
     peeking out beneath it, the classic isometric "this tile is standing on a
     pedestal" cue. Invisible at height 0 (0 offset = perfectly hidden behind its own
     tile, and opacity is 0 there too, so flat ground never shows a stray outline). */
  content: '';
  position: absolute;
  inset: 0;
  clip-path: polygon(50% 0%, 100% 50%, 50% 100%, 0% 50%);
  background: color-mix(in oklch, var(--tile-tint, var(--gold)), black 55%);
  transform: translateY(calc(var(--terrain-height, 0) * 8px));
  opacity: calc(var(--terrain-height, 0) * 0.3);
  z-index: -1;
  pointer-events: none;
}

.tgrid__cell--revealed:not([aria-disabled='true']):hover {
  filter: brightness(1.18) saturate(1.15);
  transform: translate(-50%, calc(-50% - var(--terrain-height, 0) * 8px)) scale(1.08);
}

.tgrid__cell--node.tgrid__cell--revealed {
  /* Fallback for any node type not covered by a --tone-* class below. */
  --tile-tint: var(--frost);
  --tile-tint-pct: calc(55% + var(--terrain-height, 0) * 4%);
  --tile-edge: color-mix(in oklch, var(--frost), white 30%);
}

.tgrid__cell--tone-blood.tgrid__cell--revealed { --tile-tint: var(--blood); --tile-tint-pct: calc(55% + var(--terrain-height, 0) * 4%); --tile-edge: color-mix(in oklch, var(--blood), white 30%); }
.tgrid__cell--tone-gold.tgrid__cell--revealed  { --tile-tint: var(--gold);  --tile-tint-pct: calc(55% + var(--terrain-height, 0) * 4%); --tile-edge: color-mix(in oklch, var(--gold), white 30%); }
.tgrid__cell--tone-frost.tgrid__cell--revealed { --tile-tint: var(--frost); --tile-tint-pct: calc(55% + var(--terrain-height, 0) * 4%); --tile-edge: color-mix(in oklch, var(--frost), white 30%); }
.tgrid__cell--tone-sap.tgrid__cell--revealed   { --tile-tint: var(--sap);   --tile-tint-pct: calc(55% + var(--terrain-height, 0) * 4%); --tile-edge: color-mix(in oklch, var(--sap), white 30%); }

.tgrid__cell--tone-blood.tgrid__cell--revealed,
.tgrid__cell--tone-gold.tgrid__cell--revealed,
.tgrid__cell--tone-frost.tgrid__cell--revealed,
.tgrid__cell--tone-sap.tgrid__cell--revealed,
.tgrid__cell--node.tgrid__cell--revealed {
  box-shadow:
    inset 0 0 0 2px var(--tile-edge),
    inset 0 1px 0 color-mix(in oklch, white, transparent 65%),
    0 calc(3px + var(--terrain-height, 0) * 4px) calc(6px + var(--terrain-height, 0) * 4px)
      color-mix(in oklch, var(--tile-tint), transparent 65%);
}

.tgrid__cell--boss-node.tgrid__cell--revealed {
  --tile-tint: var(--blood);
  --tile-tint-pct: calc(68% + var(--terrain-height, 0) * 4%);
  --tile-edge: color-mix(in oklch, var(--blood), white 40%);
  box-shadow:
    inset 0 0 0 2px var(--tile-edge),
    inset 0 1px 0 color-mix(in oklch, white, transparent 60%),
    0 0 16px 3px color-mix(in oklch, var(--blood), transparent 40%);
}

.tgrid__cell--resolved-node.tgrid__cell--revealed {
  /* Spent node — visibly greyed out so the player never mistakes it for something
     still worth visiting. Overrides the node/boss tints above via class order. */
  background: color-mix(in oklch, var(--panel), black 35%);
  box-shadow: inset 0 0 0 1px color-mix(in oklch, var(--line), transparent 70%);
}

@keyframes tgrid-node-pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.14); }
}

.tgrid__node-icon {
  color: var(--frost);
  animation: tgrid-node-pulse 1.8s ease-in-out infinite;
}

.tgrid__node-icon--ghost {
  opacity: 0.6;
  filter: grayscale(0.35);
}

.tgrid__node-icon--resolved {
  /* Spent node — static (no pulse) and clearly desaturated. */
  animation: none;
  opacity: 0.35;
  filter: grayscale(0.9);
}

@media (prefers-reduced-motion: reduce) {
  .tgrid__node-icon,
  .tgrid__info-toggle--alert,
  .tgrid__backdrop--final,
  .tgrid__cell--fog::before {
    animation: none;
  }

  .tgrid__party {
    transition: none;
  }
}

.tgrid__node-icon--ghost {
  opacity: 0.6;
  filter: grayscale(0.35);
}

.tgrid__cell--boss-node .tgrid__node-icon {
  color: var(--blood);
}

.tgrid__party {
  position: absolute;
  width: 0;
  height: 0;
  /* Matches PARTY_STEP_MS in the script: one cell-to-cell glide per animation
     step, so consecutive steps hand off smoothly instead of interrupting a
     longer transition mid-flight. */
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
  transform: translate(-50%, calc(-50% - var(--terrain-height, 0) * 8px));
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
