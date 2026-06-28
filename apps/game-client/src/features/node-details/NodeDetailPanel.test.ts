// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import NodeDetailPanel from './NodeDetailPanel.vue';
import type { NodeDto } from '../runs/types/runTypes';

function mountPanel(
  node: NodeDto | null = null,
  isLoading = false,
  hasActiveCombat = false,
  hasPendingReward = false,
) {
  return mount(NodeDetailPanel, {
    props: { node, isLoading, hasActiveCombat, hasPendingReward },
  });
}

describe('NodeDetailPanel', () => {
  const baseNode: NodeDto = {
    id: 'node-1',
    type: 'Combat',
    state: 'Available',
    row: 1,
    lane: 0,
    riskLevel: 50,
    rewardProfile: 'combat-common',
    isBoss: false,
    parentNodeIds: [],
  };

  it('renders without crashing with node', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.exists()).toBe(true);
  });

  it('shows empty state when no node', () => {
    const wrapper = mountPanel(null);
    expect(wrapper.text()).toContain('Le Palais vous attend');
    expect(wrapper.text()).toContain('Sélectionnez un nœud');
  });

  it('displays node type', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('Combat');
  });

  it('displays node metadata', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('NODE_DEEP 1');
    expect(wrapper.text()).toContain('Available');
  });

  it('displays risk label', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('Élevé');
  });

  it('displays reward label', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('Butin standard');
  });

  it('displays risk description', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('Cette voie semble dangereuse.');
  });

  it('displays node description', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('Un affrontement direct vous attend');
  });

  it('shows resolve button for Available node', () => {
    const wrapper = mountPanel(baseNode);
    expect(wrapper.text()).toContain('Affronter');
  });

  it('shows continue button for Resolved node', () => {
    const node = { ...baseNode, state: 'Resolved' };
    const wrapper = mountPanel(node);
    expect(wrapper.text()).toContain('Continuer');
  });

  it('emits resolveCurrentEvent when resolve button is clicked', async () => {
    const wrapper = mountPanel(baseNode);
    await wrapper.find('.node-detail__confirm').trigger('click');
    expect(wrapper.emitted('resolveCurrentEvent')).toHaveLength(1);
  });

  it('emits generateNextNodes when continue button is clicked', async () => {
    const node = { ...baseNode, state: 'Resolved' };
    const wrapper = mountPanel(node);
    await wrapper.find('.node-detail__confirm').trigger('click');
    expect(wrapper.emitted('generateNextNodes')).toHaveLength(1);
  });

  it('disables button when isLoading is true', () => {
    const wrapper = mountPanel(baseNode, true);
    const btn = wrapper.find('.node-detail__confirm');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables button when hasActiveCombat is true', () => {
    const wrapper = mountPanel(baseNode, false, true);
    const btn = wrapper.find('.node-detail__confirm');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
    expect(wrapper.text()).toContain('Combat en cours');
  });

  it('disables button when hasPendingReward is true', () => {
    const wrapper = mountPanel(baseNode, false, false, true);
    const btn = wrapper.find('.node-detail__confirm');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
    expect(wrapper.text()).toContain('Récompense en attente');
  });

  it('applies critical risk class for high risk', () => {
    const node = { ...baseNode, riskLevel: 80 };
    const wrapper = mountPanel(node);
    expect(wrapper.text()).toContain('Critique');
  });

  it('applies high risk class for elevated risk', () => {
    const node = { ...baseNode, riskLevel: 60 };
    const wrapper = mountPanel(node);
    expect(wrapper.text()).toContain('Élevé');
  });

  it('applies low risk class for low risk', () => {
    const node = { ...baseNode, riskLevel: 10 };
    const wrapper = mountPanel(node);
    expect(wrapper.text()).toContain('Faible');
  });

  it('applies boss risk class for boss nodes', () => {
    const node = { ...baseNode, isBoss: true, riskLevel: 90 };
    const wrapper = mountPanel(node);
    expect(wrapper.text()).toContain('Boss');
  });

  it('handles different node types', () => {
    const types = ['Elite', 'Rest', 'Item', 'Npc', 'Merchant', 'Law', 'Curse', 'Memory'] as const;
    for (const type of types) {
      const node = { ...baseNode, type, rewardProfile: `${type.toLowerCase()}-common` };
      const wrapper = mountPanel(node);
      expect(wrapper.text()).toContain(type);
    }
  });

  it('handles unknown node type', () => {
    const node = { ...baseNode, type: 'Unknown' };
    const wrapper = mountPanel(node);
    expect(wrapper.text()).toContain('Un nœud inconnu du Palais.');
  });

  it('hides buttons for non-Available/Selected/Resolved nodes', () => {
    const node = { ...baseNode, state: 'Locked' };
    const wrapper = mountPanel(node);
    expect(wrapper.find('.node-detail__confirm').exists()).toBe(false);
  });

  it('shows Selected state button', () => {
    const node = { ...baseNode, state: 'Selected' };
    const wrapper = mountPanel(node);
    expect(wrapper.find('.node-detail__confirm').exists()).toBe(true);
  });
});
