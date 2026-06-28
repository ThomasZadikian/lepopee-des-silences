// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatantSidePanel from './CombatantSidePanel.vue';
import type { CombatantRuntimeDto } from '../types/combatContracts';

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
    charge: 2,
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
      },
      {
        key: 'skill.guard',
        displayName: 'Garde',
        skillType: 'Guard',
        targetingType: 'Self',
        effectType: 'Guard',
        manaCost: 3,
        chargeCost: 0,
        basePower: 5,
        tags: [],
      },
    ],
    ...overrides,
  };
}

function mountPanel(combatant: CombatantRuntimeDto | null) {
  return mount(CombatantSidePanel, { props: { combatant } });
}

describe('CombatantSidePanel', () => {
  it('renders without crashing with combatant', () => {
    const wrapper = mountPanel(makeCombatant());
    expect(wrapper.exists()).toBe(true);
  });

  it('renders without crashing with null combatant', () => {
    const wrapper = mountPanel(null);
    expect(wrapper.exists()).toBe(true);
  });

  it('shows empty state when combatant is null', () => {
    const wrapper = mountPanel(null);
    expect(wrapper.text()).toContain('SÉLECTIONNE UN COMBATTANT');
  });

  it('displays ALLIÉ label for player side', () => {
    const wrapper = mountPanel(makeCombatant({ side: 'Player' }));
    expect(wrapper.text()).toContain('ALLIÉ');
  });

  it('displays ENNEMI label for enemy side', () => {
    const wrapper = mountPanel(makeCombatant({ side: 'Enemy' }));
    expect(wrapper.text()).toContain('ENNEMI');
  });

  it('displays the archetype', () => {
    const wrapper = mountPanel(makeCombatant({ archetype: 'Mage' }));
    expect(wrapper.text()).toContain('Mage');
  });

  it('displays the combatant name', () => {
    const wrapper = mountPanel(makeCombatant({ displayName: 'Warrior' }));
    expect(wrapper.find('.side-panel__name').text()).toBe('Warrior');
  });

  it('displays vitality stats', () => {
    const wrapper = mountPanel(makeCombatant({ currentVitality: 75, maxVitality: 100 }));
    expect(wrapper.text()).toContain('75 / 100');
  });

  it('displays guard stat', () => {
    const wrapper = mountPanel(makeCombatant({ guard: 25 }));
    expect(wrapper.text()).toContain('25');
  });

  it('displays mana stat', () => {
    const wrapper = mountPanel(makeCombatant({ mana: 8 }));
    expect(wrapper.text()).toContain('8');
  });

  it('displays charge stat', () => {
    const wrapper = mountPanel(makeCombatant({ charge: 3 }));
    expect(wrapper.text()).toContain('3');
  });

  it('shows skills section when combatant has skills', () => {
    const wrapper = mountPanel(makeCombatant());
    expect(wrapper.find('.side-panel__section').exists()).toBe(true);
  });

  it('hides skills section when combatant has no skills', () => {
    const wrapper = mountPanel(makeCombatant({ skills: [] }));
    expect(wrapper.find('.side-panel__section').exists()).toBe(false);
  });

  it('renders one skill item per skill', () => {
    const wrapper = mountPanel(makeCombatant());
    expect(wrapper.findAll('.side-panel__skill').length).toBe(2);
  });

  it('displays skill name', () => {
    const wrapper = mountPanel(makeCombatant());
    expect(wrapper.find('.side-panel__skill-name').text()).toBe('Frappe');
  });

  it('displays skill cost when manaCost > 0', () => {
    const wrapper = mountPanel(makeCombatant());
    const costs = wrapper.findAll('.side-panel__skill-cost');
    const guardCost = costs.find((c) => c.text().includes('MANA'));
    expect(guardCost?.text()).toContain('3 MANA');
  });

  it('displays dash when skill has no cost', () => {
    const wrapper = mountPanel(makeCombatant());
    const costs = wrapper.findAll('.side-panel__skill-cost');
    const freeCost = costs.find((c) => c.text().trim() === '—');
    expect(freeCost).toBeDefined();
  });

  it('displays skill meta information', () => {
    const wrapper = mountPanel(makeCombatant());
    expect(wrapper.find('.side-panel__skill-meta').text()).toContain('Damage');
    expect(wrapper.find('.side-panel__skill-meta').text()).toContain('PUISSANCE 10');
  });

  it('displays combatant status', () => {
    const wrapper = mountPanel(makeCombatant({ status: 'Active' }));
    expect(wrapper.find('.side-panel__status').text()).toContain('Active');
  });

  it('displays defeated status', () => {
    const wrapper = mountPanel(makeCombatant({ status: 'Defeated' }));
    expect(wrapper.find('.side-panel__status').text()).toContain('Defeated');
  });
});
