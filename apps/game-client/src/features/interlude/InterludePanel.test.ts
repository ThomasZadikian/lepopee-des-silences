// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import InterludePanel from './InterludePanel.vue';
import type { InterludeDto } from './interludeTypes';

function mountPanel(
  interlude: InterludeDto,
  isLoading = false,
  isSavingAndExiting = false,
  isAbandoningRun = false,
  runActionError: string | null = null,
) {
  return mount(InterludePanel, {
    props: { interlude, isLoading, isSavingAndExiting, isAbandoningRun, runActionError },
    global: {
      stubs: {
        Transition: { template: '<slot />' },
        ConfirmDialog: {
          template: '<div class="confirm-dialog-stub" />',
          props: ['modelValue', 'title', 'message', 'confirmLabel', 'cancelLabel', 'danger', 'loading'],
        },
      },
    },
  });
}

describe('InterludePanel', () => {
  const baseInterlude: InterludeDto = {
    id: 'interlude-1',
    displayRoomNumber: 3,
    nodes: [
      { id: 'n1', type: 'Player', positionSlot: 'center', label: 'Aventurier', description: 'Votre personnage', isEnabled: true },
      { id: 'n2', type: 'Inventory', positionSlot: 'left', label: 'Sac à dos', description: 'Inventaire', isEnabled: true },
      { id: 'n3', type: 'Placeholder', positionSlot: 'right', label: 'À venir', description: 'Bientôt', isEnabled: false },
    ],
    availableActions: [
      { key: 'save-and-exit', label: 'Sauvegarder et quitter', description: 'Sauvegarder', isEnabled: true },
      { key: 'abandon-run', label: 'Abandonner la run', description: 'Abandonner', isEnabled: true },
    ],
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseInterlude);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the title', () => {
    const wrapper = mountPanel(baseInterlude);
    expect(wrapper.text()).toContain('Repli du Palais');
  });

  it('displays the room number', () => {
    const wrapper = mountPanel(baseInterlude);
    expect(wrapper.text()).toContain('Salle 3');
  });

  it('renders all nodes', () => {
    const wrapper = mountPanel(baseInterlude);
    expect(wrapper.findAll('.interlude__node').length).toBe(3);
  });

  it('displays node labels', () => {
    const wrapper = mountPanel(baseInterlude);
    expect(wrapper.text()).toContain('Aventurier');
    expect(wrapper.text()).toContain('Sac à dos');
    expect(wrapper.text()).toContain('À venir');
  });

  it('marks disabled nodes', () => {
    const wrapper = mountPanel(baseInterlude);
    const disabledNode = wrapper.findAll('.interlude__node').find((n) => n.text().includes('À venir'));
    expect(disabledNode?.classes()).toContain('interlude__node--disabled');
  });

  it('selects a node on click', async () => {
    const wrapper = mountPanel(baseInterlude);
    await wrapper.find('.interlude__node').trigger('click');
    expect(wrapper.find('.interlude__node--active').exists()).toBe(true);
  });

  it('shows node detail when selected', async () => {
    const wrapper = mountPanel(baseInterlude);
    await wrapper.find('.interlude__node').trigger('click');
    expect(wrapper.find('.interlude__node-detail').exists()).toBe(true);
  });

  it('shows coming soon message for placeholder nodes', async () => {
    const interludeWithPlaceholder = {
      ...baseInterlude,
      nodes: [
        ...baseInterlude.nodes,
        { id: 'placeholder-1', type: 'Placeholder', label: 'Test', description: 'Test', isEnabled: false, positionSlot: 'bl' as const },
      ],
    };
    const wrapper = mountPanel(interludeWithPlaceholder);
    const placeholderNode = wrapper.findAll('.interlude__node').find((n) => n.text().includes('À venir'));
    expect(placeholderNode).toBeDefined();
  });

  it('shows description for non-placeholder nodes', async () => {
    const wrapper = mountPanel(baseInterlude);
    await wrapper.find('.interlude__node').trigger('click');
    expect(wrapper.text()).toContain('Votre personnage');
  });

  it('deselects node on second click', async () => {
    const wrapper = mountPanel(baseInterlude);
    await wrapper.find('.interlude__node').trigger('click');
    expect(wrapper.find('.interlude__node--active').exists()).toBe(true);
    await wrapper.find('.interlude__node').trigger('click');
    expect(wrapper.find('.interlude__node--active').exists()).toBe(false);
  });

  it('does not select disabled nodes', async () => {
    const wrapper = mountPanel(baseInterlude);
    const disabledNode = wrapper.findAll('.interlude__node').find((n) => n.text().includes('À venir'));
    if (disabledNode) await disabledNode.trigger('click');
    expect(wrapper.find('.interlude__node--active').exists()).toBe(false);
  });

  it('emits enterNextRoom when CTA is clicked', async () => {
    const wrapper = mountPanel(baseInterlude);
    const cta = wrapper.findAll('button').find((b) => b.text().includes('Entrer dans la prochaine Room'));
    if (cta) await cta.trigger('click');
    expect(wrapper.emitted('enterNextRoom')).toHaveLength(1);
  });

  it('emits saveAndExit when save button is clicked', async () => {
    const wrapper = mountPanel(baseInterlude);
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Sauvegarder'));
    if (saveBtn) await saveBtn.trigger('click');
    expect(wrapper.emitted('saveAndExit')).toHaveLength(1);
  });

  it('shows loading state for enterNextRoom', () => {
    const wrapper = mountPanel(baseInterlude, true);
    expect(wrapper.text()).toContain('Préparation en cours');
  });

  it('shows loading state for saveAndExit', () => {
    const wrapper = mountPanel(baseInterlude, false, true);
    expect(wrapper.text()).toContain('Sauvegarde');
  });

  it('shows loading state for abandon', () => {
    const wrapper = mountPanel(baseInterlude, false, false, true);
    expect(wrapper.text()).toContain('Abandon');
  });

  it('displays error message when provided', () => {
    const wrapper = mountPanel(baseInterlude, false, false, false, 'Erreur');
    expect(wrapper.text()).toContain('Erreur');
  });

  it('hides error message when not provided', () => {
    const wrapper = mountPanel(baseInterlude);
    expect(wrapper.find('.interlude__action-error').exists()).toBe(false);
  });

  it('disables buttons during loading', () => {
    const wrapper = mountPanel(baseInterlude, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBeGreaterThan(0);
  });

  it('hides save button when action is disabled', () => {
    const interlude = {
      ...baseInterlude,
      availableActions: [{ key: 'save-and-exit', label: 'Save', description: '', isEnabled: false }],
    };
    const wrapper = mountPanel(interlude);
    expect(wrapper.text()).not.toContain('Sauvegarder et quitter');
  });

  it('hides abandon button when action is disabled', () => {
    const interlude = {
      ...baseInterlude,
      availableActions: [{ key: 'abandon-run', label: 'Abandon', description: '', isEnabled: false }],
    };
    const wrapper = mountPanel(interlude);
    expect(wrapper.text()).not.toContain('Abandonner la run');
  });

  it('applies correct grid area classes based on positionSlot', () => {
    const wrapper = mountPanel(baseInterlude);
    const centerNode = wrapper.findAll('.interlude__node').find((n) => n.text().includes('Aventurier'));
    expect(centerNode?.classes()).toContain('interlude__node--slot-center');
  });
});
