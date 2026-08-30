// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import ChipBadge from './ChipBadge.vue';

function mountBadge(tone?: 'gold' | 'frost' | 'blood' | 'sap' | null, slotContent = 'Test') {
  return mount(ChipBadge, {
    props: { tone },
    slots: { default: slotContent },
  });
}

describe('ChipBadge', () => {
  it('renders without crashing', () => {
    const wrapper = mountBadge();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays slot content', () => {
    const wrapper = mountBadge(undefined, 'Hello');
    expect(wrapper.text()).toContain('Hello');
  });

  it('applies gold tone class', () => {
    const wrapper = mountBadge('gold');
    expect(wrapper.find('.es-chip--gold').exists()).toBe(true);
  });

  it('applies frost tone class', () => {
    const wrapper = mountBadge('frost');
    expect(wrapper.find('.es-chip--frost').exists()).toBe(true);
  });

  it('applies blood tone class', () => {
    const wrapper = mountBadge('blood');
    expect(wrapper.find('.es-chip--blood').exists()).toBe(true);
  });

  it('applies sap tone class', () => {
    const wrapper = mountBadge('sap');
    expect(wrapper.find('.es-chip--sap').exists()).toBe(true);
  });

  it('does not apply tone class when tone is null', () => {
    const wrapper = mountBadge(null);
    expect(wrapper.classes()).toContain('es-chip');
    expect(wrapper.classes()).not.toContain('es-chip--gold');
    expect(wrapper.classes()).not.toContain('es-chip--frost');
    expect(wrapper.classes()).not.toContain('es-chip--blood');
    expect(wrapper.classes()).not.toContain('es-chip--sap');
  });

  it('does not apply tone class when tone is undefined', () => {
    const wrapper = mountBadge(undefined);
    expect(wrapper.classes()).toContain('es-chip');
    expect(wrapper.classes().some((c: string) => c.startsWith('es-chip--'))).toBe(false);
  });

  it('renders as span element', () => {
    const wrapper = mountBadge();
    expect(wrapper.element.tagName).toBe('SPAN');
  });
});
