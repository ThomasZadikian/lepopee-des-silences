// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import SkillManagementTab from './SkillManagementTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { skillsApi } from '../../../party/api/skillsApi';
import { playerApi } from '../../../party/api/playerApi';
import { demoPlayerId } from '../../../runs/stores/runStore';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';

vi.mock('../../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
  },
}));

vi.mock('../../../party/api/skillsApi', () => ({
  skillsApi: {
    listActive: vi.fn(),
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
    skills: [
      { skillKey: 'skill.a', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: true },
      { skillKey: 'skill.b', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: false },
    ],
    stats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
    },
  };
}

describe('SkillManagementTab', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.mocked(skillsApi.listActive).mockResolvedValue({
      skills: [
        { key: 'skill.a', displayName: 'Frappe', description: 'Un coup.', skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10 },
        { key: 'skill.b', displayName: 'Garde', description: 'Se protège.', skillType: 'Guard', targetingType: 'Self', effectType: 'Buff', manaCost: 0, chargeCost: 0, basePower: 0 },
        { key: 'skill.c', displayName: 'Mystère', description: 'Un sort inconnu du joueur.', skillType: 'Weird', targetingType: 'SingleEnemy', effectType: 'Curious', manaCost: 3, chargeCost: 0, basePower: 5 },
      ],
    });
  });

  it('splits skills into equipped, known, and all-game sections', async () => {
    const wrapper = mount(SkillManagementTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const titles = wrapper.findAll('.smk-section__title').map((t) => t.text());
    expect(titles[0]).toContain('Sorts équipés');
    expect(titles[1]).toContain('Sorts connus');
    expect(titles[2]).toContain('Tous les sorts du jeu');
  });

  it('shows only equipped skills in the first section', async () => {
    const wrapper = mount(SkillManagementTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const equippedSection = wrapper.findAll('.smk-section')[0];
    expect(equippedSection.text()).toContain('Frappe');
    expect(equippedSection.text()).not.toContain('Garde');
  });

  it('marks game-wide skills as known or unknown relative to the character', async () => {
    const wrapper = mount(SkillManagementTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const allSkillsSection = wrapper.findAll('.smk-section')[2];
    const rows = allSkillsSection.findAll('.smk-row');
    expect(rows[0].text()).toContain('Connu');
    expect(rows[2].text()).toContain('Inconnu');
  });

  it('falls back to the raw effectType label for an unrecognized category', async () => {
    const wrapper = mount(SkillManagementTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const allSkillsSection = wrapper.findAll('.smk-section')[2];
    expect(allSkillsSection.text()).toContain('Curious');
  });

  it('toggles equip state when clicking a known-skill button', async () => {
    vi.mocked(playerApi.equipSkill).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    });
    const character = baseCharacter();
    usePlayerStore().profile = {
      id: 'player-1',
      displayName: 'Test',
      characters: [character],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    };
    const wrapper = mount(SkillManagementTab, { props: { character } });
    await flushPromises();

    const knownSection = wrapper.findAll('.smk-section')[1];
    const buttons = knownSection.findAll('.smk-toggle');
    await buttons[1].trigger('click'); // skill.b, currently unequipped

    expect(playerApi.equipSkill).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'skill.b');
  });
});
