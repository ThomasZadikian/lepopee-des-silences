// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import EliseOverlay from './EliseOverlay.vue';

function mountOverlay(message?: string | null) {
  return mount(EliseOverlay, { props: { message } });
}

describe('EliseOverlay', () => {
  it('renders without crashing with message', () => {
    const wrapper = mountOverlay('Attention, le Palais observe.');
    expect(wrapper.exists()).toBe(true);
  });

  it('does not render when message is null', () => {
    const wrapper = mountOverlay(null);
    expect(wrapper.find('.elise-overlay').exists()).toBe(false);
  });

  it('does not render when message is undefined', () => {
    const wrapper = mountOverlay(undefined);
    expect(wrapper.find('.elise-overlay').exists()).toBe(false);
  });

  it('displays the message text', () => {
    const wrapper = mountOverlay('Attention, le Palais observe.');
    expect(wrapper.text()).toContain('Attention, le Palais observe.');
  });

  it('displays Elise name', () => {
    const wrapper = mountOverlay('Test message');
    expect(wrapper.text()).toContain('Elise');
  });

  it('does not render when message is empty string', () => {
    const wrapper = mountOverlay('');
    expect(wrapper.find('.elise-overlay').exists()).toBe(false);
  });

  it('renders corner decorations', () => {
    const wrapper = mountOverlay('Test');
    expect(wrapper.findAll('.es-corner').length).toBe(4);
  });

  it('applies frost corner class', () => {
    const wrapper = mountOverlay('Test');
    const corners = wrapper.findAll('.es-corner--frost');
    expect(corners.length).toBe(4);
  });
});
