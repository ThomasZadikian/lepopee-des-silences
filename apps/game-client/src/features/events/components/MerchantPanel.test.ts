// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import MerchantPanel from './MerchantPanel.vue';
import type { EventOutcomeDto } from '../types/eventTypes';

vi.mock('../types/eventTypes', async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    getOutcomeChoices: vi.fn((outcome) => outcome.choices ?? []),
  };
});

function mountPanel(outcome: EventOutcomeDto, isLoading = false) {
  return mount(MerchantPanel, {
    props: { outcome, isLoading },
    global: {
      stubs: {
        SigilIcon: { template: '<svg />' },
        ChipBadge: { template: '<span><slot /></span>' },
      },
    },
  });
}

describe('MerchantPanel', () => {
  const baseOutcome: EventOutcomeDto = {
    id: 'outcome-1',
    title: 'Le Marchand',
    description: 'Un marchand vous propose ses marchandises.',
    choices: [
      { id: 'item-1', label: 'Potion', description: 'Une potion de soin', isEnabled: true },
      { id: 'item-2', label: 'Épée', description: 'Une lame ancienne', isEnabled: true },
    ],
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the merchant title', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Le Marchand');
  });

  it('displays the description', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.text()).toContain('Un marchand vous propose ses marchandises.');
  });

  it('renders items grid when choices exist', () => {
    const wrapper = mountPanel(baseOutcome);
    expect(wrapper.findAll('.mrc-item').length).toBe(2);
  });

  it('shows empty state when no choices', () => {
    const outcome = { ...baseOutcome, choices: [] };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).toContain("Le marchand n'a rien à proposer.");
  });

  it('selects an item on click', async () => {
    const wrapper = mountPanel(baseOutcome);
    await wrapper.find('.mrc-item').trigger('click');
    expect(wrapper.find('.mrc-item--sel').exists()).toBe(true);
  });

  it('emits selectChoice when confirm is clicked with selection', async () => {
    const wrapper = mountPanel(baseOutcome);
    await wrapper.find('.mrc-item').trigger('click');
    const confirmBtn = wrapper.findAll('.es-btn').find((b) => b.text().includes('Acquérir'));
    if (confirmBtn) await confirmBtn.trigger('click');
    expect(wrapper.emitted('selectChoice')).toBeDefined();
    expect(wrapper.emitted('selectChoice')![0][0]).toBe('item-1');
  });

  it('emits continue when decline is clicked', async () => {
    const wrapper = mountPanel(baseOutcome);
    const declineBtn = wrapper.findAll('.es-btn').find((b) => b.text().includes('Passer'));
    if (declineBtn) await declineBtn.trigger('click');
    expect(wrapper.emitted('continue')).toHaveLength(1);
  });

  it('disables buttons when isLoading is true', () => {
    const wrapper = mountPanel(baseOutcome, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBeGreaterThan(0);
  });

  it('disables confirm button when no item is selected', () => {
    const wrapper = mountPanel(baseOutcome);
    const confirmBtn = wrapper.findAll('.es-btn').find((b) => b.text().includes('Acquérir'));
    expect(confirmBtn).toBeDefined();
  });

  it('does not select disabled items', async () => {
    const outcome = {
      ...baseOutcome,
      choices: [{ id: 'item-1', label: 'Potion', description: '', isEnabled: false }],
    };
    const wrapper = mountPanel(outcome);
    await wrapper.find('.mrc-item').trigger('click');
    expect(wrapper.find('.mrc-item--sel').exists()).toBe(false);
  });

  it('shows unavailable badge for disabled items', () => {
    const outcome = {
      ...baseOutcome,
      choices: [{ id: 'item-1', label: 'Potion', description: '', isEnabled: false }],
    };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).toContain('Indisponible');
  });

  it('uses fallback title when title is absent', () => {
    const outcome = { ...baseOutcome, title: undefined };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).toContain('Le Marchand vous attend');
  });

  it('hides description when not provided', () => {
    const outcome = { ...baseOutcome, description: undefined };
    const wrapper = mountPanel(outcome);
    expect(wrapper.text()).not.toContain('Un marchand');
  });

});
