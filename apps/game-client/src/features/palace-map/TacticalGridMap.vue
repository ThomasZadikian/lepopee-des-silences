<script setup lang="ts">
import { computed } from 'vue';

import SigilIcon from '../../shared/components/SigilIcon.vue';
import type { NodeDto, RoomDto } from '../runs/types/runTypes';

const props = defineProps<{
  room: RoomDto;
}>();

const emit = defineEmits<{
  moveRequest: [x: number, y: number];
  enterNode: [nodeId: string];
  wagerNode: [nodeId: string];
  challengeBoss: [];
}>();

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

function cellClass(cell: Cell) {
  const revealed = isRevealed(cell.x, cell.y);
  const node = revealed ? nodeAt(cell.x, cell.y) : null;

  return {
    'tgrid__cell--revealed': revealed,
    'tgrid__cell--fog': !revealed,
    'tgrid__cell--node': Boolean(node),
    'tgrid__cell--boss-node': node?.isBoss ?? false,
    'tgrid__cell--party': isParty(cell.x, cell.y),
  };
}

function onCellClick(cell: Cell) {
  if (!isRevealed(cell.x, cell.y)) return;

  if (isParty(cell.x, cell.y)) {
    // Standing on a node's cell opens the standing-node panel below (entrer/wager) —
    // clicking the cell itself is a no-op so it doesn't fight with that panel.
    return;
  }

  emit('moveRequest', cell.x, cell.y);
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
</script>

<template>
  <section class="tgrid">
    <header class="tgrid__header">
      <span class="es-kicker">Exploration tactique</span>
      <span v-if="grid" class="tgrid__budget">
        Déplacement <strong>{{ grid.movementBudgetRemaining }}</strong> / {{ grid.movementBudget }}
      </span>
    </header>

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
        :disabled="!isRevealed(cell.x, cell.y)"
        :aria-label="`Case ${cell.x},${cell.y}`"
        @click="onCellClick(cell)"
      >
        <SigilIcon
          v-if="isRevealed(cell.x, cell.y) && nodeAt(cell.x, cell.y)"
          class="tgrid__node-icon"
          :kind="sigilKindFor(nodeAt(cell.x, cell.y)!)"
          :size="20"
        />
      </button>

      <div class="tgrid__party" :style="partyStyle" aria-hidden="true">
        <span class="tgrid__party-token" />
      </div>
    </div>

    <div v-if="standingNode" class="tgrid__standing-node">
      <div class="tgrid__standing-node-info">
        <SigilIcon :kind="sigilKindFor(standingNode)" :size="22" />
        <div>
          <p class="tgrid__standing-node-type">{{ standingNode.type }}</p>
          <p v-if="standingNode.combatRiskTier" class="tgrid__standing-node-risk">
            {{ standingNode.combatRiskTier }}
          </p>
        </div>
      </div>
      <div class="tgrid__standing-node-actions">
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

    <div v-if="showChallengeBossBanner" class="tgrid__boss-banner">
      <p class="tgrid__boss-banner-text">
        Le budget de déplacement est épuisé. Le gardien de la salle approche à grands pas.
      </p>
      <button type="button" class="es-btn es-btn--blood" @click="emit('challengeBoss')">
        Provoquer le combat de boss →
      </button>
    </div>
  </section>
</template>

<style scoped>
.tgrid {
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: var(--space-3) var(--space-4);
}

.tgrid__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding-bottom: var(--space-2);
  border-bottom: 1px solid var(--line-soft);
  margin-bottom: var(--space-3);
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
  display: grid;
  gap: 2px;
  padding: 6px;
  border: 1px solid color-mix(in oklch, var(--line), transparent 60%);
  background:
    radial-gradient(circle at 20% 50%, var(--wash-gold), transparent 18%),
    radial-gradient(circle at 88% 50%, var(--wash-blood), transparent 12%);
}

.tgrid__cell {
  position: relative;
  display: grid;
  place-items: center;
  border: none;
  border-radius: 2px;
  padding: 0;
  cursor: pointer;
  transition: filter 0.15s ease, transform 0.15s ease;
}

.tgrid__cell:disabled {
  cursor: default;
}

.tgrid__cell--fog {
  background: color-mix(in oklch, var(--void), black 30%);
  opacity: 0.55;
}

.tgrid__cell--revealed {
  background: color-mix(in oklch, var(--panel), black calc(var(--terrain-height, 0) * 6%));
  box-shadow: inset 0 0 0 1px color-mix(in oklch, var(--line), transparent 55%);
}

.tgrid__cell--revealed:not(:disabled):hover {
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

.tgrid__node-icon {
  color: var(--frost);
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

.tgrid__standing-node {
  margin-top: var(--space-3);
  padding: var(--space-3) var(--space-4);
  border: 1px solid color-mix(in oklch, var(--frost), transparent 45%);
  border-radius: 4px;
  background: color-mix(in oklch, var(--panel), var(--frost) 8%);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  flex-wrap: wrap;
}

.tgrid__standing-node-info {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  color: var(--frost);
}

.tgrid__standing-node-type {
  margin: 0;
  color: var(--ink);
  font-family: var(--font-caps);
  font-size: 0.8rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.tgrid__standing-node-risk {
  margin: 0;
  color: var(--ink-3);
  font-size: 0.75rem;
}

.tgrid__standing-node-actions {
  display: flex;
  gap: var(--space-2);
}

.tgrid__boss-banner {
  margin-top: var(--space-3);
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
