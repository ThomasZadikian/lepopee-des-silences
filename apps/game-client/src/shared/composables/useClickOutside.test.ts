// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { defineComponent, h, ref } from 'vue';
import { mount } from '@vue/test-utils';
import { useClickOutside } from './useClickOutside';

function mouseDownOn(el: Element) {
  el.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
}

describe('useClickOutside', () => {
  it('calls the callback when mousedown lands outside the target', () => {
    const onOutside = vi.fn();
    const Comp = defineComponent({
      setup() {
        const boxRef = ref<HTMLElement | null>(null);
        useClickOutside(boxRef, onOutside);
        return () => h('div', [h('div', { ref: boxRef, class: 'inside' }), h('div', { class: 'outside' })]);
      },
    });
    const wrapper = mount(Comp, { attachTo: document.body });

    mouseDownOn(wrapper.find('.outside').element);
    expect(onOutside).toHaveBeenCalledTimes(1);

    wrapper.unmount();
  });

  it('does not call the callback when mousedown lands inside the target', () => {
    const onOutside = vi.fn();
    const Comp = defineComponent({
      setup() {
        const boxRef = ref<HTMLElement | null>(null);
        useClickOutside(boxRef, onOutside);
        return () => h('div', [h('div', { ref: boxRef, class: 'inside' }, [h('span', { class: 'child' })])]);
      },
    });
    const wrapper = mount(Comp, { attachTo: document.body });

    mouseDownOn(wrapper.find('.child').element);
    expect(onOutside).not.toHaveBeenCalled();

    wrapper.unmount();
  });

  it('ignores clicks matching an ignoreSelectors entry', () => {
    const onOutside = vi.fn();
    const Comp = defineComponent({
      setup() {
        const boxRef = ref<HTMLElement | null>(null);
        useClickOutside(boxRef, onOutside, { ignoreSelectors: ['.trigger'] });
        return () => h('div', [
          h('div', { ref: boxRef, class: 'inside' }),
          h('button', { class: 'trigger' }),
        ]);
      },
    });
    const wrapper = mount(Comp, { attachTo: document.body });

    mouseDownOn(wrapper.find('.trigger').element);
    expect(onOutside).not.toHaveBeenCalled();

    wrapper.unmount();
  });

  it('stops listening after unmount', () => {
    const onOutside = vi.fn();
    const Comp = defineComponent({
      setup() {
        const boxRef = ref<HTMLElement | null>(null);
        useClickOutside(boxRef, onOutside);
        return () => h('div', [h('div', { ref: boxRef, class: 'inside' }), h('div', { class: 'outside' })]);
      },
    });
    const wrapper = mount(Comp, { attachTo: document.body });
    const outsideEl = wrapper.find('.outside').element;
    wrapper.unmount();

    mouseDownOn(outsideEl);
    expect(onOutside).not.toHaveBeenCalled();
  });
});
