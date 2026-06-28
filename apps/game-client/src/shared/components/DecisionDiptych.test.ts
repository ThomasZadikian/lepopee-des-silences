// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import DecisionDiptych from './DecisionDiptych.vue';

function mountDiptych(
  modelValue = true,
  title = 'Confirm',
  description?: string,
  confirmLabel?: string,
  cancelLabel?: string,
  danger = false,
) {
  return mount(DecisionDiptych, {
    props: { modelValue, title, description, confirmLabel, cancelLabel, danger },
    global: {
      stubs: {
        Teleport: { template: '<slot />' },
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('DecisionDiptych', () => {
  it('renders without crashing', () => {
    const wrapper = mountDiptych();
    expect(wrapper.exists()).toBe(true);
  });

  it('does not render when modelValue is false', () => {
    const wrapper = mountDiptych(false);
    expect(wrapper.find('.diptych-overlay').exists()).toBe(false);
  });

  it('displays the title', () => {
    const wrapper = mountDiptych(true, 'Delete item');
    expect(wrapper.text()).toContain('Delete item');
  });

  it('displays the description when provided', () => {
    const wrapper = mountDiptych(true, 'Title', 'This is a description');
    expect(wrapper.text()).toContain('This is a description');
  });

  it('hides description when not provided', () => {
    const wrapper = mountDiptych(true, 'Title');
    expect(wrapper.find('.diptych__desc').exists()).toBe(false);
  });

  it('displays custom confirm label', () => {
    const wrapper = mountDiptych(true, 'Title', undefined, 'Yes');
    expect(wrapper.text()).toContain('Yes');
  });

  it('displays custom cancel label', () => {
    const wrapper = mountDiptych(true, 'Title', undefined, undefined, 'No');
    expect(wrapper.text()).toContain('No');
  });

  it('uses default confirm label', () => {
    const wrapper = mountDiptych();
    expect(wrapper.text()).toContain('Confirmer');
  });

  it('uses default cancel label', () => {
    const wrapper = mountDiptych();
    expect(wrapper.text()).toContain('Annuler');
  });

  it('applies danger class when danger is true', () => {
    const wrapper = mountDiptych(true, 'Title', undefined, undefined, undefined, true);
    expect(wrapper.find('.diptych__side--danger').exists()).toBe(true);
  });

  it('emits confirm when confirm side is clicked', async () => {
    const wrapper = mountDiptych();
    const confirmSide = wrapper.find('.diptych__side--confirm');
    await confirmSide.trigger('click');
    expect(wrapper.emitted('confirm')).toHaveLength(1);
  });

  it('emits update:modelValue with false when cancel side is clicked', async () => {
    const wrapper = mountDiptych();
    const cancelSide = wrapper.find('.diptych__side--cancel');
    await cancelSide.trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('emits update:modelValue with false when overlay is clicked', async () => {
    const wrapper = mountDiptych();
    await wrapper.find('.diptych-overlay').trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('emits update:modelValue with false on Escape key', async () => {
    const wrapper = mountDiptych(false);
    await wrapper.setProps({ modelValue: true });
    const event = new KeyboardEvent('keydown', { key: 'Escape' });
    document.dispatchEvent(event);
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('adds keydown listener when opened', async () => {
    const wrapper = mountDiptych(false);
    const addEventListenerSpy = vi.spyOn(document, 'addEventListener');
    await wrapper.setProps({ modelValue: true });
    expect(addEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
    addEventListenerSpy.mockRestore();
  });

  it('removes keydown listener when closed', async () => {
    const wrapper = mountDiptych(true);
    const removeEventListenerSpy = vi.spyOn(document, 'removeEventListener');
    await wrapper.setProps({ modelValue: false });
    expect(removeEventListenerSpy).toHaveBeenCalledWith('keydown', expect.any(Function));
    removeEventListenerSpy.mockRestore();
  });

  it('renders center section with Confirmation kicker', () => {
    const wrapper = mountDiptych();
    expect(wrapper.text()).toContain('Confirmation');
  });
});
