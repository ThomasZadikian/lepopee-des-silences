// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import EliseComment from './EliseComment.vue';

function mountComment(tell?: string, slotContent = 'Comment text') {
  return mount(EliseComment, {
    props: { tell },
    slots: { default: slotContent },
    global: {
      stubs: {
        SigilIcon: { template: '<svg />' },
      },
    },
  });
}

describe('EliseComment', () => {
  it('renders without crashing', () => {
    const wrapper = mountComment();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays ELISE name', () => {
    const wrapper = mountComment();
    expect(wrapper.text()).toContain('ELISE');
  });

  it('displays slot content', () => {
    const wrapper = mountComment(undefined, 'This is a comment');
    expect(wrapper.text()).toContain('This is a comment');
  });

  it('displays tell when provided', () => {
    const wrapper = mountComment('à mi-voix');
    expect(wrapper.text()).toContain('à mi-voix');
  });

  it('does not display tell when not provided', () => {
    const wrapper = mountComment();
    expect(wrapper.text()).not.toContain('·');
  });

  it('renders SigilIcon', () => {
    const wrapper = mountComment();
    expect(wrapper.find('svg').exists()).toBe(true);
  });

  it('renders with empty tell', () => {
    const wrapper = mountComment('');
    expect(wrapper.exists()).toBe(true);
  });

  it('renders with empty slot content', () => {
    const wrapper = mountComment(undefined, '');
    expect(wrapper.exists()).toBe(true);
  });
});
