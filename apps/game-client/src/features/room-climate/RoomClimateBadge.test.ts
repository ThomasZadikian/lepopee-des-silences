// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RoomClimateBadge from './RoomClimateBadge.vue';
import type { RoomClimateStateDto } from '../runs/types/runTypes';

function mountBadge(climate?: RoomClimateStateDto | null) {
  return mount(RoomClimateBadge, { props: { climate } });
}

describe('RoomClimateBadge', () => {
  it('renders without crashing', () => {
    const wrapper = mountBadge();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays "Aucun" when no climate', () => {
    const wrapper = mountBadge(null);
    expect(wrapper.text()).toContain('Aucun');
  });

  it('displays climate display name when active', () => {
    const climate: RoomClimateStateDto = { type: 'Rain', displayName: 'Pluie fine' };
    const wrapper = mountBadge(climate);
    expect(wrapper.text()).toContain('Pluie fine');
  });

  it('emits open when clicked', async () => {
    const wrapper = mountBadge();
    await wrapper.find('.rcb-root').trigger('click');
    expect(wrapper.emitted('open')).toHaveLength(1);
  });

  it('applies empty class when no climate', () => {
    const wrapper = mountBadge(null);
    expect(wrapper.find('.rcb-root--empty').exists()).toBe(true);
  });

  it('does not apply empty class when climate exists', () => {
    const climate: RoomClimateStateDto = { type: 'Rain' };
    const wrapper = mountBadge(climate);
    expect(wrapper.find('.rcb-root--empty').exists()).toBe(false);
  });

  it('shows tooltip with climate name', () => {
    const climate: RoomClimateStateDto = { type: 'Rain', displayName: 'Pluie' };
    const wrapper = mountBadge(climate);
    expect(wrapper.find('.rcb-root').attributes('title')).toContain('Pluie');
  });

  it('shows tooltip with no climate message', () => {
    const wrapper = mountBadge(null);
    expect(wrapper.find('.rcb-root').attributes('title')).toContain('Aucun climat actif');
  });

  it('displays Climat label', () => {
    const wrapper = mountBadge();
    expect(wrapper.text()).toContain('Climat');
  });

  it('handles string climate type', () => {
    const climate = 'Rain' as any;
    const wrapper = mountBadge(climate);
    expect(wrapper.exists()).toBe(true);
  });

  it('handles climate with only type', () => {
    const climate: RoomClimateStateDto = { type: 'Heatwave' };
    const wrapper = mountBadge(climate);
    expect(wrapper.text()).toContain('Canicule');
  });
});
