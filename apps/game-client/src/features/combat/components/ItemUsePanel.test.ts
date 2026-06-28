// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ItemUsePanel from './ItemUsePanel.vue';
import type { CombatUsableItemDto } from '../types/combatContracts';

function makeItem(overrides: Partial<CombatUsableItemDto> = {}): CombatUsableItemDto {
  return {
    itemId: 'item-1',
    definitionKey: 'def.potion',
    displayName: 'Potion de soin',
    effectType: 'Heal',
    effectAmount: 20,
    quantity: 3,
    targetingType: 'Self',
    ...overrides,
  };
}

function mountPanel(items: CombatUsableItemDto[], selectedItemId: string | null = null) {
  return mount(ItemUsePanel, { props: { items, selectedItemId } });
}

describe('ItemUsePanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the panel header', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('La Besace');
  });

  it('renders one item row per item', () => {
    const items = [makeItem(), makeItem({ itemId: 'item-2', displayName: 'Antidote' })];
    const wrapper = mountPanel(items);
    expect(wrapper.findAll('.item-row').length).toBe(2);
  });

  it('displays item name', () => {
    const items = [makeItem({ displayName: 'Élixir' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__name').text()).toBe('Élixir');
  });

  it('displays item quantity', () => {
    const items = [makeItem({ quantity: 5 })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__qty').text()).toBe('×5');
  });

  it('shows heal effect label', () => {
    const items = [makeItem({ effectType: 'Heal', effectAmount: 30 })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('+30 PV');
  });

  it('shows guard effect label', () => {
    const items = [makeItem({ effectType: 'Guard', effectAmount: 15 })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('+15 Garde');
  });

  it('shows mana restore effect label', () => {
    const items = [makeItem({ effectType: 'ManaRestore', effectAmount: 10 })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('+10 Mana');
  });

  it('shows charge restore effect label', () => {
    const items = [makeItem({ effectType: 'ChargeRestore', effectAmount: 2 })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('+2 Charge');
  });

  it('shows damage effect label', () => {
    const items = [makeItem({ effectType: 'Damage', effectAmount: 25 })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('−25 PV');
  });

  it('shows Self target label', () => {
    const items = [makeItem({ targetingType: 'Self' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('Soi');
  });

  it('shows SingleAlly target label', () => {
    const items = [makeItem({ targetingType: 'SingleAlly' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__meta').text()).toContain('Une voix');
  });

  it('applies selected class when item is selected', () => {
    const items = [makeItem({ itemId: 'selected-item' })];
    const wrapper = mountPanel(items, 'selected-item');
    expect(wrapper.find('.item-row').classes()).toContain('item-row--selected');
  });

  it('does not apply selected class when item is not selected', () => {
    const items = [makeItem({ itemId: 'other-item' })];
    const wrapper = mountPanel(items, 'different-item');
    expect(wrapper.find('.item-row').classes()).not.toContain('item-row--selected');
  });

  it('applies offensive class for damage items', () => {
    const items = [makeItem({ effectType: 'Damage' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row').classes()).toContain('item-row--offensive');
  });

  it('does not apply offensive class for non-damage items', () => {
    const items = [makeItem({ effectType: 'Heal' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row').classes()).not.toContain('item-row--offensive');
  });

  it('emits select with item id on click', async () => {
    const items = [makeItem({ itemId: 'potion-1' })];
    const wrapper = mountPanel(items);
    await wrapper.find('.item-row').trigger('click');
    const events = wrapper.emitted('select') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('potion-1');
  });

  it('emits close on close button click', async () => {
    const wrapper = mountPanel([]);
    await wrapper.find('.item-panel__close').trigger('click');
    expect(wrapper.emitted('close')).toBeDefined();
  });

  it('sets aria-selected correctly for selected item', () => {
    const items = [makeItem({ itemId: 'sel' })];
    const wrapper = mountPanel(items, 'sel');
    expect(wrapper.find('.item-row').attributes('aria-selected')).toBe('true');
  });

  it('sets aria-selected false for non-selected item', () => {
    const items = [makeItem({ itemId: 'not-sel' })];
    const wrapper = mountPanel(items, 'other');
    expect(wrapper.find('.item-row').attributes('aria-selected')).toBe('false');
  });

  it('has listbox role', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.find('.item-panel').attributes('role')).toBe('listbox');
  });

  it('renders offensive glyph for damage items', () => {
    const items = [makeItem({ effectType: 'Damage' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__glyph').text()).toBe('✶');
  });

  it('renders default glyph for non-damage items', () => {
    const items = [makeItem({ effectType: 'Heal' })];
    const wrapper = mountPanel(items);
    expect(wrapper.find('.item-row__glyph').text()).toBe('◆');
  });
});
