// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import ConfirmDialog from './ConfirmDialog.vue';

function mountDialog(
  modelValue = true,
  title = 'Confirm action',
  message = 'Are you sure?',
  confirmLabel?: string,
  cancelLabel?: string,
  danger = false,
  loading = false,
) {
  return mount(ConfirmDialog, {
    props: { modelValue, title, message, confirmLabel, cancelLabel, danger, loading },
    global: {
      stubs: {
        Teleport: { template: '<slot />' },
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('ConfirmDialog', () => {
  it('renders without crashing', () => {
    const wrapper = mountDialog();
    expect(wrapper.exists()).toBe(true);
  });

  it('does not render when modelValue is false', () => {
    const wrapper = mountDialog(false);
    expect(wrapper.find('.dialog-backdrop').exists()).toBe(false);
  });

  it('displays the title', () => {
    const wrapper = mountDialog(true, 'Delete item');
    expect(wrapper.text()).toContain('Delete item');
  });

  it('displays the message', () => {
    const wrapper = mountDialog(true, 'Title', 'This action cannot be undone');
    expect(wrapper.text()).toContain('This action cannot be undone');
  });

  it('displays custom confirm label', () => {
    const wrapper = mountDialog(true, 'Title', 'Message', 'Yes, delete');
    expect(wrapper.text()).toContain('Yes, delete');
  });

  it('displays custom cancel label', () => {
    const wrapper = mountDialog(true, 'Title', 'Message', undefined, 'Go back');
    expect(wrapper.text()).toContain('Go back');
  });

  it('uses default confirm label', () => {
    const wrapper = mountDialog();
    expect(wrapper.text()).toContain('Confirmer');
  });

  it('uses default cancel label', () => {
    const wrapper = mountDialog();
    expect(wrapper.text()).toContain('Annuler');
  });

  it('shows danger styling when danger is true', () => {
    const wrapper = mountDialog(true, 'Title', 'Message', undefined, undefined, true);
    expect(wrapper.find('.dialog__confirm--danger').exists()).toBe(true);
  });

  it('shows "Action irréversible" label for danger dialogs', () => {
    const wrapper = mountDialog(true, 'Title', 'Message', undefined, undefined, true);
    expect(wrapper.text()).toContain('Action irréversible');
  });

  it('shows "Confirmation" label for non-danger dialogs', () => {
    const wrapper = mountDialog();
    expect(wrapper.text()).toContain('Confirmation');
  });

  it('emits confirm when confirm button is clicked', async () => {
    const wrapper = mountDialog();
    const confirmBtn = wrapper.findAll('button').find((b) => b.text().includes('Confirmer'));
    if (confirmBtn) await confirmBtn.trigger('click');
    expect(wrapper.emitted('confirm')).toHaveLength(1);
  });

  it('emits update:modelValue with false when cancel is clicked', async () => {
    const wrapper = mountDialog();
    const cancelBtn = wrapper.findAll('button').find((b) => b.text().includes('Annuler'));
    if (cancelBtn) await cancelBtn.trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('emits cancel when cancel is clicked', async () => {
    const wrapper = mountDialog();
    const cancelBtn = wrapper.findAll('button').find((b) => b.text().includes('Annuler'));
    if (cancelBtn) await cancelBtn.trigger('click');
    expect(wrapper.emitted('cancel')).toHaveLength(1);
  });

  it('emits update:modelValue with false when backdrop is clicked', async () => {
    const wrapper = mountDialog();
    await wrapper.find('.dialog-backdrop').trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeDefined();
    expect(wrapper.emitted('update:modelValue')![0][0]).toBe(false);
  });

  it('disables buttons when loading', () => {
    const wrapper = mountDialog(true, 'Title', 'Message', undefined, undefined, false, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBe(2);
  });

  it('shows loading text when loading', () => {
    const wrapper = mountDialog(true, 'Title', 'Message', undefined, undefined, false, true);
    expect(wrapper.text()).toContain('En cours');
  });

  it('closes dialog on backdrop self-click only', async () => {
    const wrapper = mountDialog();
    // Click on the dialog panel itself should not close
    await wrapper.find('.dialog').trigger('click');
    expect(wrapper.emitted('update:modelValue')).toBeUndefined();
  });
});
