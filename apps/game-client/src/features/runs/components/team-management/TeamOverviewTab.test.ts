// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';

import TeamOverviewTab from './TeamOverviewTab.vue';
import { skillsApi } from '../../../party/api/skillsApi';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';

vi.mock('../../../party/api/skillsApi', () => ({
  skillsApi: {
    listActive: vi.fn(),
  },
}));

function baseCharacter(overrides: Partial<PlayerCharacterView> = {}): PlayerCharacterView {
  return {
    id: 'char-1',
    definitionKey: 'character.player.self',
    displayName: 'Le Porteur',
    maxEquippedSkills: 4,
    items: [],
    maxEquippedItems: 3,
    characterType: 'Standard',
    skills: [
      { skillKey: 'skill.a', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: true },
    ],
    stats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0,
      magicAttack: 0, magicDefense: 0,
    },
    ...overrides,
  };
}

describe('TeamOverviewTab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: [] });
  });

  it('resolves the catalog display name instead of the raw skill key', async () => {
    vi.mocked(skillsApi.listActive).mockResolvedValue({
      skills: [
        {
          key: 'skill.a', displayName: 'Frappe', description: '', skillType: 'Damage',
          targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10,
        },
      ],
    });
    const wrapper = mount(TeamOverviewTab, { props: { characters: [baseCharacter()] } });
    await flushPromises();

    expect(wrapper.text()).toContain('Frappe');
    expect(wrapper.text()).not.toContain('skill.a');
  });

  it('renders a card per character', () => {
    const wrapper = mount(TeamOverviewTab, { props: { characters: [baseCharacter()] } });
    expect(wrapper.findAll('.tov-card')).toHaveLength(1);
    expect(wrapper.text()).toContain('Le Porteur');
  });

  it('renders all 11 canonical stats', () => {
    const wrapper = mount(TeamOverviewTab, { props: { characters: [baseCharacter()] } });
    expect(wrapper.findAll('.tov-stat')).toHaveLength(11);
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

  it('shows a "Compagnon" badge for recruited companions', () => {
    const wrapper = mount(TeamOverviewTab, {
      props: { characters: [baseCharacter({ characterType: 'Companion' })] },
    });
    expect(wrapper.text()).toContain('Compagnon');
  });

  it('does not show a "Compagnon" badge for the protagonist', () => {
    const wrapper = mount(TeamOverviewTab, {
      props: { characters: [baseCharacter({ characterType: 'Standard' })] },
    });
    expect(wrapper.text()).not.toContain('Compagnon');
  });
});
