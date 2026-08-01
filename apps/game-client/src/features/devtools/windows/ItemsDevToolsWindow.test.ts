// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ItemsDevToolsWindow from './ItemsDevToolsWindow.vue';
import type { ItemDefinitionView } from '../../party/types/itemTypes';

const allItems: ItemDefinitionView[] = [
  {
    key: 'item.the-seuil', displayName: 'Thé du seuil', description: 'Restaure 25% de Vitalité.',
    category: 'Consumable', itemType: 'Consumable', rarity: 'Common', effectRunType: 'HealPercent', effectValue: 25,
  },
  {
    key: 'item.sceau-invite', displayName: "Sceau de l'invité reconnu", description: 'Accorde de la réputation.',
    category: 'Heritage', itemType: 'MetaPassive', rarity: 'Epic', effectRunType: null, effectValue: 0,
  },
];

function mountWindow(disabled = false, isLoading = false) {
  return mount(ItemsDevToolsWindow, { props: { disabled, isLoading, allItems } });
}

describe('ItemsDevToolsWindow', () => {
  it('lists every item in the catalog grid', () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).toContain('Thé du seuil');
    expect(wrapper.text()).toContain("Sceau de l'invité reconnu");
  });

  it('filters items by search query', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input.devtools-input').setValue('sceau');
    expect(wrapper.text()).toContain('Sceau');
    expect(wrapper.text()).not.toContain('Thé du seuil');
  });

  it('shows the description sheet only once an item is selected', async () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).not.toContain('Restaure 25% de Vitalité.');
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Thé du seuil'));
    await cell!.trigger('click');
    expect(wrapper.text()).toContain('Restaure 25% de Vitalité.');
  });

  it('emits addItem with the selected key and default quantity', async () => {
    const wrapper = mountWindow();
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Thé du seuil'));
    await cell!.trigger('click');

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Ajouter'));
    await btn!.trigger('click');

    expect(wrapper.emitted('addItem')).toEqual([['item.the-seuil', 1]]);
  });

  it('emits addItem with the chosen quantity, clamped to [1,99]', async () => {
    const wrapper = mountWindow();
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Thé du seuil'));
    await cell!.trigger('click');

    await wrapper.find('input[type="number"]').setValue(150);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Ajouter'));
    await btn!.trigger('click');

    expect(wrapper.emitted('addItem')).toEqual([['item.the-seuil', 99]]);
  });

  it('shows nothing to add when no item is selected', () => {
    const wrapper = mountWindow();
    expect(wrapper.findAll('button').find((b) => b.text().includes('Ajouter'))).toBeUndefined();
  });
});
