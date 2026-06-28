// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PsycheDevToolsSection from './PsycheDevToolsSection.vue';
import type { DevToolsRunPsycheResponse } from '../types/devToolsTypes';

function mountSection(
  psyche: DevToolsRunPsycheResponse | null = null,
  disabled = false,
  isLoading = false,
) {
  return mount(PsycheDevToolsSection, {
    props: { psyche, disabled, isLoading },
  });
}

describe('PsycheDevToolsSection', () => {
  const basePsyche: DevToolsRunPsycheResponse = {
    dominant: 'Calm',
    current: {
      Calm: 0.5,
      Wary: 0.3,
      Withdrawn: 0.2,
    },
    trajectory: [
      {
        depth: 1,
        dominant: 'Wary',
        distribution: { Wary: 0.6, Calm: 0.4 },
      },
      {
        depth: 2,
        dominant: 'Calm',
        distribution: { Calm: 0.7, Withdrawn: 0.3 },
      },
    ],
  };

  it('renders without crashing', () => {
    const wrapper = mountSection();
    expect(wrapper.exists()).toBe(true);
  });

  it('shows muted message when no psyche data', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Aucune donnée');
  });

  it('shows refresh button', () => {
    const wrapper = mountSection(basePsyche);
    expect(wrapper.text()).toContain('Rafraîchir');
  });

  it('emits refresh when button is clicked', async () => {
    const wrapper = mountSection(basePsyche);
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('refresh')).toHaveLength(1);
  });

  it('displays dominant emotion', () => {
    const wrapper = mountSection(basePsyche);
    expect(wrapper.text()).toContain('Calm');
  });

  it('displays emotion bars', () => {
    const wrapper = mountSection(basePsyche);
    expect(wrapper.text()).toContain('Calm');
    expect(wrapper.text()).toContain('Wary');
    expect(wrapper.text()).toContain('Withdrawn');
  });

  it('displays trajectory', () => {
    const wrapper = mountSection(basePsyche);
    expect(wrapper.text()).toContain('Trajectoire');
    expect(wrapper.text()).toContain('#1');
    expect(wrapper.text()).toContain('#2');
  });

  it('displays trajectory dominant per step', () => {
    const wrapper = mountSection(basePsyche);
    expect(wrapper.text()).toContain('Wary');
    expect(wrapper.text()).toContain('Calm');
  });

  it('disables refresh button when disabled', () => {
    const wrapper = mountSection(basePsyche, true);
    const btn = wrapper.find('button');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables refresh button when loading', () => {
    const wrapper = mountSection(basePsyche, false, true);
    const btn = wrapper.find('button');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('sorts emotions by value descending', () => {
    const wrapper = mountSection(basePsyche);
    const bars = wrapper.findAll('.devtools-psyche__bars li');
    // First bar should be Calm (0.5)
    expect(bars[0].text()).toContain('Calm');
  });

  it('displays percentage values', () => {
    const wrapper = mountSection(basePsyche);
    expect(wrapper.text()).toContain('50%');
    expect(wrapper.text()).toContain('30%');
    expect(wrapper.text()).toContain('20%');
  });

  it('handles empty trajectory', () => {
    const psyche = { ...basePsyche, trajectory: [] };
    const wrapper = mountSection(psyche);
    expect(wrapper.exists()).toBe(true);
  });

  it('handles empty current distribution', () => {
    const psyche = { ...basePsyche, current: {} };
    const wrapper = mountSection(psyche);
    expect(wrapper.exists()).toBe(true);
  });
});
