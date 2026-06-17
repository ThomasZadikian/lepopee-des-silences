// @vitest-environment jsdom
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

import RoomClimateEffects from '../features/room-climate/RoomClimateEffects.vue';

describe('RoomClimateEffects', () => {
  it('does not render an overlay when no climate is active', () => {
    const wrapper = mount(RoomClimateEffects, { props: { climate: null } });

    expect(wrapper.find('[data-climate-effect]').exists()).toBe(false);
  });

  it('renders rain particles for Rain climate', () => {
    const wrapper = mount(RoomClimateEffects, { props: { climate: 'Rain' } });

    expect(wrapper.find('[data-climate-effect="Rain"]').exists()).toBe(true);
    expect(wrapper.find('.rce-root--rain').exists()).toBe(true);
    expect(wrapper.findAll('.rce-rain-drop').length).toBe(42);
  });

  it('renders heatwave atmosphere from DTO type', () => {
    const wrapper = mount(RoomClimateEffects, {
      props: { climate: { type: 'Heatwave', displayName: 'Canicule' } },
    });

    expect(wrapper.find('[data-climate-effect="Heatwave"]').exists()).toBe(true);
    expect(wrapper.find('.rce-root--heatwave').exists()).toBe(true);
    expect(wrapper.find('.rce-sun').exists()).toBe(true);
    expect(wrapper.find('.rce-heat-shimmer').exists()).toBe(true);
  });

  it('renders grey and hail variants without exposing gameplay coefficients', () => {
    const greyWrapper = mount(RoomClimateEffects, { props: { climate: 'Grey' } });
    const hailWrapper = mount(RoomClimateEffects, { props: { climate: 'Hail' } });

    expect(greyWrapper.find('.rce-root--grey').exists()).toBe(true);
    expect(greyWrapper.findAll('.rce-grey-wisp').length).toBe(9);
    expect(hailWrapper.find('.rce-root--hail').exists()).toBe(true);
    expect(hailWrapper.findAll('.rce-hail-stone').length).toBe(28);
    expect(greyWrapper.text()).not.toContain('0.9');
    expect(hailWrapper.text()).not.toContain('25%');
  });

  it('ignores unknown climate types', () => {
    const wrapper = mount(RoomClimateEffects, { props: { climate: 'Fog' } });

    expect(wrapper.find('[data-climate-effect]').exists()).toBe(false);
  });
});
