// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PsycheDevToolsWindow from './PsycheDevToolsWindow.vue';
import type { DevToolsRunPsycheResponse } from '../types/devToolsTypes';

const psyche: DevToolsRunPsycheResponse = {
  runId: 'run-1',
  dominant: 'Calm',
  current: { Calm: 0.6, Wary: 0.4 },
  trajectory: [
    { depth: 1, dominant: 'Calm', distribution: { Calm: 0.6, Wary: 0.4 } },
  ],
};

describe('PsycheDevToolsWindow', () => {
  it('shows a placeholder when there is no data', () => {
    const wrapper = mount(PsycheDevToolsWindow, { props: { disabled: false, isLoading: false, psyche: null } });
    expect(wrapper.text()).toContain('Aucune donnée');
  });

  it('renders the dominant emotion and distribution once data is present', () => {
    const wrapper = mount(PsycheDevToolsWindow, { props: { disabled: false, isLoading: false, psyche } });
    expect(wrapper.text()).toContain('Calm');
    expect(wrapper.text()).toContain('60%');
  });

  it('emits refresh when the button is clicked', async () => {
    const wrapper = mount(PsycheDevToolsWindow, { props: { disabled: false, isLoading: false, psyche: null } });
    await wrapper.find('button').trigger('click');
    expect(wrapper.emitted('refresh')).toHaveLength(1);
  });
});
