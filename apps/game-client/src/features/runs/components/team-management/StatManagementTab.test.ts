// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import StatManagementTab from './StatManagementTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';

vi.mock('../../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
  },
}));

function baseCharacter(): PlayerCharacterView {
  return {
    id: 'char-1',
    definitionKey: 'character.player.self',
    displayName: 'Le Porteur',
    maxEquippedSkills: 4,
    items: [],
    maxEquippedItems: 3,
    characterType: 'Standard',
    skills: [],
    stats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
    },
  };
}

describe('StatManagementTab', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('renders all 10 stats with a description each', () => {
    const wrapper = mount(StatManagementTab, { props: { character: baseCharacter() } });
    expect(wrapper.findAll('.smt-row')).toHaveLength(10);
    expect(wrapper.findAll('.smt-row__desc').every((d) => d.text().length > 0)).toBe(true);
  });

  it('shows the unspent stat points count', () => {
    usePlayerStore().profile = {
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 3, totalStatPointsEarned: 5, palaceShardCount: 0 },
      permanentItems: [],
    };
    const wrapper = mount(StatManagementTab, { props: { character: baseCharacter() } });
    expect(wrapper.find('.smt-points').text()).toBe('3');
  });

  it('disables the +1 button when there are no unspent points', () => {
    usePlayerStore().profile = {
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    };
    const wrapper = mount(StatManagementTab, { props: { character: baseCharacter() } });
    const button = wrapper.find('.smt-row__add');
    expect(button.attributes('disabled')).toBeDefined();
  });

  it('shows the correct per-stat increment on each +N button (PV +10, PP +5, others +1)', () => {
    const wrapper = mount(StatManagementTab, { props: { character: baseCharacter() } });
    const buttons = wrapper.findAll('.smt-row__add');

    expect(buttons[0].text()).toBe('+10'); // MaxVitality is first in statOrder
    expect(buttons[8].text()).toBe('+5');  // Mana is 9th in statOrder
    expect(buttons[1].text()).toBe('+1');  // AttackPower
  });
});
