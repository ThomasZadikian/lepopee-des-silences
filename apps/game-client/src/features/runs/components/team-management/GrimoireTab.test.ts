// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import GrimoireTab from './GrimoireTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { useRunStore } from '../../stores/runStore';
import { skillsApi } from '../../../party/api/skillsApi';
import { playerApi } from '../../../party/api/playerApi';
import { runApi } from '../../api/runApi';
import { demoPlayerId } from '../../stores/runStore';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';

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

vi.mock('../../api/runApi', () => ({
  runApi: {
    startRun: vi.fn(),
    getRun: vi.fn(),
    chooseNode: vi.fn(),
    resolveCurrentEvent: vi.fn(),
    progressRun: vi.fn(),
    generateNextNodes: vi.fn(),
    enterInterlude: vi.fn(),
    getInterlude: vi.fn(),
    enterNextRoom: vi.fn(),
    saveAndExitRun: vi.fn(),
    resumeRun: vi.fn(),
    exitMidRoom: vi.fn(),
    abandonRun: vi.fn(),
    getPermanentItemCandidates: vi.fn(),
    confirmPermanentItemSelection: vi.fn(),
    removePalaceLaw: vi.fn(),
    useCaliceInfini: vi.fn(),
    syncPartySkills: vi.fn(),
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

function baseSkills(): SkillDefinitionView[] {
  return [
    {
      key: 'skill.a', displayName: 'Frappe', description: 'Un coup.', skillType: 'Damage',
      targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10,
      category: 'Physical', basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: [],
    },
    {
      key: 'skill.b', displayName: 'Garde', description: 'Se protège.', skillType: 'Guard',
      targetingType: 'Self', effectType: 'Buff', manaCost: 0, chargeCost: 0, basePower: 0,
      category: 'Physical', basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: [],
    },
    {
      key: 'skill.locked', displayName: 'Secret', description: 'Un sort verrouillé.', skillType: 'Damage',
      targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 3, chargeCost: 0, basePower: 20,
      category: 'Magic', basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: ['Offert par Hitomi'],
    },
  ];
}

function setPlayerProfile(characters: PlayerCharacterView[]) {
  usePlayerStore().profile = {
    id: 'player-1',
    displayName: 'Test',
    characters,
    progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
    permanentItems: [],
  };
}

describe('GrimoireTab', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: baseSkills() });
  });

  it('renders owned skills in color and locked skills with an acquisition hint', async () => {
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    expect(cards).toHaveLength(3);
    expect(cards[2].classes()).toContain('grimoire-card--locked');
    expect(cards[2].text()).toContain('Offert par Hitomi');
    expect(cards[0].classes()).not.toContain('grimoire-card--locked');
  });

  it('shows the equip toggle only for owned skills', async () => {
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    expect(cards[0].find('.grimoire-toggle').exists()).toBe(true);
    expect(cards[2].find('.grimoire-toggle').exists()).toBe(false);
  });

  it('stages equip/unequip without calling the API until Valider is clicked', async () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    await cards[1].find('.grimoire-toggle').trigger('click'); // equip skill.b

    expect(playerApi.equipSkill).not.toHaveBeenCalled();
    expect(cards[1].find('.grimoire-toggle').text()).toBe('Équipé');
  });

  it('Annuler resets the staged selection without calling the API', async () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    await cards[1].find('.grimoire-toggle').trigger('click');
    await wrapper.find('.grimoire-btn--ghost').trigger('click');

    expect(cards[1].find('.grimoire-toggle').text()).toBe('Équiper');
    expect(playerApi.equipSkill).not.toHaveBeenCalled();
  });

  it('Valider les choix replays only the diff and does not sync party skills without an active run', async () => {
    vi.mocked(playerApi.equipSkill).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    });
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    await cards[1].find('.grimoire-toggle').trigger('click'); // equip skill.b
    await wrapper.find('.grimoire-btn--primary').trigger('click');
    await flushPromises();

    expect(playerApi.equipSkill).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'skill.b');
    expect(playerApi.unequipSkill).not.toHaveBeenCalled();
    expect(runApi.syncPartySkills).not.toHaveBeenCalled();
  });

  it('Valider les choix also syncs the active run so the change applies mid-run', async () => {
    vi.mocked(playerApi.equipSkill).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    });
    vi.mocked(runApi.syncPartySkills).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
    } as any);
    setPlayerProfile([baseCharacter()]);
    useRunStore().currentRun = { id: 'run-1', status: 'Active' } as any;

    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    await cards[1].find('.grimoire-toggle').trigger('click');
    await wrapper.find('.grimoire-btn--primary').trigger('click');
    await flushPromises();

    expect(runApi.syncPartySkills).toHaveBeenCalledWith('run-1');
  });

  it('disables further equips once the pending loadout is full', async () => {
    const character = baseCharacter();
    character.maxEquippedSkills = 1; // already 1/1 equipped (skill.a)
    setPlayerProfile([character]);
    const wrapper = mount(GrimoireTab, { props: { character } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    const unequippedToggle = cards[1].find('.grimoire-toggle'); // skill.b
    expect(unequippedToggle.attributes('disabled')).toBeDefined();
  });
});
