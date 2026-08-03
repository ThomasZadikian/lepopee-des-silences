// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import StatsPage from './StatsPage.vue';
import { usePlayerStore } from '../features/party/stores/playerStore';
import { playerApi } from '../features/party/api/playerApi';
import type { PlayerProfileView } from '../features/party/types/playerTypes';

const mockRouter = { back: vi.fn() };
vi.mock('vue-router', () => ({
  useRouter: () => mockRouter,
}));

vi.mock('../features/party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
  },
}));

vi.mock('../features/runs/api/runApi', () => ({
  runApi: {
    startRun: vi.fn(), getRun: vi.fn(), resolveCurrentEvent: vi.fn(),
    progressRun: vi.fn(), generateNextNodes: vi.fn(), enterInterlude: vi.fn(), getInterlude: vi.fn(),
    enterNextRoom: vi.fn(), saveAndExitRun: vi.fn(), resumeRun: vi.fn(), exitMidRoom: vi.fn(),
    abandonRun: vi.fn(), getPermanentItemCandidates: vi.fn(), confirmPermanentItemSelection: vi.fn(),
    removePalaceLaw: vi.fn(), useCaliceInfini: vi.fn(), syncPartySkills: vi.fn(), syncPartyStats: vi.fn(),
  },
}));

function baseProfile(): PlayerProfileView {
  return {
    id: 'player-1',
    displayName: 'Test Player',
    characters: [
      {
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
      },
    ],
    progression: { unspentStatPoints: 2, totalStatPointsEarned: 3, palaceShardCount: 0 },
    permanentItems: [],
  };
}

describe('StatsPage', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    // La page rafraîchit systématiquement le profil à l'ouverture (voir StatsPage.vue) —
    // sans quoi ce mock non résolu écraserait le profil préréglé par chaque test avec undefined.
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
  });

  it('shows the page title and the stats content once loaded', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(StatsPage);
    await flushPromises();

    expect(wrapper.text()).toContain('Statistiques');
    expect(wrapper.find('.stat-radar').exists()).toBe(true);
  });

  it('does not show a character picker with only one character', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(StatsPage);
    await flushPromises();

    expect(wrapper.find('.character-picker').exists()).toBe(false);
  });

  it('goes back in history when the back button is clicked', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(StatsPage);
    await flushPromises();

    await wrapper.find('.stats-page__back').trigger('click');
    expect(mockRouter.back).toHaveBeenCalledOnce();
  });

  it('refreshes a stale cached profile on mount, instead of leaving a just-recruited companion invisible', async () => {
    // A profile loaded earlier this session (e.g. before a companion NPC event resolved)
    // must not survive as the truth once this page is opened — see StatsPage.vue.
    usePlayerStore().profile = baseProfile();
    const freshProfile = baseProfile();
    freshProfile.characters.push({
      ...freshProfile.characters[0],
      id: 'char-2',
      displayName: 'La Compagne',
    });
    vi.mocked(playerApi.getProfile).mockResolvedValue(freshProfile);

    const wrapper = mount(StatsPage);
    await flushPromises();

    expect(wrapper.find('.character-picker').exists()).toBe(true);
  });
});
