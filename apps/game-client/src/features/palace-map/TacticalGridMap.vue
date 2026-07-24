<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import * as THREE from 'three';
import { TresCanvas } from '@tresjs/core';

import SigilIcon from '../../shared/components/SigilIcon.vue';
import type { NodeDto, RoomDto } from '../runs/types/runTypes';
import { useNodePresentation, RISK_TIER_DISPLAY } from './composables/useNodePresentation';
import { useGridCells } from './composables/useGridCells';
import PalaceScene from './scene/PalaceScene.vue';

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

const { isRevealed, nodeAt, isParty } = useGridCells(room, grid);
const { sigilKindFor, nodeTypeLabel, nodeTypeDescription } = useNodePresentation();

function onCellClick(x: number, y: number) {
  if (!isRevealed(x, y)) return;

  if (isParty(x, y)) {
    // Standing on a node's cell opens the standing-node side panel — clicking the
    // cell itself is a no-op so it doesn't fight with that panel.
    return;
  }

  emit('moveRequest', x, y);
}

// ── Hover tooltip: shows just the node type, following the cursor ──────────────────
// Driven by PalaceScene's forwarded pointer-enter/pointer-move/pointer-leave events off
// the node markers themselves (TresJS meshes dispatch native-like pointer events with
// clientX/clientY, same coordinate space as a DOM MouseEvent).
const hoveredNode = ref<NodeDto | null>(null);
const mouseX = ref(0);
const mouseY = ref(0);

const tooltipStyle = computed(() => ({
  left: `${mouseX.value + 16}px`,
  top: `${mouseY.value + 16}px`,
}));

function onNodeHover(payload: { node: NodeDto; clientX: number; clientY: number } | null) {
  hoveredNode.value = payload?.node ?? null;
  if (payload) {
    mouseX.value = payload.clientX;
    mouseY.value = payload.clientY;
  }
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
    <div v-if="grid" class="tgrid__canvas">
      <TresCanvas clear-color="#0a0a12" :shadows="true" :shadow-map-type="THREE.VSMShadowMap">
        <PalaceScene :room="room" @cell-click="onCellClick" @node-hover="onNodeHover" />
      </TresCanvas>

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

@media (prefers-reduced-motion: reduce) {
  .tgrid__info-toggle--alert {
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

@keyframes tgrid-node-pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.14); }
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
