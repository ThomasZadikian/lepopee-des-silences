<script setup lang="ts">
import { computed, ref, watch } from 'vue';

import SigilIcon from '../../shared/components/SigilIcon.vue';
import type { NodeDto, RoomDto } from '../runs/types/runTypes';

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

const grid = computed(() => props.room.grid ?? null);

const revealedCells = computed(() => {
  const set = new Set<string>();
  for (const [x, y] of grid.value?.revealedCells ?? []) {
    set.add(`${x},${y}`);
  }
  return set;
});

function isRevealed(x: number, y: number): boolean {
  return revealedCells.value.has(`${x},${y}`);
}

const nodesByCell = computed(() => {
  const map = new Map<string, NodeDto>();
  for (const node of props.room.nodes) {
    map.set(`${node.lane},${node.row}`, node);
  }
  return map;
});

function nodeAt(x: number, y: number): NodeDto | null {
  return nodesByCell.value.get(`${x},${y}`) ?? null;
}

function isParty(x: number, y: number): boolean {
  return grid.value !== null && grid.value.partyX === x && grid.value.partyY === y;
}

type Cell = { x: number; y: number };

const cells = computed<Cell[]>(() => {
  const g = grid.value;
  if (!g) return [];

  const result: Cell[] = [];
  for (let y = 0; y < g.height; y++) {
    for (let x = 0; x < g.width; x++) {
      result.push({ x, y });
    }
  }
  return result;
});

const partyStyle = computed(() => {
  const g = grid.value;
  if (!g) return {};

  return {
    left: `${((g.partyX + 0.5) / g.width) * 100}%`,
    top: `${((g.partyY + 0.5) / g.height) * 100}%`,
  };
});

/**
 * Purely cosmetic terrain height, deterministic from (roomId, x, y) — never sent
 * to or read from the backend. Only shades the cell; no mechanical effect during
 * exploration (height only matters once the future tactical combat chantier lands).
 */
function terrainHeight(x: number, y: number): number {
  const seed = `${props.room.id}:${x}:${y}`;
  let hash = 0;
  for (let i = 0; i < seed.length; i++) {
    hash = (hash * 31 + seed.charCodeAt(i)) >>> 0;
  }
  return hash % 4;
}

const SIGIL_KIND_BY_NODE_TYPE: Record<string, string> = {
  Combat: 'combat',
  Elite: 'elite',
  Rare: 'rare',
  RoomBoss: 'boss',
  FinalBoss: 'boss',
  Item: 'objet',
  Npc: 'pnj',
  Rest: 'repos',
  Merchant: 'marchand',
  Law: 'loi',
  Curse: 'malediction',
};

function sigilKindFor(node: NodeDto): string {
  return SIGIL_KIND_BY_NODE_TYPE[node.type] ?? 'objet';
}

// Short label — used both for the hover tooltip (type only, as requested) and as the
// side panel's kicker.
const NODE_TYPE_LABEL: Record<string, string> = {
  Combat: 'Combat',
  Elite: 'Élite',
  Rare: 'Rencontre rare',
  RoomBoss: 'Gardien de salle',
  FinalBoss: 'Confrontation finale',
  Item: 'Objet',
  Npc: 'Présence',
  Memory: 'Souvenir',
  Rest: 'Repos',
  Merchant: 'Marchand',
  Law: 'Décret du Palais',
  Curse: 'Malédiction',
};

function nodeTypeLabel(node: NodeDto): string {
  return NODE_TYPE_LABEL[node.type] ?? node.type;
}

// Fuller flavor text for the side panel's description.
const NODE_TYPE_DESCRIPTION: Record<string, string> = {
  Combat: 'Un affrontement direct vous attend dans les profondeurs du Palais.',
  Elite: "Un adversaire d'élite barre le passage. La victoire sera coûteuse.",
  Rare: "Une présence rare s'est manifestée — imprévisible et potentiellement précieuse.",
  RoomBoss: 'Le Gardien de cette salle attend. Aucun passage sans combat.',
  FinalBoss: 'La présence finale du Palais. Tout converge ici.',
  Rest: 'Un refuge temporaire. Reprendre souffle avant de continuer.',
  Item: 'Un objet a été laissé ici. Son origine reste obscure.',
  Npc: "Quelqu'un — ou quelque chose — souhaite vous parler.",
  Merchant: "Un marchand propose ses services dans l'ombre du Palais.",
  Law: 'Une règle du Palais inscrite dans ses murs. La lire vous changera.',
  Curse: 'Une malédiction latente. Y toucher a un coût.',
  Memory: "Un écho du passé. Ce souvenir n'est pas le vôtre.",
};

function nodeTypeDescription(node: NodeDto): string {
  return NODE_TYPE_DESCRIPTION[node.type] ?? 'Un nœud inconnu du Palais.';
}

const RISK_TIER_DISPLAY: Record<string, { text: string; cls: string }> = {
  Calme: { text: 'Calme', cls: 'tgrid__risk--low' },
  Tendu: { text: 'Tendu', cls: 'tgrid__risk--moderate' },
  Dangereux: { text: 'Dangereux', cls: 'tgrid__risk--high' },
  Perilleux: { text: 'Périlleux', cls: 'tgrid__risk--critical' },
  Fatal: { text: 'Fatal', cls: 'tgrid__risk--fatal' },
};

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
      class="tgrid__canvas"
      :style="{
        gridTemplateColumns: `repeat(${grid.width}, 1fr)`,
        gridTemplateRows: `repeat(${grid.height}, 1fr)`,
      }"
    >
      <button
        v-for="cell in cells"
        :key="`${cell.x}-${cell.y}`"
        type="button"
        class="tgrid__cell"
        :class="cellClass(cell)"
        :style="{ '--terrain-height': terrainHeight(cell.x, cell.y) }"
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
  display: flex;
  flex-direction: column;
  padding: var(--space-3) var(--space-4);
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
  display: grid;
  gap: 2px;
  padding: 6px;
  overflow: hidden;
  border: 1px solid color-mix(in oklch, var(--line), transparent 60%);
  background:
    radial-gradient(circle at 20% 50%, var(--wash-gold), transparent 18%),
    radial-gradient(circle at 88% 50%, var(--wash-blood), transparent 12%);
}

.tgrid__cell {
  position: relative;
  min-height: 0;
  min-width: 0;
  display: grid;
  place-items: center;
  border: none;
  border-radius: 2px;
  padding: 0;
  cursor: pointer;
  transition: filter 0.15s ease, transform 0.15s ease;
}

.tgrid__cell[aria-disabled='true'] {
  cursor: default;
}

.tgrid__cell--fog {
  background: color-mix(in oklch, var(--void), black 30%);
  opacity: 0.55;
}

.tgrid__cell--fog-marker {
  /* Known objective through the fog — noticeably less dim than plain fog so the
     marker reads at a glance, but still clearly distinct from a revealed cell. */
  opacity: 0.78;
}

.tgrid__cell--revealed {
  background: color-mix(in oklch, var(--panel), black calc(var(--terrain-height, 0) * 6%));
  box-shadow: inset 0 0 0 1px color-mix(in oklch, var(--line), transparent 55%);
}

.tgrid__cell--revealed:not([aria-disabled='true']):hover {
  filter: brightness(1.18);
  transform: scale(1.03);
}

.tgrid__cell--node.tgrid__cell--revealed {
  background: color-mix(in oklch, var(--panel), var(--frost) 14%);
  box-shadow: inset 0 0 0 1px color-mix(in oklch, var(--frost), transparent 30%);
}

.tgrid__cell--boss-node.tgrid__cell--revealed {
  background: color-mix(in oklch, var(--panel), var(--blood) 18%);
  box-shadow: inset 0 0 0 1px color-mix(in oklch, var(--blood), transparent 25%);
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
  .tgrid__node-icon {
    animation: none;
  }
}

.tgrid__cell--boss-node .tgrid__node-icon {
  color: var(--blood);
}

.tgrid__party {
  position: absolute;
  width: 0;
  height: 0;
  transition: left 0.3s ease, top 0.3s ease;
  pointer-events: none;
  z-index: 3;
}

.tgrid__party-token {
  position: absolute;
  translate: -50% -50%;
  width: 0.85rem;
  height: 0.85rem;
  border-radius: 50%;
  background: var(--gold);
  box-shadow: 0 0 10px 2px color-mix(in oklch, var(--gold), transparent 30%);
}

/* ── Node side panel ─────────────────────────────────────────────────────────── */
.tgrid__node-panel {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 260px;
  max-width: 80%;
  display: flex;
  z-index: 4;
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
  z-index: 5;
  display: flex;
  align-items: flex-start;
  flex-wrap: wrap;
  gap: 3px;
  max-width: calc(100% - 2 * var(--space-3));
}

.tgrid__info-overlay {
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
