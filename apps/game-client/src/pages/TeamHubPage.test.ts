// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import TeamHubPage from './TeamHubPage.vue';
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

vi.mock('../features/party/api/itemsApi', () => ({
  itemsApi: {
    listActive: vi.fn().mockResolvedValue({ items: [] }),
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

vi.mock('../features/inventory/api/inventoryApi', () => ({
  inventoryApi: {
    useItem: vi.fn().mockResolvedValue({}),
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
          speed: 10, initiative: 10, focus: 0, mana: 0, charge: 0,
        },
      },
    ],
    progression: { unspentStatPoints: 2, totalStatPointsEarned: 3, palaceShardCount: 0 },
    permanentItems: [],
  };
}

describe('TeamHubPage', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    // Rafraîchi systématiquement à l'ouverture (voir TeamHubPage.vue) — sans quoi ce mock
    // non résolu écraserait le profil préréglé par chaque test avec undefined.
    vi.mocked(playerApi.getProfile).mockResolvedValue(baseProfile());
  });

  it('shows the page title', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    expect(wrapper.text()).toContain('Équipe');
  });

  it('defaults to the Équipe tab and renders the team overview', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    expect(wrapper.text()).toContain('Le Porteur');
  });

  it('goes back in history when the back button is clicked', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    await wrapper.find('.team-hub__back').trigger('click');
    expect(mockRouter.back).toHaveBeenCalledOnce();
  });

  it('refreshes a stale cached profile on mount, instead of leaving a just-recruited companion invisible', async () => {
    usePlayerStore().profile = baseProfile();
    const freshProfile = baseProfile();
    freshProfile.characters.push({
      ...freshProfile.characters[0],
      id: 'char-2',
      displayName: 'La Compagne',
    });
    vi.mocked(playerApi.getProfile).mockResolvedValue(freshProfile);

    const wrapper = mount(TeamHubPage);
    await flushPromises();

    expect(wrapper.text()).toContain('La Compagne');
  });

  it('switches to the Statistiques tab and shows the radar chart', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    const tab = wrapper.findAll('.team-hub__tab').find((b) => b.text() === 'Statistiques');
    await tab!.trigger('click');

    expect(wrapper.find('.stat-radar').exists()).toBe(true);
  });

  it('does not show a character picker with only one character', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    const tab = wrapper.findAll('.team-hub__tab').find((b) => b.text() === 'Statistiques');
    await tab!.trigger('click');

    expect(wrapper.find('.character-picker').exists()).toBe(false);
  });

  it('shows a character picker with several characters on a character-scoped tab', async () => {
    const profile = baseProfile();
    profile.characters.push({ ...profile.characters[0], id: 'char-2', displayName: 'La Compagne' });
    usePlayerStore().profile = profile;
    vi.mocked(playerApi.getProfile).mockResolvedValue(profile);

    const wrapper = mount(TeamHubPage);
    await flushPromises();

    const tab = wrapper.findAll('.team-hub__tab').find((b) => b.text() === 'Statistiques');
    await tab!.trigger('click');

    expect(wrapper.find('.character-picker').exists()).toBe(true);
  });

  it('switches to the Grimoire tab and shows the loadout controls', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    const tab = wrapper.findAll('.team-hub__tab').find((b) => b.text() === 'Grimoire');
    await tab!.trigger('click');

    expect(wrapper.text()).toContain('Valider les choix');
  });

  it('switches to the Équipement tab and shows the equipped-items section', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    const tab = wrapper.findAll('.team-hub__tab').find((b) => b.text() === 'Équipement');
    await tab!.trigger('click');

    expect(wrapper.text()).toContain('Objets équipés');
  });

  it('switches to the Besace tab and shows the empty-run status when no run is active', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage);
    await flushPromises();

    const tab = wrapper.findAll('.team-hub__tab').find((b) => b.text() === 'Besace');
    await tab!.trigger('click');

    expect(wrapper.text()).toContain('Aucune run active.');
  });

  it('opens directly on the requested tab via the initialTab prop', async () => {
    usePlayerStore().profile = baseProfile();
    const wrapper = mount(TeamHubPage, { props: { initialTab: 'grimoire' } });
    await flushPromises();

    expect(wrapper.text()).toContain('Valider les choix');
  });
});
