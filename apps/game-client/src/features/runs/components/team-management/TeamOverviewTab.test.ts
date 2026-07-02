// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import TeamOverviewTab from './TeamOverviewTab.vue';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';

function baseCharacter(overrides: Partial<PlayerCharacterView> = {}): PlayerCharacterView {
  return {
    id: 'char-1',
    definitionKey: 'character.player.self',
    displayName: 'Le Porteur',
    maxEquippedSkills: 4,
    skills: [
      { skillKey: 'skill.a', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: true },
    ],
    stats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
    },
    ...overrides,
  };
}

describe('TeamOverviewTab', () => {
  it('renders a card per character', () => {
    const wrapper = mount(TeamOverviewTab, { props: { characters: [baseCharacter()] } });
    expect(wrapper.findAll('.tov-card')).toHaveLength(1);
    expect(wrapper.text()).toContain('Le Porteur');
  });

  it('renders all 10 stats', () => {
    const wrapper = mount(TeamOverviewTab, { props: { characters: [baseCharacter()] } });
    expect(wrapper.findAll('.tov-stat')).toHaveLength(10);
  });

  it('shows equipped skills', () => {
    const wrapper = mount(TeamOverviewTab, { props: { characters: [baseCharacter()] } });
    expect(wrapper.text()).toContain('skill.a');
  });

  it('shows an empty-state message when no skills are equipped', () => {
    const wrapper = mount(TeamOverviewTab, {
      props: { characters: [baseCharacter({ skills: [] })] },
    });
    expect(wrapper.text()).toContain('Aucun sort équipé.');
  });

  it('shows an empty-state message when no characters exist', () => {
    const wrapper = mount(TeamOverviewTab, { props: { characters: [] } });
    expect(wrapper.text()).toContain('Aucun personnage disponible.');
  });
});
