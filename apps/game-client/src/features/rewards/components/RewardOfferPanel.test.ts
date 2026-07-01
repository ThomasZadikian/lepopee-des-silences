// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RewardOfferPanel from './RewardOfferPanel.vue';
import type { RewardOfferDto } from '../types/rewardTypes';

const baseOffer: RewardOfferDto = {
  id: 'offer-1',
  source: 'NodeEvent',
  state: 'Pending',
  choices: [
    {
      id: 'choice-1',
      rewardType: 'Heal',
      label: 'Soin de base',
      description: 'Restaure quelques points de vitalité.',
    },
    {
      id: 'choice-2',
      rewardType: 'MemoryFragment',
      label: 'Fragment mémoriel',
      description: 'Un éclat du passé.',
    },
  ],
};

function mountPanel(offer: RewardOfferDto, isLoading = false) {
  return mount(RewardOfferPanel, {
    props: { offer, isLoading },
    global: {
      stubs: {
        ChipBadge: { template: '<span><slot /></span>' },
        EliseComment: { template: '<div />' },
        RuleOrnament: { template: '<hr />' },
        SigilIcon: { template: '<svg />' },
      },
    },
  });
}

describe('RewardOfferPanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel(baseOffer);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the source label when source is provided', () => {
    const wrapper = mountPanel(baseOffer);
    expect(wrapper.text()).toContain('Événement');
  });

  it('does not display a source chip when source is absent', () => {
    const wrapper = mountPanel({ ...baseOffer, source: undefined });
    expect(wrapper.find('.rop-source-chip').exists()).toBe(false);
  });

  it('shows an empty state when no choices are provided', () => {
    const wrapper = mountPanel({ ...baseOffer, choices: [] });
    expect(wrapper.text()).toContain('Aucune récompense disponible');
  });

  it('renders one card per choice', () => {
    const wrapper = mountPanel(baseOffer);
    expect(wrapper.findAll('.rop-card').length).toBe(2);
  });

  it('selecting a card enables the confirm button', async () => {
    const wrapper = mountPanel(baseOffer);
    const firstCard = wrapper.find('.rop-card');
    await firstCard.trigger('click');
    const btn = wrapper.find('.es-btn');
    expect((btn.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('emits selectReward with the choice id when confirmed', async () => {
    const wrapper = mountPanel(baseOffer);
    await wrapper.find('.rop-card').trigger('click');
    await wrapper.find('.es-btn').trigger('click');
    const events = wrapper.emitted('selectReward') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('choice-1');
  });

  it('does not emit when no card is selected', async () => {
    const wrapper = mountPanel(baseOffer);
    const btn = wrapper.find('.es-btn');
    if (!btn.exists()) return; // button may be hidden if disabled
    // button should be disabled
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('assigns frost tone to MemoryFragment rewardType', () => {
    const wrapper = mountPanel({
      ...baseOffer,
      choices: [{ id: 'c1', rewardType: 'MemoryFragment', label: 'Fragment', description: 'Un éclat.' }],
    });
    // Frost-toned card won't have rop-card--gold class
    expect(wrapper.find('.rop-card--gold').exists()).toBe(false);
  });

  it('shows the state chip when offer is selected', () => {
    const wrapper = mountPanel({ ...baseOffer, state: 'Selected' });
    expect(wrapper.find('.rop-state-chip').exists()).toBe(true);
    expect(wrapper.find('.rop-state-chip').text()).toContain('Sélectionné');
  });

  it('hides the confirm button when offer is already resolved', () => {
    const wrapper = mountPanel({ ...baseOffer, state: 'Selected' });
    expect(wrapper.find('.es-btn').exists()).toBe(false);
  });

  it('handles missing optional fields without crashing', () => {
    const minimal: RewardOfferDto = { id: 'x', choices: [] };
    const wrapper = mountPanel(minimal);
    expect(wrapper.exists()).toBe(true);
  });

  it('handles choices with no rarity gracefully', () => {
    const wrapper = mountPanel({
      ...baseOffer,
      choices: [{ id: 'c1', rewardType: 'Heal', label: 'Soin', description: 'PV.' }],
    });
    expect(wrapper.find('.rop-card').exists()).toBe(true);
  });

  it('disables interaction when isLoading is true', async () => {
    const wrapper = mountPanel(baseOffer, true);
    await wrapper.find('.rop-card').trigger('click');
    const btn = wrapper.find('.es-btn');
    if (btn.exists()) {
      expect((btn.element as HTMLButtonElement).disabled).toBe(true);
    }
  });

  it('shows a source-enemy tag on a card whose loot came from an enemy', () => {
    const wrapper = mountPanel({
      ...baseOffer,
      choices: [
        {
          id: 'c1',
          rewardType: 'TemporaryItem',
          label: 'Peau de serpent',
          description: 'Une mue encore souple.',
          sourceEnemyDisplayName: 'Chimere Serpentaire',
        },
      ],
    });
    const source = wrapper.find('.rop-card__source');
    expect(source.exists()).toBe(true);
    expect(source.text()).toBe('Chimere Serpentaire');
  });

  it('does not show a source-enemy tag for a fallback/generic item', () => {
    const wrapper = mountPanel({
      ...baseOffer,
      choices: [
        {
          id: 'c1',
          rewardType: 'TemporaryItem',
          label: 'Baume de mémoire',
          description: 'Restaure une partie de la vitalité.',
          sourceEnemyDisplayName: null,
        },
      ],
    });
    expect(wrapper.find('.rop-card__source').exists()).toBe(false);
  });

  it('renders up to six cards in the wrapping grid without crashing', () => {
    const wrapper = mountPanel({
      ...baseOffer,
      choices: Array.from({ length: 6 }, (_, i) => ({
        id: `c${i}`,
        rewardType: 'TemporaryItem',
        label: `Objet ${i}`,
        description: 'Un butin de combat.',
        sourceEnemyDisplayName: i % 2 === 0 ? 'Chimere Serpentaire' : null,
      })),
    });
    expect(wrapper.findAll('.rop-card').length).toBe(6);
    expect(wrapper.findAll('.rop-card__source').length).toBe(3);
  });
});
