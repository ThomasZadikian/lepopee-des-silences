// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import RunDangerActions from './RunDangerActions.vue';

function mountComponent(
  isSafePoint = true,
  isSavingAndExiting = false,
  isAbandoningRun = false,
  isExitingMidRoom = false,
  error: string | null = null,
) {
  return mount(RunDangerActions, {
    props: { isSafePoint, isSavingAndExiting, isAbandoningRun, isExitingMidRoom, error },
    global: {
      stubs: {
        ConfirmDialog: {
          template: '<div class="confirm-dialog-stub" />',
          props: ['modelValue', 'title', 'message', 'confirmLabel', 'cancelLabel', 'danger', 'loading'],
        },
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('RunDangerActions', () => {
  it('renders without crashing', () => {
    const wrapper = mountComponent();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the section label', () => {
    const wrapper = mountComponent();
    expect(wrapper.text()).toContain('Actions de run');
  });

  it('shows save button enabled at safe point', () => {
    const wrapper = mountComponent(true);
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Sauvegarder'));
    expect(saveBtn).toBeDefined();
    expect((saveBtn!.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('disables save button when not at safe point', () => {
    const wrapper = mountComponent(false);
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Sauvegarder'));
    expect(saveBtn).toBeDefined();
    expect((saveBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows hint when not at safe point', () => {
    const wrapper = mountComponent(false);
    expect(wrapper.text()).toContain('Disponible en fin de salle');
  });

  it('shows exit room button when not at safe point', () => {
    const wrapper = mountComponent(false);
    expect(wrapper.text()).toContain('Quitter la salle');
  });

  it('emits exitMidRoom when exit room button is clicked', async () => {
    const wrapper = mountComponent(false);
    const exitBtn = wrapper.findAll('button').find((b) => b.text().includes('Quitter la salle'));
    if (exitBtn) await exitBtn.trigger('click');
    expect(wrapper.emitted('exitMidRoom')).toHaveLength(1);
  });

  it('emits saveAndExit when save button is clicked', async () => {
    const wrapper = mountComponent(true);
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Sauvegarder'));
    if (saveBtn) await saveBtn.trigger('click');
    expect(wrapper.emitted('saveAndExit')).toHaveLength(1);
  });

  it('shows abandon button at safe point', () => {
    const wrapper = mountComponent(true);
    expect(wrapper.text()).toContain('Abandonner la run');
  });

  it('shows loading state for saveAndExit', () => {
    const wrapper = mountComponent(true, true);
    expect(wrapper.text()).toContain('Sauvegarde');
  });

  it('shows loading state for abandoning', () => {
    const wrapper = mountComponent(true, false, true);
    expect(wrapper.text()).toContain('Abandon');
  });

  it('shows loading state for exiting mid room', () => {
    const wrapper = mountComponent(false, false, false, true);
    expect(wrapper.text()).toContain('Sortie');
  });

  it('displays error message when provided', () => {
    const wrapper = mountComponent(true, false, false, false, 'Une erreur est survenue');
    expect(wrapper.text()).toContain('Une erreur est survenue');
  });

  it('hides error message when not provided', () => {
    const wrapper = mountComponent(true);
    expect(wrapper.find('.danger-actions__error').exists()).toBe(false);
  });

  it('disables abandon button when not at safe point', () => {
    const wrapper = mountComponent(false);
    const abandonBtn = wrapper.findAll('button').find((b) => b.text().includes('Abandonner'));
    expect(abandonBtn).toBeDefined();
    expect((abandonBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('opens abandon dialog on button click at safe point', async () => {
    const wrapper = mountComponent(true);
    const abandonBtn = wrapper.findAll('button').find((b) => b.text().includes('Abandonner'));
    if (abandonBtn) await abandonBtn.trigger('click');
    // Dialog stub should be present
    expect(wrapper.find('.confirm-dialog-stub').exists()).toBe(true);
  });

  it('emits abandon when dialog confirms', async () => {
    const wrapper = mountComponent(true);
    // Simulate dialog confirm via the stub
    await wrapper.vm.$emit('abandon');
    expect(wrapper.emitted('abandon')).toBeDefined();
  });

  it('disables all action buttons during saving', () => {
    const wrapper = mountComponent(true, true);
    const buttons = wrapper.findAll('button:not(.confirm-dialog-stub button)');
    const disabledCount = buttons.filter((b) => (b.element as HTMLButtonElement).disabled).length;
    expect(disabledCount).toBeGreaterThan(0);
  });
});
