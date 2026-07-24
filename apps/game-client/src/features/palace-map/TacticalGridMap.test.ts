// @vitest-environment jsdom
//
// Since Phase A.4, the actual grid/terrain/node/party rendering lives in the TresJS
// scene (PalaceScene.vue and friends) — real WebGL, which jsdom cannot run (confirmed
// at the A.0 spike: TresCanvas throws when mounted under jsdom). This file now only
// covers what's still a genuine HTML/CSS layer around that 3D canvas: the node side
// panel, the top tabs (info overlay/room name/Lois), the hover tooltip, and the boss
// banner — all stubbing PalaceScene/TresCanvas out rather than trying to render them.
//
// Deliberately dropped from the pre-A.4 suite (53 tests), with reasons:
// - Isometric cell/party positioning ("positions each cell at a distinct isometric
//   coordinate", "positions the party token via the same isometric projection",
//   "carries a distinct --terrain-height custom property") — that whole CSS 2.5D
//   projection (isoLeft/isoTop/ISO_*) was deleted in A.4, replaced by literal 3D world
//   coordinates computed inside TerrainTile.vue/PartyToken3D.vue, which aren't
//   reachable under jsdom.
// - Per-theme backdrop CSS class assertions ("applies the %s theme backdrop class",
//   "falls back to the default backdrop", "same/different backdrop nuance") — the
//   backdrop is now scene lighting/fog driven by useRoomBackdropTheme's palette3D,
//   already covered by useRoomBackdropTheme.spec.ts's parity + determinism tests.
// - Party token step-by-step movement animation tests — usePartyTokenPath's own
//   spec (usePartyTokenPath.spec.ts) already asserts the X-then-Y stepping order and
//   the instant-snap-on-room-change behavior directly against the composable.
// - "renders one cell per grid position" / fog vs revealed cell counts / node icon
//   presence / resolved-node visual state — these all targeted DOM nodes
//   (.tgrid__cell, .tgrid__node-icon, .tgrid__cell--resolved-node) that no longer
//   exist; the equivalent behavior (terrain tile vs fog cloud per cell, node marker
//   presence/ghost/resolved material) now lives in PalaceScene/TerrainTile/FogCloud/
//   NodeMarker3D, covered by useGridCells.spec.ts (revealed/party/node lookups) and
//   useNodeGeometry.spec.ts (per-kind geometry) at the composable/table level.
// - The click-to-move *decision* (revealed + not-occupied-by-party) is exercised here
//   only as a thin wiring test against the stubbed PalaceScene's forwarded cellClick
//   event; the underlying isRevealed/isParty logic itself is asserted independently in
//   useGridCells.spec.ts.
import { defineComponent } from 'vue';
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import TacticalGridMap from './TacticalGridMap.vue';
import type { NodeDto, RoomDto, RoomGridDto } from '../runs/types/runTypes';

// Hand-written stubs (rather than `stubs: { TresCanvas: true, PalaceScene: true }`):
// VTU's automatic `true` stub discards a component's default slot entirely, which
// would silently prevent PalaceScene (mounted inside TresCanvas's default slot) from
// ever rendering at all. TresCanvasStub renders its slot through; PalaceSceneStub lets
// tests directly drive the cellClick/nodeHover events TacticalGridMap listens for,
// without needing PalaceScene's real Three.js-driven internals (unreachable under jsdom).
const TresCanvasStub = defineComponent({
  name: 'TresCanvas',
  template: '<div class="tres-canvas-stub"><slot /></div>',
});

const PalaceSceneStub = defineComponent({
  name: 'PalaceScene',
  props: ['room'],
  emits: ['cellClick', 'nodeHover'],
  template: '<div class="palace-scene-stub" />',
});

const globalStubs = { TresCanvas: TresCanvasStub, PalaceScene: PalaceSceneStub };

function makeNode(overrides: Partial<NodeDto> = {}): NodeDto {
  return {
    id: 'node-1',
    type: 'Combat',
    row: 0,
    lane: 1,
    riskLevel: 40,
    combatRiskTier: 'Tendu',
    rewardProfile: 'combat-common',
    parentNodeIds: [],
    state: 'Available',
    isBoss: false,
    isInitial: false,
    hasChosenEventOption: false,
    ...overrides,
  };
}

function makeGrid(overrides: Partial<RoomGridDto> = {}): RoomGridDto {
  return {
    width: 3,
    height: 3,
    movementBudget: 10,
    movementBudgetRemaining: 10,
    partyX: 0,
    partyY: 0,
    canChallengeBossRemotely: false,
    revealedCells: [[0, 0], [1, 0]],
    ...overrides,
  };
}

function makeRoom(overrides: Partial<RoomDto> = {}, gridOverrides: Partial<RoomGridDto> = {}): RoomDto {
  return {
    id: 'room-1',
    depth: 0,
    roomType: 'Threshold',
    theme: 'Threshold',
    state: 'Active',
    currentNodeDepth: 0,
    maxNodeDepth: 0,
    totalNodeCount: 2,
    bossPreview: { bossId: 'boss-1', name: 'Boss', roomType: 'Threshold', dangerHint: 'High' },
    nodes: [],
    availableNodes: [],
    layoutTemplateKey: 'tactical-default-v1',
    layoutTemplateVersion: '1.0.0',
    grid: makeGrid(gridOverrides),
    ...overrides,
  };
}

function mountMap(room: RoomDto, props: Record<string, unknown> = {}) {
  return mount(TacticalGridMap, { props: { room, ...props }, global: { stubs: globalStubs } });
}

describe('TacticalGridMap', () => {
  it('renders without crashing', () => {
    const wrapper = mountMap(makeRoom());
    expect(wrapper.exists()).toBe(true);
  });

  // ── Click-to-move wiring (decision logic itself lives in useGridCells.spec.ts) ────

  it('emits moveRequest when PalaceScene reports a click on a revealed non-party cell', async () => {
    const wrapper = mountMap(makeRoom());
    await wrapper.findComponent('.palace-scene-stub').vm.$emit('cellClick', 1, 0);
    expect(wrapper.emitted('moveRequest')).toEqual([[1, 0]]);
  });

  it('does not emit moveRequest for a click on a fogged cell', async () => {
    const wrapper = mountMap(makeRoom());
    await wrapper.findComponent('.palace-scene-stub').vm.$emit('cellClick', 2, 2);
    expect(wrapper.emitted('moveRequest')).toBeUndefined();
  });

  it('does not emit moveRequest for a click on the party cell', async () => {
    const wrapper = mountMap(makeRoom());
    await wrapper.findComponent('.palace-scene-stub').vm.$emit('cellClick', 0, 0);
    expect(wrapper.emitted('moveRequest')).toBeUndefined();
  });

  // ── Hover tooltip (driven by PalaceScene's forwarded nodeHover event) ────────────

  it('shows a hover tooltip with just the node type when PalaceScene reports a hovered node', async () => {
    // Teleported to <body> — query the document directly rather than the wrapper subtree.
    const node = makeNode({ row: 0, lane: 1, type: 'Merchant' });
    const wrapper = mount(TacticalGridMap, {
      props: { room: makeRoom({ nodes: [node] }) },
      global: { stubs: globalStubs },
      attachTo: document.body,
    });

    expect(document.querySelector('.tgrid__hover-tooltip')).toBeNull();

    await wrapper.findComponent('.palace-scene-stub').vm.$emit('nodeHover', { node, clientX: 10, clientY: 20 });

    const tooltip = document.querySelector('.tgrid__hover-tooltip');
    expect(tooltip).not.toBeNull();
    expect(tooltip?.textContent?.trim()).toBe('Marchand');

    await wrapper.findComponent('.palace-scene-stub').vm.$emit('nodeHover', null);
    expect(document.querySelector('.tgrid__hover-tooltip')).toBeNull();

    wrapper.unmount();
  });

  // ── Node side panel ───────────────────────────────────────────────────────────

  it('shows the node side panel when the party is on an available node', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));
    expect(wrapper.find('.tgrid__node-panel').exists()).toBe(true);
  });

  it('does not show the node side panel when the party cell has no node', () => {
    const wrapper = mountMap(makeRoom());
    expect(wrapper.find('.tgrid__node-panel').exists()).toBe(false);
  });

  it('opens the panel on the right when the party is in the left half of the grid', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }, { partyX: 0, partyY: 0 }));
    expect(wrapper.find('.tgrid__node-panel--right').exists()).toBe(true);
    expect(wrapper.find('.tgrid__node-panel--left').exists()).toBe(false);
  });

  it('opens the panel on the left when the party is in the right half of the grid', () => {
    const node = makeNode({ row: 0, lane: 2, state: 'Available' });
    const wrapper = mountMap(
      makeRoom({ nodes: [node] }, { partyX: 2, partyY: 0, revealedCells: [[0, 0], [1, 0], [2, 0]] }),
    );
    expect(wrapper.find('.tgrid__node-panel--left').exists()).toBe(true);
    expect(wrapper.find('.tgrid__node-panel--right').exists()).toBe(false);
  });

  it('collapses and reopens the node panel', async () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));

    expect(wrapper.find('.tgrid__node-panel-body').exists()).toBe(true);

    await wrapper.find('.tgrid__node-panel-toggle').trigger('click');
    expect(wrapper.find('.tgrid__node-panel--collapsed').exists()).toBe(true);
    expect(wrapper.find('.tgrid__node-panel-body').exists()).toBe(false);

    await wrapper.find('.tgrid__node-panel-toggle').trigger('click');
    expect(wrapper.find('.tgrid__node-panel--collapsed').exists()).toBe(false);
    expect(wrapper.find('.tgrid__node-panel-body').exists()).toBe(true);
  });

  it('emits enterNode when the "Entrer" button is clicked', async () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));
    const enterButton = wrapper.findAll('button').find((b) => b.text().includes('Entrer'));
    await enterButton!.trigger('click');
    expect(wrapper.emitted('enterNode')).toEqual([['node-1']]);
  });

  it('shows the wager button for a combat-flavored node below Fatal', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', combatRiskTier: 'Tendu' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    expect(wagerButton).toBeDefined();
  });

  it('hides the wager button for a node already at Fatal risk', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', combatRiskTier: 'Fatal' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    expect(wagerButton).toBeUndefined();
  });

  it('hides the wager button for a non-combat node', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', type: 'Item', combatRiskTier: null });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    expect(wagerButton).toBeUndefined();
  });

  it('emits wagerNode when the wager button is clicked', async () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', combatRiskTier: 'Tendu' });
    const wrapper = mountMap(makeRoom({ nodes: [node] }));
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    await wagerButton!.trigger('click');
    expect(wrapper.emitted('wagerNode')).toEqual([['node-1']]);
  });

  // ── Top info overlay (kicker/budget/boss banner) ─────────────────────────────────

  it('the info overlay (kicker/budget/boss banner) is collapsed by default', () => {
    const wrapper = mountMap(makeRoom());
    expect(wrapper.find('.tgrid__info-overlay--collapsed').exists()).toBe(true);
    expect(wrapper.find('.tgrid__info-body').exists()).toBe(false);
  });

  it('expands and re-collapses the info overlay on toggle click, overlaying the map', async () => {
    const wrapper = mountMap(makeRoom());

    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__info-overlay--collapsed').exists()).toBe(false);
    expect(wrapper.find('.tgrid__info-body').exists()).toBe(true);
    expect(wrapper.text()).toContain('Exploration tactique');

    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__info-overlay--collapsed').exists()).toBe(true);
    expect(wrapper.find('.tgrid__info-body').exists()).toBe(false);
  });

  it('shows a pulsing alert dot and glows the info toggle when the boss challenge is available', () => {
    const wrapper = mountMap(makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true }));
    expect(wrapper.find('.tgrid__info-alert-dot').exists()).toBe(true);
    expect(wrapper.find('.tgrid__info-toggle').classes()).toContain('tgrid__info-toggle--alert');
  });

  it('hides the alert dot once the info overlay is expanded, and does not glow when unavailable', async () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mountMap(room);
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__info-alert-dot').exists()).toBe(false);

    const calmWrapper = mountMap(makeRoom({}, { movementBudgetRemaining: 5, canChallengeBossRemotely: false }));
    expect(calmWrapper.find('.tgrid__info-toggle').classes()).not.toContain('tgrid__info-toggle--alert');
  });

  it('shows the challenge-boss banner and emits challengeBoss on click', async () => {
    const wrapper = mountMap(makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true }));
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__boss-banner').exists()).toBe(true);

    await wrapper.find('.tgrid__boss-banner button').trigger('click');
    expect(wrapper.emitted('challengeBoss')).toHaveLength(1);
  });

  it('hides the challenge-boss banner when movement budget remains', async () => {
    const wrapper = mountMap(makeRoom({}, { movementBudgetRemaining: 5, canChallengeBossRemotely: false }));
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__boss-banner').exists()).toBe(false);
  });

  // ── Room name / Lois tab ──────────────────────────────────────────────────────

  it('shows the room name next to the "Exploration tactique" tag, falling back to theme', () => {
    const wrapper = mountMap(makeRoom({ theme: 'La Forêt', catalogName: null }));
    expect(wrapper.find('.tgrid__room-tab').text()).toBe('La Forêt');
  });

  it('prefers the canon room name over the theme when both are present', () => {
    const wrapper = mountMap(makeRoom({ theme: 'La Forêt', catalogName: 'Le temple de Mounkaanêt' }));
    expect(wrapper.find('.tgrid__room-tab').text()).toBe('Le temple de Mounkaanêt');
  });

  it('always shows the Lois tab and emits toggleLaws when clicked', async () => {
    const wrapper = mountMap(makeRoom());
    expect(wrapper.find('.tgrid__laws-tab').exists()).toBe(true);
    await wrapper.find('.tgrid__laws-tab').trigger('click');
    expect(wrapper.emitted('toggleLaws')).toHaveLength(1);
  });

  it('shows an influence count badge on the Lois tab when provided, hides it when zero', () => {
    const withBadge = mountMap(makeRoom(), { influenceCount: 3 });
    expect(withBadge.find('.tgrid__laws-tab-count').text()).toBe('3');

    const withoutBadge = mountMap(makeRoom(), { influenceCount: 0 });
    expect(withoutBadge.find('.tgrid__laws-tab-count').exists()).toBe(false);
  });
});
