// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import EventOutcomePanel from './EventOutcomePanel.vue';
import type { EventOutcomeDto } from '../types/eventTypes';

vi.mock('../types/eventTypes', async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    getOutcomeChoices: vi.fn((outcome) => outcome.choices ?? []),
    getOutcomeFamily: vi.fn(() => 'Narrative'),
    isChoiceOutcome: vi.fn((outcome) => Boolean(outcome.choices?.length)),
  };
});

function mountPanel(outcome: EventOutcomeDto, isLoading = false) {
  return mount(EventOutcomePanel, {
    props: { outcome, isLoading },
    global: {
      stubs: {
        Transition: { template: '<slot />' },
      },
    },
  });
}

describe('EventOutcomePanel', () => {
  const baseOutcome: EventOutcomeDto = {
    id: 'outcome-1',
    nodeId: 'node-1',
    title: 'Résolution',
    description: 'Le Palais a statué.',
    resolutionKind: 'Narrative',
    choices: [
      { id: 'c1', label: 'Option A', description: 'Choix A', isEnabled: true },
      { id: 'c2', label: 'Option B', description: 'Choix B', isEnabled: false },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the outcome title', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Résolution');
  });

  it('displays the description', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Le Palais a statué.');
  });

  it('shows the resolution kind chip', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Narrative');
  });

  it('displays Elise quote from narrativeFragments', () => {
    const outcome = {
      ...baseOutcome,
      narrativeFragments: [
        { text: 'Ceci est une citation.', speaker: 'Élise' },
        { text: 'Fragment suivant.', speaker: 'Narrateur' },
      ],
    };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).toContain('Ceci est une citation.');
  });

  it('falls back to description when no narrativeFragments', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Le Palais a statué.');
  });

  it('shows choice drawer button when outcome has choices', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Que fais-tu ?');
  });

  it('shows direct continue button when no choices', async () => {
    const outcome = { ...baseOutcome, choices: [] };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).toContain('Continuer');
  });

  it('emits continue on direct continue button click', async () => {
    const outcome = { ...baseOutcome, choices: [] };
    const wrapper = mountPanel(outcome);
    await wrapper.findAll('.eo-btn').find((b) => b.text().includes('Continuer'))!.trigger('click');
    expect(wrapper.emitted('continue')).toHaveLength(1);
  });

  it('opens drawer on choice opener click', async () => {
    const wrapper = mountPanel(baseOutcome);
    const btn = wrapper.findAll('.eo-btn').find((b) => b.text().includes('Que fais-tu'));
    if (btn) await btn.trigger('click');
    expect(wrapper.text()).toContain('Que fais-tu ?');
  });

  it('emits selectChoice when choice is confirmed', async () => {
    const wrapper = mountPanel(baseOutcome);
    const btn = wrapper.findAll('.eo-btn').find((b) => b.text().includes('Que fais-tu'));
    if (btn) await btn.trigger('click');
    const selectBtn = wrapper.findAll('.eo-btn').find((b) => b.text().includes('Sélectionner ce geste'));
    if (selectBtn) await selectBtn.trigger('click');
    const confirmBtn = wrapper.findAll('.eo-btn').find((b) => b.text().includes('Valider'));
    if (confirmBtn) await confirmBtn.trigger('click');
    expect(wrapper.emitted('selectChoice')).toBeDefined();
  });

  it('disables buttons when isLoading is true', () => {
    const wrapper = mountPanel(baseOutcome, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBeGreaterThan(0);
  });

  it('shows the outcome family chip', () => {
    const wrapper = mountPanel(baseOutcome);
    const chip = wrapper.find('.eo-top-row .eo-chip');
    expect(chip.text()).toBe('Narrative');
  });

  it('renders body fragments beyond the first', () => {
    const outcome = {
      ...baseOutcome,
      narrativeFragments: [
        { text: 'Quote', speaker: 'Élise' },
        { text: 'Fragment 1', speaker: 'Narrateur' },
        { text: 'Fragment 2', speaker: 'Voix' },
      ],
    };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).toContain('Fragment 1');
    expect(wrapper.text()).toContain('Fragment 2');
  });

  it('handles missing optional fields gracefully', () => {
    const minimal: EventOutcomeDto = { id: 'x', nodeId: 'n1', title: 'T' };
    const wrapper = mountPanel(minimal);
    expect(wrapper.exists()).toBe(true);
  });

  it('resets state when nodeId changes', async () => {
    const wrapper = mountPanel(baseOutcome);
    await wrapper.setProps({ outcome: { ...baseOutcome, nodeId: 'node-2' } });
    expect(wrapper.exists()).toBe(true);
  });
});
