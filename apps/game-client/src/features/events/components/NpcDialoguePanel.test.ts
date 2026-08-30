// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import NpcDialoguePanel from './NpcDialoguePanel.vue';
import type { NpcDialogueViewDto, NarrativeFragmentDto } from '../../runs/types/runTypes';

vi.useFakeTimers();

function mountPanel(
  dialogue: NpcDialogueViewDto | null = null,
  echoes: NarrativeFragmentDto[] = [],
  ended = false,
  isLoading = false,
) {
  return mount(NpcDialoguePanel, {
    props: { dialogue, echoes, ended, isLoading },
    attachTo: document.body,
  });
}

describe('NpcDialoguePanel', () => {
  const baseDialogue: NpcDialogueViewDto = {
    nodeKey: 'npc-1',
    speaker: 'L\'Ombre',
    aggregateState: 'latent',
    lines: ['Bienvenue dans le Palais.', 'Tu cherches quelque chose, n\'est-ce pas ?'],
    choices: [
      { choiceId: 'c1', label: 'Oui, je cherche la vérité.' },
      { choiceId: 'c2', label: 'Non, je me suis perdu.' },
    ],
  };

  beforeEach(() => {
    vi.clearAllTimers();
  });

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseDialogue);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the speaker name', () => {
    const wrapper = mountPanel(baseDialogue);
    expect(wrapper.text()).toContain('L\'Ombre');
  });

  it('starts typewriter effect on mount', async () => {
    const wrapper = mountPanel(baseDialogue);
    expect(wrapper.find('.npc-caret').exists()).toBe(true);
  });

  it('shows choices after all lines are typed', async () => {
    const singleLineDialogue = { ...baseDialogue, lines: ['Single line.'] };
    const wrapper = mountPanel(singleLineDialogue);
    vi.runAllTimers();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();
    expect(wrapper.findAll('.npc-choice').length).toBe(2);
  });

  it('emits selectChoice when a choice is clicked', async () => {
    const singleLineDialogue = { ...baseDialogue, lines: ['Single line.'] };
    const wrapper = mountPanel(singleLineDialogue);
    vi.runAllTimers();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();
    await wrapper.find('.npc-choice').trigger('click');
    expect(wrapper.emitted('selectChoice')).toBeDefined();
    expect(wrapper.emitted('selectChoice')![0][0]).toBe('c1');
  });

  it('shows continue button when dialogue is ended', () => {
    const wrapper = mountPanel(baseDialogue, [], true);
    expect(wrapper.text()).toContain('Se retirer du seuil');
  });

  it('emits continue when ended button is clicked', async () => {
    const wrapper = mountPanel(baseDialogue, [], true);
    await wrapper.find('.npc-exit').trigger('click');
    expect(wrapper.emitted('continue')).toHaveLength(1);
  });

  it('disables choices when isLoading is true', async () => {
    const singleLineDialogue = { ...baseDialogue, lines: ['Single line.'] };
    const wrapper = mountPanel(singleLineDialogue, [], false, true);
    vi.runAllTimers();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();
    const choice = wrapper.find('.npc-choice');
    expect((choice.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('displays echoes when provided', () => {
    const echoes = [{ text: 'Un écho du passé.' }];
    const wrapper = mountPanel(baseDialogue, echoes);
    expect(wrapper.text()).toContain('Un écho du passé.');
  });

  it('applies bad mood styling for rompu state', () => {
    const dialogue = { ...baseDialogue, aggregateState: 'rompu' };
    const wrapper = mountPanel(dialogue);
    expect(wrapper.find('.mood-rompu').exists()).toBe(true);
  });

  it('applies warm mood for latent state', () => {
    const wrapper = mountPanel(baseDialogue);
    expect(wrapper.find('.mood-latent').exists()).toBe(true);
  });

  it('uses fallback speaker when dialogue is null', () => {
    const wrapper = mountPanel(null);
    expect(wrapper.text()).toContain('Une présence');
  });

  it('shows empty message when no choices available', async () => {
    const dialogue = { ...baseDialogue, choices: [], lines: ['Single line.'] };
    const wrapper = mountPanel(dialogue);
    vi.runAllTimers();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();
    expect(wrapper.text()).toContain('Le silence s\'installe');
  });

  it('still offers a way to leave when choices are empty but the encounter is not formally ended (soft-lock fallback)', async () => {
    const dialogue = { ...baseDialogue, choices: [], lines: ['Single line.'] };
    const wrapper = mountPanel(dialogue, [], false);
    vi.runAllTimers();
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();
    expect(wrapper.text()).toContain('Se retirer du seuil');
    await wrapper.find('.npc-choices .npc-exit').trigger('click');
    expect(wrapper.emitted('continue')).toHaveLength(1);
  });

  it('skips to full line on click while typing', async () => {
    const wrapper = mountPanel(baseDialogue);
    await wrapper.find('.npc-history').trigger('click');
    // Should show full first line
    expect(wrapper.text()).toContain('Bienvenue dans le Palais.');
  });

  it('handles space key for advancing', async () => {
    const wrapper = mountPanel(baseDialogue);
    const event = new KeyboardEvent('keydown', { code: 'Space' });
    window.dispatchEvent(event);
    await wrapper.vm.$nextTick();
  });

  it('cleans up timer on unmount', () => {
    const wrapper = mountPanel(baseDialogue);
    wrapper.unmount();
    // No error should be thrown
  });

  it('handles empty lines array', () => {
    const dialogue = { ...baseDialogue, lines: [] };
    const wrapper = mountPanel(dialogue);
    expect(wrapper.exists()).toBe(true);
  });
});
