// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ItemTargetPanel from './ItemTargetPanel.vue';
import type { CombatantRuntimeDto } from '../types/combatContracts';

function makeTarget(id: string, displayName: string, currentVitality: number, maxVitality: number): CombatantRuntimeDto {
  return {
    id,
    sourceKey: `source.${id}`,
    displayName,
    side: 'Player',
    archetype: 'Fighter',
    maxVitality,
    currentVitality,
    guard: 0,
    mana: 0,
    charge: 0,
    status: 'Active',
    skills: [],
  };
}

function mountPanel(itemName: string, side: 'ally' | 'enemy', targets: CombatantRuntimeDto[]) {
  return mount(ItemTargetPanel, { props: { itemName, side, targets } });
}

describe('ItemTargetPanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the item name', () => {
    const wrapper = mountPanel('Potion de soin', 'ally', []);
    expect(wrapper.find('.target-panel__sub').text()).toContain('Potion de soin');
  });

  it('shows ally-themed title', () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    expect(wrapper.find('.target-panel__title').text()).toContain('Soigner une voix');
  });

  it('shows enemy-themed title', () => {
    const wrapper = mountPanel('Poison', 'enemy', []);
    expect(wrapper.find('.target-panel__title').text()).toContain('Frapper une manifestation');
  });

  it('applies ally class for ally side', () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    expect(wrapper.find('.target-panel').classes()).toContain('target-panel--ally');
  });

  it('applies enemy class for enemy side', () => {
    const wrapper = mountPanel('Poison', 'enemy', []);
    expect(wrapper.find('.target-panel').classes()).toContain('target-panel--enemy');
  });

  it('renders one target row per target', () => {
    const targets = [
      makeTarget('t1', 'Hero', 80, 100),
      makeTarget('t2', 'Mage', 50, 100),
    ];
    const wrapper = mountPanel('Potion', 'ally', targets);
    expect(wrapper.findAll('.target-row').length).toBe(2);
  });

  it('displays target name', () => {
    const targets = [makeTarget('t1', 'Warrior', 100, 100)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    expect(wrapper.find('.target-row__name').text()).toBe('Warrior');
  });

  it('displays target archetype', () => {
    const targets = [makeTarget('t1', 'Hero', 100, 100)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    expect(wrapper.find('.target-row__arch').text()).toBe('Fighter');
  });

  it('displays target HP', () => {
    const targets = [makeTarget('t1', 'Hero', 75, 100)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    expect(wrapper.find('.target-row__hp').text()).toBe('75/100');
  });

  it('renders gauge fill with correct width', () => {
    const targets = [makeTarget('t1', 'Hero', 50, 100)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    const fill = wrapper.find('.target-row__gauge-fill');
    expect(fill.attributes('style')).toContain('50%');
  });

  it('shows empty state when no targets', () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    expect(wrapper.find('.target-panel__empty').exists()).toBe(true);
    expect(wrapper.text()).toContain('Aucune cible valide');
  });

  it('does not show empty state when targets exist', () => {
    const targets = [makeTarget('t1', 'Hero', 100, 100)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    expect(wrapper.find('.target-panel__empty').exists()).toBe(false);
  });

  it('emits select with target id on row click', async () => {
    const targets = [makeTarget('target-42', 'Hero', 100, 100)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    await wrapper.find('.target-row').trigger('click');
    const events = wrapper.emitted('select') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('target-42');
  });

  it('emits back on back button click', async () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    await wrapper.find('.target-panel__back').trigger('click');
    expect(wrapper.emitted('back')).toBeDefined();
  });

  it('handles zero maxVitality without crashing', () => {
    const targets = [makeTarget('t1', 'Hero', 0, 0)];
    const wrapper = mountPanel('Potion', 'ally', targets);
    expect(wrapper.exists()).toBe(true);
  });

  it('has listbox role', () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    expect(wrapper.find('.target-panel').attributes('role')).toBe('listbox');
  });

  it('has aria-label for ally side', () => {
    const wrapper = mountPanel('Potion', 'ally', []);
    expect(wrapper.find('.target-panel').attributes('aria-label')).toBe('Choisir une voix');
  });

  it('has aria-label for enemy side', () => {
    const wrapper = mountPanel('Poison', 'enemy', []);
    expect(wrapper.find('.target-panel').attributes('aria-label')).toBe('Choisir une manifestation');
  });
});
