// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import PalaceMapPlaceholder from './PalaceMapPlaceholder.vue';
import type { NodeDto } from '../runs/types/runTypes';

function mountMap(
  nodes: NodeDto[] = [],
  availableNodes: NodeDto[] = [],
  selectedNodeId: string | null = null,
  currentRow = 0,
) {
  return mount(PalaceMapPlaceholder, {
    props: { nodes, availableNodes, selectedNodeId, currentRow },
  });
}

describe('PalaceMapPlaceholder', () => {
  const baseNode: NodeDto = {
    id: 'node-1',
    type: 'Combat',
    state: 'Available',
    row: 0,
    lane: 0,
    riskLevel: 50,
    rewardProfile: 'combat-common',
    isBoss: false,
    parentNodeIds: [],
  };

  it('renders without crashing', () => {
    const wrapper = mountMap();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the header', () => {
    const wrapper = mountMap();
    expect(wrapper.text()).toContain('Carte du Palais');
  });

  it('renders nodes', () => {
    const wrapper = mountMap([baseNode]);
    expect(wrapper.findAll('.map__node').length).toBe(1);
  });

  it('emits chooseNode when available node is clicked', async () => {
    const wrapper = mountMap([baseNode], [baseNode]);
    await wrapper.find('.map__node').trigger('click');
    expect(wrapper.emitted('chooseNode')).toBeDefined();
    expect(wrapper.emitted('chooseNode')![0][0]).toBe('node-1');
  });

  it('does not emit chooseNode for unavailable node', async () => {
    const node = { ...baseNode, state: 'Locked' };
    const wrapper = mountMap([node], []);
    await wrapper.find('.map__node').trigger('click');
    expect(wrapper.emitted('chooseNode')).toBeUndefined();
  });

  it('emits deselectNode when canvas is clicked', async () => {
    const wrapper = mountMap([baseNode]);
    await wrapper.find('.map__canvas').trigger('click');
    expect(wrapper.emitted('deselectNode')).toHaveLength(1);
  });

  it('applies available class for available nodes', () => {
    const wrapper = mountMap([baseNode], [baseNode]);
    expect(wrapper.find('.map__node--available').exists()).toBe(true);
  });

  it('applies resolved class for resolved nodes', () => {
    const node = { ...baseNode, state: 'Resolved' };
    const wrapper = mountMap([node]);
    expect(wrapper.find('.map__node--resolved').exists()).toBe(true);
  });

  it('applies selected class for selected nodes', () => {
    const node = { ...baseNode, state: 'Selected' };
    const wrapper = mountMap([node]);
    expect(wrapper.find('.map__node--selected').exists()).toBe(true);
  });

  it('applies danger class for boss nodes', () => {
    const node = { ...baseNode, isBoss: true, type: 'RoomBoss', state: 'Locked' };
    const wrapper = mountMap([node]);
    expect(wrapper.find('.map__node--danger').exists()).toBe(true);
  });

  it('applies locked class for locked nodes', () => {
    const node = { ...baseNode, state: 'Locked' };
    const wrapper = mountMap([node]);
    expect(wrapper.find('.map__node--locked').exists()).toBe(true);
  });

  it('disables buttons for non-available nodes', () => {
    const node = { ...baseNode, state: 'Locked' };
    const wrapper = mountMap([node]);
    const btn = wrapper.find('.map__node');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('displays node glyph based on type', () => {
    const node = { ...baseNode, type: 'Combat' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('△');
  });

  it('displays elite glyph', () => {
    const node = { ...baseNode, type: 'Elite' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('▲');
  });

  it('displays rest glyph', () => {
    const node = { ...baseNode, type: 'Rest' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('◐');
  });

  it('displays merchant glyph', () => {
    const node = { ...baseNode, type: 'Merchant' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('◎');
  });

  it('displays law glyph', () => {
    const node = { ...baseNode, type: 'Law' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('◆');
  });

  it('displays item glyph', () => {
    const node = { ...baseNode, type: 'Item' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('♢');
  });

  it('displays npc glyph', () => {
    const node = { ...baseNode, type: 'Npc' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('◇');
  });

  it('displays fallback glyph for unknown type', () => {
    const node = { ...baseNode, type: 'Unknown' };
    const wrapper = mountMap([node]);
    expect(wrapper.text()).toContain('○');
  });

  it('shows risk label in title', () => {
    const wrapper = mountMap([baseNode]);
    const node = wrapper.find('.map__node');
    expect(node.attributes('title')).toContain('Risque élevé');
  });

  it('shows boss risk label', () => {
    const node = { ...baseNode, isBoss: true, type: 'RoomBoss' };
    const wrapper = mountMap([node]);
    const el = wrapper.find('.map__node');
    expect(el.attributes('title')).toContain('Boss');
  });

  it('shows critical risk label', () => {
    const node = { ...baseNode, riskLevel: 80 };
    const wrapper = mountMap([node]);
    const el = wrapper.find('.map__node');
    expect(el.attributes('title')).toContain('Risque critique');
  });

  it('shows high risk label', () => {
    const node = { ...baseNode, riskLevel: 60 };
    const wrapper = mountMap([node]);
    const el = wrapper.find('.map__node');
    expect(el.attributes('title')).toContain('Risque élevé');
  });

  it('shows low risk label', () => {
    const node = { ...baseNode, riskLevel: 10 };
    const wrapper = mountMap([node]);
    const el = wrapper.find('.map__node');
    expect(el.attributes('title')).toContain('Risque faible');
  });

  it('renders edges between connected nodes', () => {
    const parent = { ...baseNode, id: 'parent' };
    const child = { ...baseNode, id: 'child', row: 1, parentNodeIds: ['parent'] };
    const wrapper = mountMap([parent, child]);
    expect(wrapper.findAll('.map__edge').length).toBe(1);
  });

  it('highlights selected route', () => {
    const parent = { ...baseNode, id: 'parent', state: 'Resolved' };
    const child = { ...baseNode, id: 'child', row: 1, parentNodeIds: ['parent'] };
    const wrapper = mountMap([parent, child], [child], 'child');
    expect(wrapper.find('.map__node--selected-route').exists()).toBe(true);
  });

  it('dims unselected route nodes', () => {
    const parent = { ...baseNode, id: 'parent', state: 'Resolved' };
    const selected = { ...baseNode, id: 'selected', row: 1, parentNodeIds: ['parent'] };
    const other = { ...baseNode, id: 'other', row: 1, parentNodeIds: ['parent'] };
    const wrapper = mountMap([parent, selected, other], [selected, other], 'selected');
    expect(wrapper.find('.map__node--unselected-route').exists()).toBe(true);
  });

  it('hides edges for past unchosen nodes', () => {
    const past = { ...baseNode, id: 'past', row: 0, state: 'Available' };
    const current = { ...baseNode, id: 'current', row: 1, parentNodeIds: ['past'] };
    const wrapper = mountMap([past, current], [], null, 2);
    expect(wrapper.findAll('.map__edge').length).toBe(0);
  });

  it('selects node by matching selectedNodeId prop', () => {
    const node = { ...baseNode, state: 'Available' };
    const wrapper = mountMap([node], [node], 'node-1');
    expect(wrapper.find('.map__node--selected').exists()).toBe(true);
  });

  it('handles empty nodes array', () => {
    const wrapper = mountMap([]);
    expect(wrapper.exists()).toBe(true);
  });
});
