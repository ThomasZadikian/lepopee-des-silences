// @vitest-environment jsdom
import { mount } from '@vue/test-utils';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

import LawResolutionPanel from '../features/palace-laws/LawResolutionPanel.vue';
import type { EventOutcomeDto } from '../features/events/types/eventTypes';

const lawOutcome: EventOutcomeDto = {
  nodeId: 'node-law-1',
  eventTypes: ['Law'],
  primaryEventType: 'Law',
  resolutionKind: 'PalaceLawOffered',
  riskLevel: 2,
  rewardProfile: 'Attack Bonus',
  title: 'Décret du Carnage',
  description: 'Les attaques gagnent en force, mais le Palais devient plus brutal.',
  requiresPlayerChoice: true,
  choices: [
    {
      choiceId: 'accept-law:law-carnage-v1',
      label: 'Inscrire la Loi',
      description: 'La loi entre en vigueur pour la run.',
    },
  ],
  narrativeFragments: [
    { speaker: 'Elise', text: 'Une loi se grave toujours dans quelqu’un.' },
  ],
};

function mountPanel(outcome: EventOutcomeDto = lawOutcome, isLoading = false) {
  return mount(LawResolutionPanel, {
    props: { outcome, isLoading, activeLaws: [], activeCurses: [] },
    global: {
      stubs: {
        ChipBadge: { template: '<span><slot /></span>' },
        EliseComment: { template: '<div><slot /></div>' },
        LawsPopover: { template: '<aside />' },
        RuleOrnament: { template: '<hr />' },
        SigilIcon: { template: '<svg />' },
      },
    },
  });
}

describe('LawResolutionPanel', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('displays an explicit proposed law section', () => {
    const wrapper = mountPanel();

    expect(wrapper.text()).toContain('Loi proposée');
    expect(wrapper.text()).toContain('Décret du Carnage');
    expect(wrapper.text()).toContain('Les attaques gagnent en force');
  });

  it('does not render decision buttons because laws cannot be refused', () => {
    const wrapper = mountPanel();

    expect(wrapper.findAll('.vlo-choice-card').length).toBe(0);
    expect(wrapper.text()).not.toContain('Refuser la Loi');
    expect(wrapper.text()).toContain('Cette Loi ne peut pas être refusée');
  });

  it('can seal immediately when the server provides an application choice', () => {
    const wrapper = mountPanel();
    const sealButton = wrapper.find('.es-btn--gold');

    expect((sealButton.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('emits the accept law choice after the law is sealed', async () => {
    vi.useFakeTimers();
    const wrapper = mountPanel();

    await wrapper.find('.es-btn--gold').trigger('click');
    vi.advanceTimersByTime(460);
    await nextTick();
    await wrapper.find('.es-btn--frost').trigger('click');

    const events = wrapper.emitted('selectChoice') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('accept-law:law-carnage-v1');
  });
});
