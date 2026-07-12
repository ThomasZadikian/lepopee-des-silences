// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import SkillBar from './SkillBar.vue';
import EmotionalTypeBadge from './EmotionalTypeBadge.vue';
import type { CombatantRuntimeDto, CombatUsableItemDto } from '../types/combatContracts';

function makeCombatant(overrides: Partial<CombatantRuntimeDto> = {}): CombatantRuntimeDto {
  return {
    id: 'ally-1',
    sourceKey: 'player.self',
    displayName: 'Hero',
    side: 'Player',
    archetype: 'Fighter',
    maxVitality: 100,
    currentVitality: 80,
    guard: 10,
    mana: 5,
    charge: 0,
    status: 'Active',
    skills: [
      {
        key: 'skill.strike',
        displayName: 'Frappe',
        skillType: 'Damage',
        targetingType: 'SingleEnemy',
        effectType: 'Damage',
        manaCost: 0,
        chargeCost: 0,
        basePower: 10,
        tags: [],
        category: 'Physical',
      },
      {
        key: 'skill.heal',
        displayName: 'Soin',
        skillType: 'Damage',
        targetingType: 'SingleAlly',
        effectType: 'Heal',
        manaCost: 3,
        chargeCost: 0,
        basePower: 5,
        tags: [],
        category: 'Magic',
        emotionalType: 'Memoire',
      },
    ],
    ...overrides,
  };
}

function makeItem(overrides: Partial<CombatUsableItemDto> = {}): CombatUsableItemDto {
  return {
    itemId: 'item-1',
    definitionKey: 'def.potion',
    displayName: 'Potion',
    effectType: 'Heal',
    effectAmount: 20,
    quantity: 3,
    targetingType: 'Self',
    ...overrides,
  };
}

function mountBar(
  combatant: CombatantRuntimeDto | null = makeCombatant(),
  selectedSkillKey: string | null = null,
  isPlayerTurn = true,
  isLoading = false,
  usableBattleItems: CombatUsableItemDto[] = [],
  selectedItemId: string | null = null,
) {
  return mount(SkillBar, {
    props: {
      combatant,
      selectedSkillKey,
      isPlayerTurn,
      isLoading,
      usableBattleItems,
      selectedItemId,
    },
  });
}

describe('SkillBar', () => {
  it('renders without crashing', () => {
    const wrapper = mountBar();
    expect(wrapper.exists()).toBe(true);
  });

  it('shows empty state when combatant is null', () => {
    const wrapper = mountBar(null);
    expect(wrapper.text()).toContain('Aucun combattant actif');
  });

  it('displays available mana', () => {
    const wrapper = mountBar(makeCombatant({ mana: 10 }));
    expect(wrapper.find('.skill-bar__verb').text()).toContain('10 PP disponibles');
  });

  it('renders one skill card per skill', () => {
    const wrapper = mountBar();
    expect(wrapper.findAll('.skill-card').length).toBe(2);
  });

  it('displays skill name', () => {
    const wrapper = mountBar();
    expect(wrapper.find('.skill-card__name').text()).toBe('Frappe');
  });

  it('displays skill cost when non-zero', () => {
    const wrapper = mountBar();
    const cards = wrapper.findAll('.skill-card');
    expect(cards[1].find('.skill-card__cost').text()).toBe('3 PP');
  });

  it('does not display a cost line when the skill is free', () => {
    const wrapper = mountBar();
    const cards = wrapper.findAll('.skill-card');
    expect(cards[0].find('.skill-card__cost').exists()).toBe(false);
  });

  it('shows the element badge when the skill has an emotional type', () => {
    const wrapper = mountBar();
    const cards = wrapper.findAll('.skill-card');
    expect(cards[1].findComponent(EmotionalTypeBadge).props('type')).toBe('Memoire');
    expect(cards[0].findComponent(EmotionalTypeBadge).exists()).toBe(false);
  });

  it('renders mechanical skill details in the hover tooltip', () => {
    const wrapper = mountBar();
    const cards = wrapper.findAll('.skill-card');
    const bubble = cards[1].find('.skill-card__bubble');
    expect(bubble.text()).toContain('Soin');
    expect(bubble.text()).toContain('Un allié');
    expect(bubble.text()).toContain('Magique');
    expect(bubble.text()).toContain('Mémoire');
    expect(bubble.text()).toContain('Puissance de base : 5');
    expect(bubble.text()).toContain('Coût : 3 PP');
  });

  it('applies selected class to selected skill', () => {
    const wrapper = mountBar(makeCombatant(), 'skill.strike');
    const cards = wrapper.findAll('.skill-card');
    expect(cards[0].classes()).toContain('skill-card--selected');
  });

  it('does not apply selected class to unselected skill', () => {
    const wrapper = mountBar(makeCombatant(), 'skill.heal');
    const cards = wrapper.findAll('.skill-card');
    expect(cards[0].classes()).not.toContain('skill-card--selected');
  });

  it('disables skills when not player turn', () => {
    const wrapper = mountBar(makeCombatant(), null, false);
    const cards = wrapper.findAll('.skill-card');
    cards.forEach((card) => {
      expect((card.element as HTMLButtonElement).disabled).toBe(true);
    });
  });

  it('disables skills when loading', () => {
    const wrapper = mountBar(makeCombatant(), null, true, true);
    const cards = wrapper.findAll('.skill-card');
    cards.forEach((card) => {
      expect((card.element as HTMLButtonElement).disabled).toBe(true);
    });
  });

  it('disables skills when combatant is defeated', () => {
    const wrapper = mountBar(makeCombatant({ status: 'Defeated' }));
    const cards = wrapper.findAll('.skill-card');
    cards.forEach((card) => {
      expect((card.element as HTMLButtonElement).disabled).toBe(true);
    });
  });

  it('emits selectSkill with skill key on click', async () => {
    const wrapper = mountBar();
    await wrapper.findAll('.skill-card')[0].trigger('click');
    const events = wrapper.emitted('selectSkill') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('skill.strike');
  });

  it('does not emit selectSkill when disabled', async () => {
    const wrapper = mountBar(makeCombatant(), null, false);
    await wrapper.findAll('.skill-card')[0].trigger('click');
    expect(wrapper.emitted('selectSkill')).toBeUndefined();
  });

  it('renders battle items when provided', () => {
    const items = [makeItem()];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    expect(wrapper.find('.skill-bar__items').exists()).toBe(true);
  });

  it('does not render battle items section when no items', () => {
    const wrapper = mountBar(makeCombatant(), null, true, false, []);
    expect(wrapper.find('.skill-bar__items').exists()).toBe(false);
  });

  it('renders item cards', () => {
    const items = [makeItem(), makeItem({ itemId: 'item-2', displayName: 'Antidote' })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    const itemCards = wrapper.findAll('.skill-card--item');
    expect(itemCards.length).toBe(2);
  });

  it('displays item name', () => {
    const items = [makeItem({ displayName: 'Élixir' })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    const itemNames = wrapper.findAll('.skill-card--item .skill-card__name');
    expect(itemNames[0].text()).toBe('Élixir');
  });

  it('displays item effect and quantity', () => {
    const items = [makeItem({ effectType: 'Heal', effectAmount: 30, quantity: 5 })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    const metas = wrapper.findAll('.skill-card--item .skill-card__meta');
    expect(metas[0].text()).toContain('+30 PV');
    expect(metas[0].text()).toContain('×5');
  });

  it('applies selected class to selected item', () => {
    const items = [makeItem({ itemId: 'sel-item' })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items, 'sel-item');
    const itemCard = wrapper.find('.skill-card--item');
    expect(itemCard.classes()).toContain('skill-card--selected');
  });

  it('disables items when not player turn', () => {
    const items = [makeItem()];
    const wrapper = mountBar(makeCombatant(), null, false, false, items);
    const itemCards = wrapper.findAll('.skill-card--item');
    itemCards.forEach((card) => {
      expect((card.element as HTMLButtonElement).disabled).toBe(true);
    });
  });

  it('disables items when loading', () => {
    const items = [makeItem()];
    const wrapper = mountBar(makeCombatant(), null, true, true, items);
    const itemCards = wrapper.findAll('.skill-card--item');
    itemCards.forEach((card) => {
      expect((card.element as HTMLButtonElement).disabled).toBe(true);
    });
  });

  it('emits selectItem with item id on click', async () => {
    const items = [makeItem({ itemId: 'potion-1' })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    await wrapper.find('.skill-card--item').trigger('click');
    const events = wrapper.emitted('selectItem') as string[][];
    expect(events).toBeDefined();
    expect(events[0][0]).toBe('potion-1');
  });

  it('shows Besace label when items are present', () => {
    const items = [makeItem()];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    expect(wrapper.find('.skill-bar__items-label').text()).toBe('Besace');
  });

  it('shows guard effect label for Guard items', () => {
    const items = [makeItem({ effectType: 'Guard', effectAmount: 15 })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    const metas = wrapper.findAll('.skill-card--item .skill-card__meta');
    expect(metas[0].text()).toContain('+15 Garde');
  });

  it('shows mana restore label for ManaRestore items', () => {
    const items = [makeItem({ effectType: 'ManaRestore', effectAmount: 10 })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    const metas = wrapper.findAll('.skill-card--item .skill-card__meta');
    expect(metas[0].text()).toContain('+10 Mana');
  });

  it('shows charge restore label for ChargeRestore items', () => {
    const items = [makeItem({ effectType: 'ChargeRestore', effectAmount: 2 })];
    const wrapper = mountBar(makeCombatant(), null, true, false, items);
    const metas = wrapper.findAll('.skill-card--item .skill-card__meta');
    expect(metas[0].text()).toContain('+2 Charge');
  });
});
