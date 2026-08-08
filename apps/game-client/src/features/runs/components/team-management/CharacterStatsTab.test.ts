// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import CharacterStatsTab from './CharacterStatsTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { useRunStore } from '../../stores/runStore';
import { playerApi } from '../../../party/api/playerApi';
import { runApi } from '../../api/runApi';
import { demoPlayerId } from '../../stores/runStore';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';

vi.mock('../../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
  },
}));

vi.mock('../../api/runApi', () => ({
  runApi: {
    startRun: vi.fn(),
    getRun: vi.fn(),
    resolveCurrentEvent: vi.fn(),
    progressRun: vi.fn(),
    generateNextNodes: vi.fn(),
    confirmRoomExit: vi.fn(),
    saveAndExitRun: vi.fn(),
    resumeRun: vi.fn(),
    exitMidRoom: vi.fn(),
    abandonRun: vi.fn(),
    getPermanentItemCandidates: vi.fn(),
    confirmPermanentItemSelection: vi.fn(),
    removePalaceLaw: vi.fn(),
    useCaliceInfini: vi.fn(),
    syncPartySkills: vi.fn(),
    syncPartyStats: vi.fn(),
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
      speed: 10, initiative: 10,focus: 0, mana: 0, charge: 0,
    },
  };
}

function setPlayerProfile(characters: PlayerCharacterView[], unspentStatPoints = 3) {
  usePlayerStore().profile = {
    id: 'player-1',
    displayName: 'Test',
    characters,
    progression: { unspentStatPoints, totalStatPointsEarned: unspentStatPoints, palaceShardCount: 0 },
    permanentItems: [],
  };
}

describe('CharacterStatsTab', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it('renders the radar chart and one row per stat', () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(CharacterStatsTab, { props: { character: baseCharacter() } });

    expect(wrapper.find('.stat-radar').exists()).toBe(true);
    expect(wrapper.findAll('.cst-row').length).toBeGreaterThan(0);
  });

  it('stages a point without calling the API until Valider is clicked', async () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(CharacterStatsTab, { props: { character: baseCharacter() } });

    const attackRow = wrapper.findAll('.cst-row')[1]; // AttackPower is statOrder[1]
    await attackRow.find('.cst-row__step:last-child').trigger('click');

    expect(playerApi.spendStatPoint).not.toHaveBeenCalled();
    expect(attackRow.find('.cst-row__staged').text()).toBe('1');
  });

  it('disables staging further points once the available pool is exhausted', async () => {
    setPlayerProfile([baseCharacter()], 1);
    const wrapper = mount(CharacterStatsTab, { props: { character: baseCharacter() } });

    const rows = wrapper.findAll('.cst-row');
    await rows[1].find('.cst-row__step:last-child').trigger('click'); // spend the only point

    const otherRowPlusButton = rows[2].find('.cst-row__step:last-child');
    expect(otherRowPlusButton.attributes('disabled')).toBeDefined();
  });

  it('Annuler resets the staged selection without calling the API', async () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(CharacterStatsTab, { props: { character: baseCharacter() } });

    const attackRow = wrapper.findAll('.cst-row')[1];
    await attackRow.find('.cst-row__step:last-child').trigger('click');
    await wrapper.find('.cst-btn--ghost').trigger('click');

    expect(attackRow.find('.cst-row__staged').text()).toBe('0');
    expect(playerApi.spendStatPoint).not.toHaveBeenCalled();
  });

  it('Valider les choix replays one spendStatPoint call per staged point and does not sync without an active run', async () => {
    const character = baseCharacter();
    vi.mocked(playerApi.spendStatPoint).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [character],
      progression: { unspentStatPoints: 2, totalStatPointsEarned: 3, palaceShardCount: 0 },
      permanentItems: [],
    });
    setPlayerProfile([character]);
    const wrapper = mount(CharacterStatsTab, { props: { character } });

    const attackRow = wrapper.findAll('.cst-row')[1];
    await attackRow.find('.cst-row__step:last-child').trigger('click');
    await attackRow.find('.cst-row__step:last-child').trigger('click');
    await wrapper.find('.cst-btn--primary').trigger('click');
    await flushPromises();

    expect(playerApi.spendStatPoint).toHaveBeenCalledTimes(2);
    expect(playerApi.spendStatPoint).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'AttackPower');
    expect(runApi.syncPartyStats).not.toHaveBeenCalled();
  });

  it('Valider les choix also syncs the active run so the change applies mid-run', async () => {
    const character = baseCharacter();
    vi.mocked(playerApi.spendStatPoint).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [character],
      progression: { unspentStatPoints: 2, totalStatPointsEarned: 3, palaceShardCount: 0 },
      permanentItems: [],
    });
    vi.mocked(runApi.syncPartyStats).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
    } as any);
    setPlayerProfile([character]);
    useRunStore().currentRun = { id: 'run-1', status: 'Active' } as any;

    const wrapper = mount(CharacterStatsTab, { props: { character } });

    const attackRow = wrapper.findAll('.cst-row')[1];
    await attackRow.find('.cst-row__step:last-child').trigger('click');
    await wrapper.find('.cst-btn--primary').trigger('click');
    await flushPromises();

    expect(runApi.syncPartyStats).toHaveBeenCalledWith('run-1');
  });
});
