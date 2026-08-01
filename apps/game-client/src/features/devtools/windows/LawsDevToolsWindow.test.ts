// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import LawsDevToolsWindow from './LawsDevToolsWindow.vue';
import type { PalaceLawDefinitionView } from '../../palace-laws/types/lawTypes';

const allLaws: PalaceLawDefinitionView[] = [
  {
    key: 'law-aegis-v1', name: 'Aegis', description: 'Une loi de protection.',
    rarity: 'Rare', polarity: 'Positif', isMajeure: true, impactDomains: ['Combat'],
  },
  {
    key: 'law-tribut-v1', name: 'Tribut', description: 'Une loi de taxation.',
    rarity: 'Commun', polarity: 'Negatif', isMajeure: false, impactDomains: ['Economie'],
  },
];

function mountWindow(disabled = false, isLoading = false) {
  return mount(LawsDevToolsWindow, { props: { disabled, isLoading, allLaws } });
}

describe('LawsDevToolsWindow', () => {
  it('lists every law in the catalog grid', () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).toContain('Aegis');
    expect(wrapper.text()).toContain('Tribut');
  });

  it('filters laws by search query', async () => {
    const wrapper = mountWindow();
    await wrapper.find('input.devtools-input').setValue('aegis');
    expect(wrapper.text()).toContain('Aegis');
    expect(wrapper.text()).not.toContain('Tribut');
  });

  it('shows the description sheet only once a law is selected', async () => {
    const wrapper = mountWindow();
    expect(wrapper.text()).not.toContain('Une loi de protection.');
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Aegis'));
    await cell!.trigger('click');
    expect(wrapper.text()).toContain('Une loi de protection.');
  });

  it('emits activateLaw with the selected key', async () => {
    const wrapper = mountWindow();
    const cell = wrapper.findAll('.devtools-catalog-cell').find((c) => c.text().includes('Aegis'));
    await cell!.trigger('click');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activer cette loi'));
    await btn!.trigger('click');
    expect(wrapper.emitted('activateLaw')).toEqual([['law-aegis-v1']]);
  });

  it('shows nothing to activate when no law is selected', () => {
    const wrapper = mountWindow();
    expect(wrapper.findAll('button').find((b) => b.text().includes('Activer cette loi'))).toBeUndefined();
  });

  it('emits clearLaws on confirm', async () => {
    const wrapper = mountWindow();
    const originalConfirm = globalThis.window.confirm;
    globalThis.window.confirm = vi.fn(() => true);

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Effacer'));
    await btn!.trigger('click');

    expect(wrapper.emitted('clearLaws')).toHaveLength(1);
    globalThis.window.confirm = originalConfirm;
  });
});
