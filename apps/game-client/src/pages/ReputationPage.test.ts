// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import ReputationPage from './ReputationPage.vue';
import { reputationApi } from '../features/reputation/api/reputationApi';

const mockRoute: { params: Record<string, string> } = { params: { runId: 'run-1' } };
const mockRouter = { back: vi.fn() };

vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
  useRouter: () => mockRouter,
}));

vi.mock('../features/reputation/api/reputationApi', () => ({
  reputationApi: {
    getRunReputation: vi.fn(),
  },
}));

function makeNpc(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    npcKey: 'npc.erina',
    displayName: 'Erina',
    emotionalRegister: 'Rupture',
    relationshipScore: 300,
    aggregateState: 'Latent',
    timesMet: 2,
    offerings: [
      {
        key: 'offer.erina.reve',
        kind: 'Item',
        isMajor: true,
        requiredRelationshipScore: 250,
        scoreThresholdMet: true,
      },
      {
        key: 'offer.erina.liberte',
        kind: 'Skill',
        isMajor: true,
        requiredRelationshipScore: 1000,
        scoreThresholdMet: false,
      },
    ],
    ...overrides,
  };
}

describe('ReputationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockRoute.params = { runId: 'run-1' };
  });

  it('shows a loading state while fetching', async () => {
    vi.mocked(reputationApi.getRunReputation).mockReturnValue(new Promise(() => {}));

    const wrapper = mount(ReputationPage);
    await flushPromises();

    expect(wrapper.text()).toContain('Chargement');
  });

  it('renders a card per NPC once loaded', async () => {
    vi.mocked(reputationApi.getRunReputation).mockResolvedValueOnce({
      runId: 'run-1',
      npcs: [makeNpc()],
    });

    const wrapper = mount(ReputationPage);
    await flushPromises();

    expect(wrapper.findAll('.reputation-card')).toHaveLength(1);
    expect(wrapper.text()).toContain('Erina');
    expect(wrapper.text()).toContain('300');
  });

  it('shows offering unlock status', async () => {
    vi.mocked(reputationApi.getRunReputation).mockResolvedValueOnce({
      runId: 'run-1',
      npcs: [makeNpc()],
    });

    const wrapper = mount(ReputationPage);
    await flushPromises();

    const offerings = wrapper.findAll('.reputation-offering');
    expect(offerings).toHaveLength(2);
    expect(offerings[0].text()).toContain('Atteint');
    expect(offerings[1].text()).toContain('Non atteint');
  });

  it('shows an empty state when no NPC has been met', async () => {
    vi.mocked(reputationApi.getRunReputation).mockResolvedValueOnce({
      runId: 'run-1',
      npcs: [],
    });

    const wrapper = mount(ReputationPage);
    await flushPromises();

    expect(wrapper.text()).toContain("Vous n'avez encore croisé personne");
  });

  it('shows an error message when the request fails', async () => {
    vi.mocked(reputationApi.getRunReputation).mockRejectedValueOnce(new Error('boom'));

    const wrapper = mount(ReputationPage);
    await flushPromises();

    expect(wrapper.text()).toContain('boom');
  });

  it('does not call the API when there is no active run', async () => {
    mockRoute.params = {};

    const wrapper = mount(ReputationPage);
    await flushPromises();

    expect(reputationApi.getRunReputation).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Aucune run active');
  });
});
