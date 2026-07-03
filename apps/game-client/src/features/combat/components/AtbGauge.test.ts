// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import AtbGauge from './AtbGauge.vue';

function mountGauge(gauge = 0, fillPerTick = 10, active = false) {
  return mount(AtbGauge, { props: { gauge, fillPerTick, active } });
}

describe('AtbGauge', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  it('renders without crashing', () => {
    const wrapper = mountGauge();
    expect(wrapper.exists()).toBe(true);
    expect(wrapper.find('.atb').exists()).toBe(true);
  });

  it('renders the fill bar', () => {
    const wrapper = mountGauge(25000);
    expect(wrapper.find('.atb__fill').exists()).toBe(true);
  });

  it('applies atb--ready class when gauge >= 50000', async () => {
    const wrapper = mountGauge(50000);
    expect(wrapper.find('.atb').classes()).toContain('atb--ready');
  });

  it('does not apply atb--ready class when gauge < 50000', () => {
    const wrapper = mountGauge(40000);
    expect(wrapper.find('.atb').classes()).not.toContain('atb--ready');
  });

  it('applies atb--active class when active prop is true', () => {
    const wrapper = mountGauge(0, 10, true);
    expect(wrapper.find('.atb').classes()).toContain('atb--active');
  });

  it('does not apply atb--active class when active prop is false', () => {
    const wrapper = mountGauge(0, 10, false);
    expect(wrapper.find('.atb').classes()).not.toContain('atb--active');
  });

  it('renders spark element when ready', () => {
    const wrapper = mountGauge(50000);
    expect(wrapper.find('.atb__spark').exists()).toBe(true);
  });

  it('does not render spark element when not ready', () => {
    const wrapper = mountGauge(30000);
    expect(wrapper.find('.atb__spark').exists()).toBe(false);
  });

  it('cleans up animation frame on unmount', () => {
    const wrapper = mountGauge();
    const cancelSpy = vi.spyOn(window, 'cancelAnimationFrame');
    wrapper.unmount();
    expect(cancelSpy).toHaveBeenCalled();
  });
});
