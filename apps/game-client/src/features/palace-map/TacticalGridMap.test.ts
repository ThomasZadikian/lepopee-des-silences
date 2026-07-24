// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
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

describe('TacticalGridMap', () => {
  it('renders without crashing', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.exists()).toBe(true);
  });

  it('renders one cell per grid position', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.findAll('.tgrid__cell')).toHaveLength(9);
  });

  it('marks revealed cells as revealed and the rest as fog', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.findAll('.tgrid__cell--revealed')).toHaveLength(2);
    expect(wrapper.findAll('.tgrid__cell--fog')).toHaveLength(7);
  });

  it('marks fog cells as aria-disabled and revealed cells as not', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    const disabledCount = cells.filter((c) => c.attributes('aria-disabled') === 'true').length;
    expect(disabledCount).toBe(7);
  });

  it('emits moveRequest when clicking a revealed non-party cell', async () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    // revealed cells are (0,0) [party] and (1,0) — index 1 in row-major order.
    await cells[1].trigger('click');
    expect(wrapper.emitted('moveRequest')).toEqual([[1, 0]]);
  });

  it('does not emit moveRequest when clicking a fog cell', async () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    await cells[2].trigger('click'); // (2,0) — not revealed
    expect(wrapper.emitted('moveRequest')).toBeUndefined();
  });

  it('does not emit moveRequest when clicking the party cell', async () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    await cells[0].trigger('click'); // (0,0) — party cell
    expect(wrapper.emitted('moveRequest')).toBeUndefined();
  });

  it('renders a node icon on a revealed cell holding a node', () => {
    const node = makeNode({ row: 0, lane: 1 });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.findAll('.tgrid__node-icon')).toHaveLength(1);
  });

  it('renders a dimmed marker icon for an available node on an unrevealed (fogged) cell', () => {
    // (2,2) is outside the default revealedCells ([[0,0],[1,0]]) — the backend now sends
    // Available nodes regardless of fog so the player still sees where to head.
    const node = makeNode({ row: 2, lane: 2, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });

    const icons = wrapper.findAll('.tgrid__node-icon');
    expect(icons).toHaveLength(1);
    expect(icons[0].classes()).toContain('tgrid__node-icon--ghost');

    const cells = wrapper.findAll('.tgrid__cell');
    const fogMarkerCell = cells.find((c) => c.classes().includes('tgrid__cell--fog-marker'));
    expect(fogMarkerCell).toBeDefined();
  });

  it('shows a hover tooltip with just the node type when hovering a node cell', async () => {
    // The tooltip is Teleported to <body> so it can escape the map's overflow:hidden
    // clipping — query the document directly rather than the mounted wrapper subtree.
    const node = makeNode({ row: 0, lane: 1, type: 'Merchant' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room }, attachTo: document.body });

    expect(document.querySelector('.tgrid__hover-tooltip')).toBeNull();

    const cells = wrapper.findAll('.tgrid__cell');
    await cells[1].trigger('mouseenter'); // (1,0) holds the node

    const tooltip = document.querySelector('.tgrid__hover-tooltip');
    expect(tooltip).not.toBeNull();
    expect(tooltip?.textContent?.trim()).toBe('Marchand');

    await cells[1].trigger('mouseleave');
    expect(document.querySelector('.tgrid__hover-tooltip')).toBeNull();

    wrapper.unmount();
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

  it('marks a resolved node as visually spent', () => {
    const node = makeNode({ row: 0, lane: 1, state: 'Resolved' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });

    expect(wrapper.find('.tgrid__cell--resolved-node').exists()).toBe(true);
    expect(wrapper.find('.tgrid__node-icon--resolved').exists()).toBe(true);
  });

  it('does not mark an available node as resolved', () => {
    const node = makeNode({ row: 0, lane: 1, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });

    expect(wrapper.find('.tgrid__cell--resolved-node').exists()).toBe(false);
    expect(wrapper.find('.tgrid__node-icon--resolved').exists()).toBe(false);
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

  it('handles a room without a grid gracefully', () => {
    const room = makeRoom({ grid: null });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.findAll('.tgrid__cell')).toHaveLength(0);
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

  // ── Fake isometry / height (2.5D projection) ────────────────────────────────────

  it('positions each cell at a distinct isometric coordinate', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    const positions = new Set(cells.map((c) => c.attributes('style')));
    expect(positions.size).toBe(cells.length);
  });

  it('positions the party token via the same isometric projection as its cell', () => {
    const room = makeRoom({}, { partyX: 1, partyY: 0 });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    const partyStyle = wrapper.find('.tgrid__party').attributes('style') ?? '';
    const cells = wrapper.findAll('.tgrid__cell');
    // (1,0) is the second cell in row-major order for a 3x3 grid.
    const cellStyle = cells[1].attributes('style') ?? '';

    const extractLeft = (style: string) => style.match(/left:\s*([\d.]+)%/)?.[1];
    const extractTop = (style: string) => style.match(/top:\s*([\d.]+)%/)?.[1];

    expect(extractLeft(partyStyle)).toBe(extractLeft(cellStyle));
    expect(extractTop(partyStyle)).toBe(extractTop(cellStyle));
  });

  it('carries a distinct --terrain-height custom property per cell', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    const heights = cells.map((c) => c.attributes('style')?.match(/--terrain-height:\s*(\d)/)?.[1]);
    expect(heights.every((h) => h !== undefined)).toBe(true);
  });

  // ── Party token movement animation (step-by-step, not a teleport) ──────────────

  it('steps the party token through each intermediate cell instead of jumping straight to the destination', async () => {
    vi.useFakeTimers();
    try {
      const room = makeRoom({}, { partyX: 0, partyY: 0, revealedCells: [[0, 0], [1, 0], [2, 0]] });
      const wrapper = mount(TacticalGridMap, { props: { room } });

      const extractLeft = (style: string | undefined) => style?.match(/left:\s*([\d.]+)%/)?.[1];
      const currentPartyLeft = () => extractLeft(wrapper.find('.tgrid__party').attributes('style'));

      const startLeft = currentPartyLeft();
      const finalCellLeft = extractLeft(wrapper.findAll('.tgrid__cell')[2].attributes('style'));

      await wrapper.setProps({
        room: makeRoom({}, { partyX: 2, partyY: 0, revealedCells: [[0, 0], [1, 0], [2, 0]] }),
      });

      // One step interval in: the token should have advanced exactly one cell
      // (toward x=1) — neither still at the start nor already at the destination,
      // since the move animates cell-by-cell rather than jumping straight there.
      vi.advanceTimersByTime(150);
      await wrapper.vm.$nextTick();
      const midLeft = currentPartyLeft();
      expect(midLeft).not.toBe(startLeft);
      expect(midLeft).not.toBe(finalCellLeft);

      vi.advanceTimersByTime(1000);
      await wrapper.vm.$nextTick();

      expect(currentPartyLeft()).toBe(finalCellLeft);
    } finally {
      vi.useRealTimers();
    }
  });

  it('snaps the party token immediately (no animation) when the room changes', async () => {
    vi.useFakeTimers();
    try {
      const roomA = makeRoom({ id: 'room-1' }, { partyX: 0, partyY: 0 });
      const wrapper = mount(TacticalGridMap, { props: { room: roomA } });

      const roomB = makeRoom({ id: 'room-2' }, { partyX: 1, partyY: 0 });
      await wrapper.setProps({ room: roomB });

      const partyLeft = wrapper.find('.tgrid__party').attributes('style')
        ?.match(/left:\s*([\d.]+)%/)?.[1];
      const cellLeft = wrapper.findAll('.tgrid__cell')[1].attributes('style')
        ?.match(/left:\s*([\d.]+)%/)?.[1];

      expect(partyLeft).toBe(cellLeft);
    } finally {
      vi.useRealTimers();
    }
  });
});
