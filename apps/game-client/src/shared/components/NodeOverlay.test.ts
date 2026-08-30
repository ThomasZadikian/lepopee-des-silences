// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import NodeOverlay from './NodeOverlay.vue';

beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('NodeOverlay', () => {
  it('shows the backdrop immediately but withholds the content until it has finished darkening', async () => {
    const wrapper = mount(NodeOverlay, { slots: { default: '<p class="probe">Contenu</p>' } });

    expect(wrapper.find('.node-overlay__backdrop').exists()).toBe(true);
    expect(wrapper.find('.probe').exists()).toBe(false);

    vi.runAllTimers();
    await wrapper.vm.$nextTick();

    expect(wrapper.find('.probe').text()).toBe('Contenu');
  });

  it('defaults to the card size once the content appears', async () => {
    const wrapper = mount(NodeOverlay);
    vi.runAllTimers();
    await wrapper.vm.$nextTick();

    expect(wrapper.find('.node-overlay__stage--card').exists()).toBe(true);
  });

  it('applies the requested size variant once the content appears', async () => {
    const wrapper = mount(NodeOverlay, { props: { size: 'wide' } });
    vi.runAllTimers();
    await wrapper.vm.$nextTick();

    expect(wrapper.find('.node-overlay__stage--wide').exists()).toBe(true);
  });
});
