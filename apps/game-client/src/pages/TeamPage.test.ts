// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import TeamPage from './TeamPage.vue';
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
    equipItem: vi.fn(),
    unequipItem: vi.fn(),
  },
}));

vi.mock('../features/party/api/skillsApi', () => ({
  skillsApi: {
    listActive: vi.fn().mockResolvedValue({ skills: [] }),
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

describe('TeamPage', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    // La page rafraîchit systématiquement le profil à l'ouverture (voir TeamPage.vue) —
    // sans quoi ce mock non résolu écraserait le profil préréglé par chaque test avec undefined.
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
  });

  it('shows the page title', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamPage);
    await flushPromises();

    expect(wrapper.text()).toContain('Équipe');
  });

  it('renders the team overview once the profile is loaded', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamPage);
    await flushPromises();

    expect(wrapper.text()).toContain('Le Porteur');
  });

  it('goes back in history when the back button is clicked', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamPage);
    await flushPromises();

    await wrapper.find('.team-page__back').trigger('click');
    expect(mockRouter.back).toHaveBeenCalledOnce();
  });

  it('refreshes a stale cached profile on mount, instead of leaving a just-recruited companion invisible', async () => {
    // A profile loaded earlier this session (e.g. before a companion NPC event resolved)
    // must not survive as the truth once this page is opened — see TeamPage.vue.
    usePlayerStore().profile = baseProfile();
    const freshProfile = baseProfile();
    freshProfile.characters.push({
      ...freshProfile.characters[0],
      id: 'char-2',
      displayName: 'La Compagne',
    });
    vi.mocked(playerApi.getProfile).mockResolvedValue(freshProfile);

    const wrapper = mount(TeamPage);
    await flushPromises();

    expect(wrapper.text()).toContain('La Compagne');
  });
});
