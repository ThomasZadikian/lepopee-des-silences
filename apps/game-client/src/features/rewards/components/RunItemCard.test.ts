// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RunItemCard from './RunItemCard.vue';
import type { RunItemDto } from '../../runs/types/runTypes';

const baseItem: RunItemDto = {
  id: 'item-1',
  definitionKey: 'potion.soin',
  displayName: 'Fiole de sève',
  description: 'Une fiole translucide remplie de sève.',
  type: 'Consumable',
  rarity: 'Uncommon',
  quantity: 2,
  effectType: 'Heal',
  effectAmount: 25,
};

function mountCard(item: RunItemDto, compact = false) {
  return mount(RunItemCard, { props: { item, compact } });
}

describe('RunItemCard', () => {
  it('renders without crashing', () => {
    expect(mountCard(baseItem).exists()).toBe(true);
  });

  it('displays the item name', () => {
    expect(mountCard(baseItem).text()).toContain('Fiole de sève');
  });

  it('displays the quantity when > 1', () => {
    expect(mountCard(baseItem).text()).toContain('×2');
  });

  it('does not display quantity when exactly 1', () => {
    const wrapper = mountCard({ ...baseItem, quantity: 1 });
    expect(wrapper.find('.ric__qty').exists()).toBe(false);
  });

  it('displays the rarity label', () => {
    expect(mountCard(baseItem).text()).toContain('Peu commun');
  });

  it('displays the item type', () => {
    expect(mountCard(baseItem).text()).toContain('Consumable');
  });

  it('displays the description in normal mode', () => {
    const wrapper = mountCard(baseItem);
    expect(wrapper.find('.ric__desc').text()).toContain('sève');
  });

  it('hides the description in compact mode', () => {
    const wrapper = mountCard(baseItem, true);
    expect(wrapper.find('.ric__desc').exists()).toBe(false);
  });

  it('displays the effect badge when effectAmount > 0', () => {
    expect(mountCard(baseItem).find('.ric__effect-badge').exists()).toBe(true);
    expect(mountCard(baseItem).text()).toContain('+25 Vitalité');
  });

  it('hides the effect badge when effectAmount is 0', () => {
    const wrapper = mountCard({ ...baseItem, effectAmount: 0 });
    expect(wrapper.find('.ric__effect').exists()).toBe(false);
  });

  it('shows the use button only when isUsable is explicitly true', () => {
    const withUse = mountCard({ ...baseItem, isUsable: true });
    expect(withUse.find('.ric__use-btn').exists()).toBe(true);

    const withoutUse = mountCard({ ...baseItem, isUsable: false });
    expect(withoutUse.find('.ric__use-btn').exists()).toBe(false);

    const undefinedUse = mountCard({ ...baseItem, isUsable: undefined });
    expect(undefinedUse.find('.ric__use-btn').exists()).toBe(false);
  });

  it('emits use with the item id when the button is clicked', async () => {
    const wrapper = mountCard({ ...baseItem, isUsable: true });
    await wrapper.find('.ric__use-btn').trigger('click');
    const events = wrapper.emitted('use') as string[][];
    expect(events[0][0]).toBe('item-1');
  });

  it('applies sap rarity class for Uncommon', () => {
    expect(mountCard(baseItem).find('.ric--sap').exists()).toBe(true);
  });

  it('applies frost rarity class for Rare', () => {
    const wrapper = mountCard({ ...baseItem, rarity: 'Rare' });
    expect(wrapper.find('.ric--frost').exists()).toBe(true);
  });

  it('applies gold rarity class for Epic', () => {
    const wrapper = mountCard({ ...baseItem, rarity: 'Epic' });
    expect(wrapper.find('.ric--gold').exists()).toBe(true);
  });

  it('applies common class for Common rarity', () => {
    const wrapper = mountCard({ ...baseItem, rarity: 'Common' });
    expect(wrapper.find('.ric--common').exists()).toBe(true);
  });

  it('renders NarrativeFragment effect correctly', () => {
    const wrapper = mountCard({ ...baseItem, effectType: 'NarrativeFragment', effectAmount: 1 });
    expect(wrapper.text()).toContain('Fragment narratif');
  });

  it('renders without crashing when optional fields are missing', () => {
    const minimal: RunItemDto = {
      id: 'x',
      definitionKey: 'x',
      displayName: 'Objet',
      description: '',
      type: 'Passive',
      rarity: 'Common',
      quantity: 1,
      effectType: 'None',
      effectAmount: 0,
    };
    expect(mountCard(minimal).exists()).toBe(true);
  });
});
