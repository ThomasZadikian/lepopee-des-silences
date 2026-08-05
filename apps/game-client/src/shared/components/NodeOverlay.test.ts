// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import NodeOverlay from './NodeOverlay.vue';

describe('NodeOverlay', () => {
  it('renders the backdrop and the slot content', () => {
    const wrapper = mount(NodeOverlay, { slots: { default: '<p class="probe">Contenu</p>' } });
    expect(wrapper.find('.node-overlay__backdrop').exists()).toBe(true);
    expect(wrapper.find('.probe').text()).toBe('Contenu');
  });

  it('defaults to the card size', () => {
    const wrapper = mount(NodeOverlay);
    expect(wrapper.find('.node-overlay__stage--card').exists()).toBe(true);
  });

  it('applies the requested size variant', () => {
    const wrapper = mount(NodeOverlay, { props: { size: 'wide' } });
    expect(wrapper.find('.node-overlay__stage--wide').exists()).toBe(true);
  });
});
