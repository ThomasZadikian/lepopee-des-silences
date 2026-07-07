// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PermanentItemSelectionPanel from './PermanentItemSelectionPanel.vue';
import type { PermanentItemCandidateDto } from '../types/runTypes';

const candidates: PermanentItemCandidateDto[] = [
  { itemDefinitionKey: 'item.relic.tome', displayName: 'Tome-38', description: 'Un tome oublié.', rarity: 'Epic' },
  { itemDefinitionKey: 'item.equipment.sac', displayName: 'Sac renforcé', description: 'Augmente la capacité.', rarity: 'Rare' },
];

function mountPanel(items: PermanentItemCandidateDto[] = candidates, isLoading = false) {
  return mount(PermanentItemSelectionPanel, {
    props: { candidates: items, isLoading },
  });
}

describe('PermanentItemSelectionPanel', () => {
  it('renders a card per candidate', () => {
    const wrapper = mountPanel();
    expect(wrapper.findAll('.pis-card')).toHaveLength(2);
    expect(wrapper.text()).toContain('Tome-38');
    expect(wrapper.text()).toContain('Sac renforcé');
  });

  it('shows the empty state when there are no candidates', () => {
    const wrapper = mountPanel([]);
    expect(wrapper.text()).toContain("Aucun objet éligible n'a été trouvé durant cette run.");
  });

  it('toggles selection when a card is clicked', async () => {
    const wrapper = mountPanel();
    const cards = wrapper.findAll('.pis-card');
    await cards[0].trigger('click');
    expect(cards[0].classes()).toContain('pis-card--sel');
    await cards[0].trigger('click');
    expect(cards[0].classes()).not.toContain('pis-card--sel');
  });

  it('emits confirm with the selected keys', async () => {
    const wrapper = mountPanel();
    const cards = wrapper.findAll('.pis-card');
    await cards[0].trigger('click');
    await cards[1].trigger('click');

    await wrapper.find('.es-btn--gold').trigger('click');

    expect(wrapper.emitted('confirm')).toHaveLength(1);
    const [emittedKeys] = wrapper.emitted('confirm')![0] as [string[]];
    expect(emittedKeys).toEqual(
      expect.arrayContaining(['item.relic.tome', 'item.equipment.sac']),
    );
    expect(emittedKeys).toHaveLength(2);
  });

  it('emits confirm with an empty array when nothing is selected', async () => {
    const wrapper = mountPanel();
    await wrapper.find('.es-btn--gold').trigger('click');

    expect(wrapper.emitted('confirm')![0]).toEqual([[]]);
  });

  it('disables the confirm button while loading', () => {
    const wrapper = mountPanel(candidates, true);
    expect(wrapper.find('.es-btn--gold').attributes('disabled')).toBeDefined();
  });
});
