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

  it('displays risk level', () => {
    const wrapper = mountDrawer(baseNode);
    expect(wrapper.text()).toContain('Élevé');
    expect(wrapper.text()).toContain('50/100');
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

  it('applies correct risk class for low risk', () => {
    const node = { ...baseNode, riskLevel: 10 };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.risk--low').exists()).toBe(true);
  });

  it('applies correct risk class for critical risk', () => {
    const node = { ...baseNode, riskLevel: 90 };
    const wrapper = mountDrawer(node);
    expect(wrapper.find('.risk--critical').exists()).toBe(true);
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
});
