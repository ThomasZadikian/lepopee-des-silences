// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PageOverlayModal from './PageOverlayModal.vue';

describe('PageOverlayModal', () => {
  it('renders slot content', () => {
    const wrapper = mount(PageOverlayModal, {
      slots: { default: '<p class="content">Hello</p>' },
    });
    expect(wrapper.find('.content').text()).toBe('Hello');
  });

  it('emits close when the close button is clicked', async () => {
    const wrapper = mount(PageOverlayModal);
    await wrapper.find('.pom-close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('emits close when the backdrop itself is clicked', async () => {
    const wrapper = mount(PageOverlayModal);
    await wrapper.find('.pom-backdrop').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('does not emit close when clicking inside the panel', async () => {
    const wrapper = mount(PageOverlayModal, {
      slots: { default: '<p class="content">Hello</p>' },
    });
    await wrapper.find('.pom-panel').trigger('click');
    expect(wrapper.emitted('close')).toBeUndefined();
  });
});
