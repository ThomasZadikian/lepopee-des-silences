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

  it('renders a node icon marker for a revealed node', () => {
    const node = makeNode({ row: 0, lane: 1 });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const icons = wrapper.findAll('.tgrid__node-icon');
    expect(icons).toHaveLength(1);
  });

  it('renders a node icon marker for an available node on an unrevealed cell, same as a revealed one', () => {
    // (2,2) is outside the default revealedCells ([[0,0],[1,0]]) — the backend sends Available
    // nodes regardless of fog so the player still sees where to head. There's no more visual
    // distinction for reveal state (fog rendering was removed), so this marker paints exactly
    // like any other node marker.
    const node = makeNode({ row: 2, lane: 2, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });

    const icons = wrapper.findAll('.tgrid__node-icon');
    expect(icons).toHaveLength(1);
  });

  it('marks a resolved node marker as visually spent', () => {
    const node = makeNode({ row: 0, lane: 1, state: 'Resolved' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__node-icon--resolved').exists()).toBe(true);
  });

  it('does not mark an available node marker as resolved', () => {
    const node = makeNode({ row: 0, lane: 1, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__node-icon--resolved').exists()).toBe(false);
  });

  it('marks a boss node marker distinctly', () => {
    const node = makeNode({ row: 0, lane: 1, isBoss: true, type: 'RoomBoss' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__node-icon--boss').exists()).toBe(true);
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

  // ── Room backdrop (theme-coherent, CSS-only) ────────────────────────────────────

  it.each([
    ['Threshold', 'tgrid__backdrop--threshold'],
    ['Memory', 'tgrid__backdrop--memory'],
    ['Forest', 'tgrid__backdrop--forest'],
    ['Rupture', 'tgrid__backdrop--rupture'],
    ['Silence', 'tgrid__backdrop--silence'],
    ['Antechamber', 'tgrid__backdrop--antechamber'],
    ['Final', 'tgrid__backdrop--final'],
  ])('applies the %s theme backdrop class', (theme, expectedClass) => {
    const room = makeRoom({ theme });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__backdrop').classes()).toContain(expectedClass);
  });

  it('falls back to the default backdrop for an unrecognized theme', () => {
    const room = makeRoom({ theme: 'La Forêt' });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__backdrop').classes()).toContain('tgrid__backdrop--default');
  });

  it('gives two rooms with the same id the same backdrop nuance', () => {
    const roomA = makeRoom({ id: 'room-42', theme: 'Forest' });
    const roomB = makeRoom({ id: 'room-42', theme: 'Forest' });
    const wrapperA = mount(TacticalGridMap, { props: { room: roomA } });
    const wrapperB = mount(TacticalGridMap, { props: { room: roomB } });
    expect(wrapperA.find('.tgrid__backdrop').attributes('style'))
      .toBe(wrapperB.find('.tgrid__backdrop').attributes('style'));
  });

  it('gives two rooms with different ids a different backdrop nuance', () => {
    const roomA = makeRoom({ id: 'room-1', theme: 'Forest' });
    const roomB = makeRoom({ id: 'room-999', theme: 'Forest' });
    const wrapperA = mount(TacticalGridMap, { props: { room: roomA } });
    const wrapperB = mount(TacticalGridMap, { props: { room: roomB } });
    expect(wrapperA.find('.tgrid__backdrop').attributes('style'))
      .not.toBe(wrapperB.find('.tgrid__backdrop').attributes('style'));
  });

  // Party token position + step-by-step movement animation: the token is now drawn INTO the
  // terrain canvas (see useTerrainSprites' 'party' sprite kind), not a separate DOM element,
  // so there's no `.tgrid__party` node left to query here. Its screen position/sprite/depth-
  // sort entry is pure and framework-agnostic by construction — see useTerrainDrawPlan.spec.ts
  // ("party token" describe block). The underlying step-by-step animation timing (X-then-Y,
  // one cell per PARTY_STEP_MS) is covered by usePartyTokenPath.spec.ts, which still drives
  // the same displayPartyX/displayPartyY this component feeds into the draw plan.
});
