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
        category: 'Physical',
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
        category: 'Physical',
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

// Tests à implémenter ultérieurement
describe('CombatantSidePanel', () => {
  it('should render correctly', () => {
    const combatant = makeCombatant();
    const wrapper = mountPanel(combatant);
    expect(wrapper.exists()).toBe(true);
  });
});
