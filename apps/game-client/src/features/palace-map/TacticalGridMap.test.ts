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

  it('disables fog cells and enables revealed cells', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    const cells = wrapper.findAll('.tgrid__cell');
    const disabledCount = cells.filter((c) => (c.element as HTMLButtonElement).disabled).length;
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

  it('renders a node icon only on a revealed cell holding a node', () => {
    const node = makeNode({ row: 0, lane: 1 });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.findAll('.tgrid__node-icon')).toHaveLength(1);
  });

  it('shows the standing-node panel when the party is on an available node', () => {
    const node = makeNode({ row: 0, lane: 0, state: 'Available' });
    const room = makeRoom({ nodes: [node] });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__standing-node').exists()).toBe(true);
  });

  it('does not show the standing-node panel when the party cell has no node', () => {
    const wrapper = mount(TacticalGridMap, { props: { room: makeRoom() } });
    expect(wrapper.find('.tgrid__standing-node').exists()).toBe(false);
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

  it('shows the challenge-boss banner when the budget is exhausted and challenge is available', () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__boss-banner').exists()).toBe(true);
  });

  it('hides the challenge-boss banner when budget remains', () => {
    const room = makeRoom({}, { movementBudgetRemaining: 5, canChallengeBossRemotely: false });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.find('.tgrid__boss-banner').exists()).toBe(false);
  });

  it('emits challengeBoss when the banner button is clicked', async () => {
    const room = makeRoom({}, { movementBudgetRemaining: 0, canChallengeBossRemotely: true });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    await wrapper.find('.tgrid__boss-banner button').trigger('click');
    expect(wrapper.emitted('challengeBoss')).toHaveLength(1);
  });

  it('handles a room without a grid gracefully', () => {
    const room = makeRoom({ grid: null });
    const wrapper = mount(TacticalGridMap, { props: { room } });
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.findAll('.tgrid__cell')).toHaveLength(0);
  });
});
