// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import TacticalGridMap from './TacticalGridMap.vue';
import type { NodeDto, RoomDto, RoomGridDto } from '../runs/types/runTypes';

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
    elevation: new Array(9).fill(0),
    obstacleCells: [],
    floorCells: [],
    canSearch: false,
    hintCells: [],
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

// A note on what changed from the CSS-isometric renderer this replaced: cells used to be
// individual DOM `<button class="tgrid__cell">`s, so click-to-move, hover, revealed state, and
// per-cell position/height could all be asserted by querying/regex-parsing that DOM. The board
// is now painted onto a single `<canvas>` (see useTerrainDrawPlan.ts / useTerrainSprites.ts) —
// there's no per-cell DOM node left to query, and jsdom's `<canvas>` has no real layout
// (`getBoundingClientRect()` always returns zeros), so click/hover hit-testing can't be driven
// through simulated DOM events here either. That logic (the isometric projection, its inverse,
// and the sprite selection rules) is pure and framework-agnostic by construction — see
// useTerrainDrawPlan.spec.ts, which tests it directly instead. This file keeps only what's
// still real DOM: the side panel, top tabs, tooltip, boss banner, node markers, and party
// token (all deliberately still DOM overlays — see TacticalGridMap.vue).
//
// Terrain no longer renders a fog-of-war overlay at all: every cell always paints its real
// sprite (floor/obstacle) regardless of reveal state, and node markers no longer dim to a
// "ghost" look for an unrevealed cell either — see useTerrainDrawPlan.ts/TacticalGridMap.vue.
// Fog-of-war still gates movement (useGridCells.isRevealed, unchanged), it just isn't drawn.

describe('TacticalGridMap', () => {
  it('renders without crashing', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.exists()).toBe(true);
  });

  it('renders the terrain canvas', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.find('.tgrid__terrain-canvas').exists()).toBe(true);
  });

  it('floats no sigil over the board — nodes read from what is painted on their tile', () => {
    // Replaces five DOM-marker tests (presence, unrevealed-but-available, resolved, boss).
    // Those markers were flat SVG glyphs hovering above the canvas; the painted scene now says
    // the same things in its own language — a prop for the node, a wash for a spent one, a glow
    // for the boss — and that lives in the draw plan, asserted in useTerrainDrawPlan.spec.ts.
    // This keeps a guard that they never creep back in over the canvas.
    const room = makeRoom({
      nodes: [
        makeNode({ row: 0, lane: 1, state: 'Available' }),
        makeNode({ row: 2, lane: 2, state: 'Resolved' }),
        makeNode({ row: 3, lane: 3, isBoss: true, type: 'RoomBoss' }),
      ],
    });
    const wrapper = mount(TacticalGridMap, { props: { room } });

    expect(wrapper.findAll('.tgrid__node-icon')).toHaveLength(0);
  });

  it('shows the node side panel when the party is on an available node', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__node-panel').exists()).toBe(true);
  });

  it('does not show the node side panel when the party cell has no node', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.find('.tgrid__node-panel').exists()).toBe(false);
  });

  it('opens the panel on the right when the party is in the left half of the grid', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const room = makeRoom({ nodes: [node] }, { partyX: 0, partyY: 0 });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__node-panel--right').exists()).toBe(true);
    expect(wrapper.find('.tgrid__node-panel--left').exists()).toBe(false);
  });

  it('opens the panel on the left when the party is in the right half of the grid', () => {
    const node = makeNode({ row: 0, lane: 2, state: 'Available' });
    const room = makeRoom(
      { nodes: [node] },
      { partyX: 2, partyY: 0, revealedCells: [[0, 0], [1, 0], [2, 0]] },
    );
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__node-panel--left').exists()).toBe(true);
    expect(wrapper.find('.tgrid__node-panel--right').exists()).toBe(false);
  });

  it('collapses and reopens the node panel', async () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });

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
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const enterButton = wrapper.findAll('button').find((b) => b.text().includes('Entrer'));
    await enterButton!.trigger('click');
    expect(wrapper.emitted('enterNode')).toEqual([['node-1']]);
  });

  it('shows the wager button for a combat-flavored node below Fatal', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', combatRiskTier: 'Tendu' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    expect(wagerButton).toBeDefined();
  });

  it('hides the wager button for a node already at Fatal risk', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', combatRiskTier: 'Fatal' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    expect(wagerButton).toBeUndefined();
  });

  it('hides the wager button for a non-combat node', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', type: 'Item', combatRiskTier: null });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    expect(wagerButton).toBeUndefined();
  });

  it('emits wagerNode when the wager button is clicked', async () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available', combatRiskTier: 'Tendu' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const wagerButton = wrapper.findAll('button').find((b) => b.text().includes('Provoquer le destin'));
    await wagerButton!.trigger('click');
    expect(wrapper.emitted('wagerNode')).toEqual([['node-1']]);
  });

  it('hides the room-wide difficulty tab when no combat node is still available', () => {
    const room = makeRoom({
      nodes: [makeNode({ row: 2, lane: 2, state: 'Resolved', combatRiskTier: 'Tendu' })],
    });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__difficulty-tab').exists()).toBe(false);
  });

  it('shows the room-wide difficulty tab when a combat node is still available', () => {
    const room = makeRoom({
      nodes: [makeNode({ row: 2, lane: 2, state: 'Available', combatRiskTier: 'Tendu' })],
    });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__difficulty-tab').exists()).toBe(true);
  });

  it('opens the difficulty popover with all 5 tiers on click', async () => {
    const room = makeRoom({
      nodes: [makeNode({ row: 2, lane: 2, state: 'Available', combatRiskTier: 'Tendu' })],
    });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__difficulty-popover').exists()).toBe(false);

    await wrapper.find('.tgrid__difficulty-tab').trigger('click');

    const options = wrapper.findAll('.tgrid__difficulty-option');
    expect(options).toHaveLength(5);
    expect(options.map((o) => o.text())).toEqual(['Calme', 'Tendu', 'Dangereux', 'Périlleux', 'Fatal']);
  });

  it('emits setRoomRiskTier and closes the popover when a tier is chosen', async () => {
    const room = makeRoom({
      nodes: [makeNode({ row: 2, lane: 2, state: 'Available', combatRiskTier: 'Tendu' })],
    });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    await wrapper.find('.tgrid__difficulty-tab').trigger('click');

    const fatalOption = wrapper.findAll('.tgrid__difficulty-option').find((o) => o.text() === 'Fatal');
    await fatalOption!.trigger('click');

    expect(wrapper.emitted('setRoomRiskTier')).toEqual([['Fatal']]);
    expect(wrapper.find('.tgrid__difficulty-popover').exists()).toBe(false);
  });

  it('the info overlay (kicker/budget/boss banner) is collapsed by default', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.find('.tgrid__info-overlay--collapsed').exists()).toBe(true);
    expect(wrapper.find('.tgrid__info-body').exists()).toBe(false);
  });

  it('expands and re-collapses the info overlay on toggle click, overlaying the map', async () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });

    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__info-overlay--collapsed').exists()).toBe(false);
    expect(wrapper.find('.tgrid__info-body').exists()).toBe(true);
    expect(wrapper.text()).toContain('Exploration tactique');

    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__info-overlay--collapsed').exists()).toBe(true);
    expect(wrapper.find('.tgrid__info-body').exists()).toBe(false);
  });

  it('shows a pulsing alert dot on the collapsed info tab when the boss challenge is available', () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__info-alert-dot').exists()).toBe(true);
  });

  it('hides the alert dot once the info overlay is expanded', async () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__info-alert-dot').exists()).toBe(false);
  });

  it('glows the info toggle when the boss challenge is available', () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__info-toggle').classes()).toContain('tgrid__info-toggle--alert');
  });

  it('does not glow the info toggle when the boss challenge is unavailable', () => {
    const room = makeRoom({}, { movementBudgetRemaining: 5, canChallengeBossRemotely: false });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__info-toggle').classes()).not.toContain('tgrid__info-toggle--alert');
  });

  it('shows the challenge-boss banner when the budget is exhausted and challenge is available', async () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__boss-banner').exists()).toBe(true);
  });

  it('hides the challenge-boss banner when budget remains', async () => {
    const room = makeRoom({}, { movementBudgetRemaining: 5, canChallengeBossRemotely: false });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    expect(wrapper.find('.tgrid__boss-banner').exists()).toBe(false);
  });

  it('emits challengeBoss when the banner button is clicked', async () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    await wrapper.find('.tgrid__info-toggle').trigger('click');
    await wrapper.find('.tgrid__boss-banner button').trigger('click');
    expect(wrapper.emitted('challengeBoss')).toHaveLength(1);
  });

  it('emits search from the search tab without expanding the info panel first', async () => {
    // The info panel starts collapsed, so the labelled button inside it is out of reach on
    // arrival. The tab is what the player actually sees and clicks; it used to be an inert div,
    // which is exactly what made searching look broken.
    const room = makeRoom({}, { canSearch: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });

    await wrapper.find('.tgrid__search-tab').trigger('click');

    expect(wrapper.emitted('search')).toHaveLength(1);
  });

  it('hides the search tab when there is nothing within reach to find', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom({}, { canSearch: false }) } });
    expect(wrapper.find('.tgrid__search-tab').exists()).toBe(false);
  });

  it('shows the room name next to the "Exploration tactique" tag, falling back to theme', () => {
    const room = makeRoom({ theme: 'La Forêt', catalogName: null });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__room-tab').text()).toBe('La Forêt');
  });

  it('prefers the canon room name over the theme when both are present', () => {
    const room = makeRoom({ theme: 'La Forêt', catalogName: 'Le temple de Mounkaanêt' });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__room-tab').text()).toBe('Le temple de Mounkaanêt');
  });

  it('always shows the Lois tab, independent of the info overlay collapse state', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.find('.tgrid__laws-tab').exists()).toBe(true);
  });

  it('emits toggleLaws when the Lois tab is clicked', async () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    await wrapper.find('.tgrid__laws-tab').trigger('click');
    expect(wrapper.emitted('toggleLaws')).toHaveLength(1);
  });

  it('shows an influence count badge on the Lois tab when provided', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom(), influenceCount: 3 } });
    expect(wrapper.find('.tgrid__laws-tab-count').text()).toBe('3');
  });

  it('hides the influence count badge when there are no active influences', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom(), influenceCount: 0 } });
    expect(wrapper.find('.tgrid__laws-tab-count').exists()).toBe(false);
  });

  // Room backdrop: the seven per-theme CSS gradient stacks are gone — the backdrop is now
  // painted into the terrain canvas by the tile engine (drawBackdrop), seeded by the room id.
  // There is no `.tgrid__backdrop` element left to assert on, and jsdom cannot judge canvas
  // pixels. The decision those tests really guarded — which theme a room resolves to, and the
  // fallback for an unknown one — moved to useRoomBackdropTheme.spec.ts, where it is testable
  // without mounting anything.

  // Party token position + step-by-step movement animation: the token is now drawn INTO the
  // terrain canvas (see useTerrainSprites' 'party' sprite kind), not a separate DOM element,
  // so there's no `.tgrid__party` node left to query here. Its screen position/sprite/depth-
  // sort entry is pure and framework-agnostic by construction — see useTerrainDrawPlan.spec.ts
  // ("party token" describe block). The underlying step-by-step animation timing (X-then-Y,
  // one cell per PARTY_STEP_MS) is covered by usePartyTokenPath.spec.ts, which still drives
  // the same displayPartyX/displayPartyY this component feeds into the draw plan.
});
