// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import PalaceNodeDrawer from './PalaceNodeDrawer.vue';
import type { NodeDto } from '../runs/types/runTypes';

function mountDrawer(
  node: NodeDto | null = null,
  isLoading = false,
  hasActiveCombat = false,
  hasPendingReward = false,
) {
  return mount(PalaceNodeDrawer, {
    props: { node, isLoading, hasActiveCombat, hasPendingReward },
    global: {
      stubs: {
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('PalaceNodeDrawer', () => {
  const baseNode: NodeDto = {
    id: 'node-1',
    type: 'Combat',
    state: 'Available',
    row: 1,
    lane: 0,
    riskLevel: 50,
    combatRiskTier: 'Dangereux',
    rewardProfile: 'combat-common',
    isBoss: false,
    parentNodeIds: [],
  };

  it('renders without crashing with node', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.exists()).toBe(true);
  });

  it('shows empty state when no node', () => {
    const wrapper = mountDrawer(null);
    expect(wrapper.text()).toContain('Sélectionne un nœud');
  });

  it('displays node type label', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.text()).toContain('Confrontation');
  });

  it('displays state chip', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.text()).toContain('Accessible');
  });

  it('displays risk tier', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.text()).toContain('Dangereux');
  });

  it('hides risk tier for non-combat node types', () => {
    const node = { ...baseNode, type: 'Item', combatRiskTier: null };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.node-drawer__risk').exists()).toBe(false);
  });

  it('hides risk tier when combatRiskTier is null on a combat node', () => {
    const node = { ...baseNode, combatRiskTier: null };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.node-drawer__risk').exists()).toBe(false);
  });

  it('displays reward profile when available', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.text()).toContain('combat-common');
  });

  it('emits chooseAndResolve for Available node', async () => {
    const wrapper = mountDrawer(baseNode);
    await wrapper.find('.node-drawer__cta').trigger('click');
    expect(wrapper.emitted('chooseAndResolve')).toHaveLength(1);
  });

  it('emits resolveCurrentEvent for Selected node', async () => {
    const node = { ...baseNode, state: 'Selected' };
    const wrapper = mountDrawer(node);
    await wrapper.find('.node-drawer__cta').trigger('click');
    expect(wrapper.emitted('resolveCurrentEvent')).toHaveLength(1);
  });

  it('emits close when close button is clicked', async () => {
    const wrapper = mountDrawer(baseNode);
    await wrapper.find('.node-drawer__close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('disables CTA when isLoading is true', () => {
    const wrapper = mountDrawer(baseNode, true);
    const btn = wrapper.find('.node-drawer__cta');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables CTA when hasActiveCombat is true', () => {
    const wrapper = mountDrawer(baseNode, false, true);
    expect(wrapper.find('.node-drawer__cta').exists()).toBe(false);
  });

  it('disables CTA when hasPendingReward is true', () => {
    const wrapper = mountDrawer(baseNode, false, false, true);
    expect(wrapper.find('.node-drawer__cta').exists()).toBe(false);
  });

  it('shows hint for Resolved node', () => {
    const node = { ...baseNode, state: 'Resolved' };
    const wrapper = mountDrawer(node);
    expect(wrapper.text()).toContain('Ce nœud a été résolu');
  });

  it('shows hint for Locked node', () => {
    const node = { ...baseNode, state: 'Locked' };
    const wrapper = mountDrawer(node);
    expect(wrapper.text()).toContain('Ce nœud est verrouillé');
  });

  it('applies correct risk class for Calme tier', () => {
    const node = { ...baseNode, combatRiskTier: 'Calme' as const };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.risk--low').exists()).toBe(true);
  });

  it('applies correct risk class for Perilleux tier', () => {
    const node = { ...baseNode, combatRiskTier: 'Perilleux' as const };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.risk--critical').exists()).toBe(true);
  });

  it('applies correct risk class for Fatal tier', () => {
    const node = { ...baseNode, combatRiskTier: 'Fatal' as const };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.risk--fatal').exists()).toBe(true);
  });

  it('handles different node types', () => {
    const types: Record<string, string> = {
      Elite: 'Manifestation elite',
      Rest: 'Repos',
      Item: 'Offrande',
      Npc: 'Présence',
      Merchant: 'Marchand',
      Law: 'Décret du Palais',
      Curse: 'Malédiction',
      Memory: 'Souvenir',
    };
    for (const [type, label] of Object.entries(types)) {
      const node = { ...baseNode, type };
      const wrapper = mountDrawer(node);
      expect(wrapper.text()).toContain(label);
    }
  });

  it('handles different state labels', () => {
    const states: Record<string, string> = {
      Available: 'Accessible',
      Selected: 'Sélectionné',
      Resolved: 'Résolu',
      Locked: 'Verrouillé',
      Unreachable: 'Inaccessible',
      Planned: 'En attente',
    };
    for (const [state, label] of Object.entries(states)) {
      const node = { ...baseNode, state: state as any };
      const wrapper = mountDrawer(node);
      expect(wrapper.text()).toContain(label);
    }
  });

  it('hides reward profile when not available', () => {
    const node = { ...baseNode, rewardProfile: undefined as any };
    const wrapper = mountDrawer(node);
    expect(wrapper.text()).not.toContain('Profil de récompense');
  });

  it('shows loading text when isLoading is true', () => {
    const wrapper = mountDrawer(baseNode, true);
    expect(wrapper.text()).toContain('Résolution');
  });

  it('shows the wager button for an available combat node below Fatal', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.find('.node-drawer__wager').exists()).toBe(true);
  });

  it('hides the wager button for non-combat node types', () => {
    const node = { ...baseNode, type: 'Item', combatRiskTier: null };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.node-drawer__wager').exists()).toBe(false);
  });

  it('hides the wager button when the node is already at Fatal tier', () => {
    const node = { ...baseNode, combatRiskTier: 'Fatal' as const };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.node-drawer__wager').exists()).toBe(false);
  });

  it('hides the wager button when the node is not Available', () => {
    const node = { ...baseNode, state: 'Selected' };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.node-drawer__wager').exists()).toBe(false);
  });

  it('hides the wager button when there is an active combat or pending reward', () => {
    const wrapperCombat = mountDrawer(baseNode, false, true);
    expect(wrapperCombat.find('.node-drawer__wager').exists()).toBe(false);

    const wrapperReward = mountDrawer(baseNode, false, false, true);
    expect(wrapperReward.find('.node-drawer__wager').exists()).toBe(false);
  });

  it('emits wagerNode with the node id when the wager button is clicked', async () => {
    const wrapper = mountDrawer(baseNode);
    await wrapper.find('.node-drawer__wager').trigger('click');
    expect(wrapper.emitted('wagerNode')).toHaveLength(1);
    expect(wrapper.emitted('wagerNode')![0][0]).toBe('node-1');
  });
});
