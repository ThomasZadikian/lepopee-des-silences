// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import StatTooltip from './StatTooltip.vue';

describe('StatTooltip', () => {
  it('renders the slot content', () => {
    const wrapper = mount(StatTooltip, {
      props: { text: 'Une description.' },
      slots: { default: '<span class="label">Vitesse</span>' },
    });

    expect(wrapper.find('.label').text()).toBe('Vitesse');
  });

  it('renders the tooltip text in a bubble', () => {
    const wrapper = mount(StatTooltip, {
      props: { text: 'Augmente les chances de coup critique.' },
      slots: { default: 'Focus' },
    });

    expect(wrapper.find('.stat-tooltip__bubble').text()).toBe(
      'Augmente les chances de coup critique.',
    );
  });

  it('defaults to top placement', () => {
    const wrapper = mount(StatTooltip, { props: { text: 'desc' }, slots: { default: 'Stat' } });
    expect(wrapper.classes()).toContain('stat-tooltip--top');
  });

  it('applies bottom placement when requested', () => {
    const wrapper = mount(StatTooltip, {
      props: { text: 'desc', placement: 'bottom' },
      slots: { default: 'Stat' },
    });
    expect(wrapper.classes()).toContain('stat-tooltip--bottom');
  });
});
