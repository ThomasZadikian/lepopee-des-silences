// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PalaceLawPanel from './PalaceLawPanel.vue';
import type { ActivePalaceLawDto } from '../runs/types/runTypes';

function mountPanel(laws?: ActivePalaceLawDto[]) {
  return mount(PalaceLawPanel, { props: { laws } });
}

describe('PalaceLawPanel', () => {
  const baseLaw: ActivePalaceLawDto = {
    key: 'law-aegis-v1',
    displayName: 'Loi d\'Aegis',
    domain: 'Combat',
    version: 'v1',
    description: 'Protège les alliés.',
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the header', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('Registre du Palais');
    expect(wrapper.text()).toContain('Lois en vigueur');
  });

  it('shows empty state when no laws', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('Aucune loi active.');
    expect(wrapper.text()).toContain("Le Palais n'a encore édicté aucune loi.");
  });

  it('shows empty state when laws array is empty', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain('Aucune loi active.');
  });

  it('displays law count', () => {
    const wrapper = mountPanel([baseLaw]);
    expect(wrapper.text()).toContain('1 loi régissent ta descente.');
  });

  it('displays plural for multiple laws', () => {
    const laws = [
      baseLaw,
      { ...baseLaw, key: 'law-2', displayName: 'Loi 2' },
    ];
    const wrapper = mountPanel(laws);
    expect(wrapper.text()).toContain('2 lois régissent ta descente.');
  });

  it('displays law name', () => {
    const wrapper = mountPanel([baseLaw]);
    expect(wrapper.text()).toContain('Loi d\'Aegis');
  });

  it('displays law version', () => {
    const wrapper = mountPanel([baseLaw]);
    expect(wrapper.text()).toContain('v1');
  });

  it('displays law domain chip', () => {
    const wrapper = mountPanel([baseLaw]);
    expect(wrapper.text()).toContain('Combat');
  });

  it('displays law description', () => {
    const wrapper = mountPanel([baseLaw]);
    expect(wrapper.text()).toContain('Protège les alliés.');
  });

  it('applies blood tone for combat domain', () => {
    const law = { ...baseLaw, domain: 'Confrontation' };
    const wrapper = mountPanel([law]);
    const chip = wrapper.find('.plp-law .es-chip');
    expect(chip.classes()).toContain('es-chip--blood');
  });

  it('applies frost tone for memory domain', () => {
    const law = { ...baseLaw, domain: 'Memory' };
    const wrapper = mountPanel([law]);
    const chip = wrapper.find('.plp-law .es-chip');
    expect(chip.classes()).toContain('es-chip--frost');
  });

  it('applies gold tone for law domain', () => {
    const law = { ...baseLaw, domain: 'Loi' };
    const wrapper = mountPanel([law]);
    const chip = wrapper.find('.plp-law .es-chip');
    expect(chip.classes()).toContain('es-chip--gold');
  });

  it('handles missing domain gracefully', () => {
    const law = { ...baseLaw, domain: undefined };
    const wrapper = mountPanel([law]);
    expect(wrapper.exists()).toBe(true);
  });

  it('handles null domain gracefully', () => {
    const law = { ...baseLaw, domain: null };
    const wrapper = mountPanel([law]);
    expect(wrapper.exists()).toBe(true);
  });

  it('handles undefined laws prop', () => {
    const wrapper = mountPanel(undefined);
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.text()).toContain('Aucune loi active.');
  });

  it('renders multiple laws', () => {
    const laws = [
      baseLaw,
      { ...baseLaw, key: 'law-2', displayName: 'Loi 2', domain: 'Narratif' },
      { ...baseLaw, key: 'law-3', displayName: 'Loi 3', domain: 'Édit' },
    ];
    const wrapper = mountPanel(laws);
    expect(wrapper.findAll('.plp-law').length).toBe(3);
  });
});
