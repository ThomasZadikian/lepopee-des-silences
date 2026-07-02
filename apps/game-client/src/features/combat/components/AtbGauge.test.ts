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

  it('applies atb--charging class when gauge overflows READY', () => {
    const wrapper = mountGauge(55000);
    expect(wrapper.find('.atb').classes()).toContain('atb--charging');
  });

  it('applies atb--max class when gauge >= READY + MAX_OVERFLOW', () => {
    const wrapper = mountGauge(100000);
    expect(wrapper.find('.atb').classes()).toContain('atb--max');
  });

  it('applies atb--active class when active prop is true', () => {
    const wrapper = mountGauge(0, 10, true);
    expect(wrapper.find('.atb').classes()).toContain('atb--active');
  });

  it('does not apply atb--active class when active prop is false', () => {
    const wrapper = mountGauge(0, 10, false);
    expect(wrapper.find('.atb').classes()).not.toContain('atb--active');
  });

  it('renders charge overflow bar when gauge > READY', () => {
    const wrapper = mountGauge(60000);
    expect(wrapper.find('.atb__charge').exists()).toBe(true);
  });

  it('does not render charge overflow bar when gauge <= READY', () => {
    const wrapper = mountGauge(40000);
    expect(wrapper.find('.atb__charge').exists()).toBe(false);
  });

  it('renders spark element when ready', () => {
    const wrapper = mountGauge(50000);
    expect(wrapper.find('.atb__spark').exists()).toBe(true);
  });

  it('does not render spark element when not ready', () => {
    const wrapper = mountGauge(30000);
    expect(wrapper.find('.atb__spark').exists()).toBe(false);
  });

  it('displays charge multiplier when gauge exceeds READY', () => {
    const wrapper = mountGauge(55000);
    expect(wrapper.find('.atb-gauge__charge').exists()).toBe(true);
  });

  it('does not display charge multiplier when gauge is below READY', () => {
    const wrapper = mountGauge(40000);
    expect(wrapper.find('.atb-gauge__charge').exists()).toBe(false);
  });

  it('shows ×1.15 charge multiplier at low overflow', () => {
    const wrapper = mountGauge(51000);
    expect(wrapper.find('.atb-gauge__charge').text()).toContain('×1.15');
  });

  it('shows ×1.30 charge multiplier at mid overflow', () => {
    const wrapper = mountGauge(54000);
    expect(wrapper.find('.atb-gauge__charge').text()).toContain('×1.30');
  });

  it('shows ×1.5 charge multiplier at high overflow', () => {
    const wrapper = mountGauge(65000);
    expect(wrapper.find('.atb-gauge__charge').text()).toContain('×1.5');
  });

  it('is atb--charging, not atb--max, just below the 10,000 overflow cap', () => {
    const wrapper = mountGauge(59999);
    expect(wrapper.find('.atb').classes()).toContain('atb--charging');
    expect(wrapper.find('.atb').classes()).not.toContain('atb--max');
  });

  it('is atb--max exactly at the 10,000 overflow cap (READY + MaxChargeOverflow)', () => {
    const wrapper = mountGauge(60000);
    expect(wrapper.find('.atb').classes()).toContain('atb--max');
  });

  it('renders a title attribute on the charge label describing the damage bonus', () => {
    const wrapper = mountGauge(55000);
    const label = wrapper.find('.atb-gauge__charge');
    expect(label.attributes('title')).toContain('×1.30');
    expect(label.attributes('title')).toContain('Surcharge');
  });

  it('applies the --max modifier class to the charge label at ×1.5', () => {
    const wrapper = mountGauge(65000);
    expect(wrapper.find('.atb-gauge__charge').classes()).toContain('atb-gauge__charge--max');
  });

  it('does not apply the --max modifier class to the charge label below ×1.5', () => {
    const wrapper = mountGauge(51000);
    expect(wrapper.find('.atb-gauge__charge').classes()).not.toContain('atb-gauge__charge--max');
  });

  it('cleans up animation frame on unmount', () => {
    const wrapper = mountGauge();
    const cancelSpy = vi.spyOn(window, 'cancelAnimationFrame');
    wrapper.unmount();
    expect(cancelSpy).toHaveBeenCalled();
  });
});
