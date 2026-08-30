// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import CharacterStatsTab from './CharacterStatsTab.vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';

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
      speed: 10, initiative: 10, focus: 0, mana: 0, charge: 0,
    },
  };
}

describe('CharacterStatsTab', () => {
  it('renders effective stats without permanent allocation controls', () => {
    const wrapper = mount(CharacterStatsTab, { props: { character: baseCharacter() } });

    expect(wrapper.find('.stat-radar').exists()).toBe(true);
    expect(wrapper.findAll('.cst-row').length).toBeGreaterThan(0);
    expect(wrapper.find('button').exists()).toBe(false);
    expect(wrapper.text()).not.toContain('Points disponibles');
    expect(wrapper.text()).not.toContain('Valider les choix');
  });
});
