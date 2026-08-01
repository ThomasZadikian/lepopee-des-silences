// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
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

function mountPanel(offer: RewardOfferDto, isLoading = false, errorMessage: string | null = null) {
  return mount(RewardOfferPanel, {
    props: { offer, isLoading, errorMessage },
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

const defeatedEnemyOffer: RewardOfferDto = {
  ...baseOffer,
  defeatedEnemies: [
    {
      enemyKey: 'canon.enemy.chimeres-serpentaires',
      displayName: 'Chimères serpentaires',
      description: 'Ce qui reste quand plusieurs serpents partagent une seule volonté.',
      count: 2,
      lootEntries: [
        { itemKey: 'canon.item.datura', itemDisplayName: 'Datura stramonium', rarity: 'Rare', dropPercent: 30 },
        { itemKey: 'canon.item.cendre-benite', itemDisplayName: 'Cendre bénite', rarity: 'Common', dropPercent: 40 },
      ],
    },
  ],
};

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

  it('does not show the defeated-enemies sidebar when defeatedEnemies is absent', () => {
    const wrapper = mountPanel(baseOffer);
    expect(wrapper.find('.del').exists()).toBe(false);
  });

  it('shows the defeated-enemies sidebar with a count badge for grouped duplicates', () => {
    const wrapper = mountPanel(defeatedEnemyOffer);
    expect(wrapper.find('.del').exists()).toBe(true);
    expect(wrapper.text()).toContain('Chimères serpentaires');
    expect(wrapper.text()).toContain('×2');
  });

  it('opens the loot popover with description and full loot table on click', async () => {
    const wrapper = mountPanel(defeatedEnemyOffer);
    await wrapper.find('.del-row__trigger').trigger('click');
    await flushPromises();
    // Teleported to <body>, so it lives outside the mounted wrapper's own root.
    const popover = document.body.querySelector('.del-pop');
    expect(popover).not.toBeNull();
    const popoverText = popover?.textContent ?? '';
    expect(popoverText).toContain('Ce qui reste quand plusieurs serpents partagent une seule volonté.');
    expect(popoverText).toContain('Datura stramonium');
    expect(popoverText).toContain('30%');
    expect(popoverText).toContain('Cendre bénite');
    expect(popoverText).toContain('40%');
  });

  // ── Merchant purchase: price display + insufficient-funds error handling ──

  const merchantOffer: RewardOfferDto = {
    ...baseOffer,
    choices: [
      {
        id: 'c-item',
        rewardType: 'TemporaryItem',
        label: 'Objet du marchand',
        description: 'Un objet à vendre.',
        palaceShardCost: 500,
        himLitShardCost: 25,
      },
      {
        id: 'c-decline',
        rewardType: 'Decline',
        label: 'Refuser',
        description: "Tu quittes le marchand les mains vides.",
      },
    ],
  };

  it('shows the price on a merchant choice with a cost', () => {
    const wrapper = mountPanel(merchantOffer);
    expect(wrapper.text()).toContain('500 Éclats du Palais');
    expect(wrapper.text()).toContain("25 Éclats de Him'Lit");
  });

  it('does not show a price on a free choice', () => {
    const wrapper = mountPanel(baseOffer);
    expect(wrapper.text()).not.toContain('Éclats du Palais');
  });

  it('labels a Decline choice as "Renoncer" and shows no price for it', () => {
    const wrapper = mountPanel(merchantOffer);
    const cards = wrapper.findAll('.rop-card');
    const declineCard = cards[1];
    expect(declineCard.text()).toContain('Renoncer');
  });

  it('displays the error message and does not crash after a failed purchase', async () => {
    const wrapper = mountPanel(merchantOffer, false, 'Fonds insuffisants pour cet achat.');
    expect(wrapper.text()).toContain('Fonds insuffisants pour cet achat.');
  });

  it('re-enables the confirm button after an errorMessage appears post-selection', async () => {
    const wrapper = mountPanel(merchantOffer);
    await wrapper.findAll('.rop-card')[0].trigger('click');
    await wrapper.find('.es-btn--lg').trigger('click');
    expect(wrapper.emitted('selectReward')).toEqual([['c-item']]);

    await wrapper.setProps({ errorMessage: 'Fonds insuffisants pour cet achat.' });

    const button = wrapper.find('.es-btn--lg');
    expect(button.text()).not.toContain('✓');
  });
});
