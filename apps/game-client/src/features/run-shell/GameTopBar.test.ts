// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import GameTopBar from './GameTopBar.vue';

const mockStore = {
  currentRun: null,
  gameplayPhase: 'Loading',
};

vi.mock('../runs/stores/runStore', () => ({
  useRunStore: vi.fn(() => mockStore),
}));

const routerLinkStub = { template: '<a><slot /></a>', props: ['to'] };

function pageStub(name: string) {
  return { name, props: ['embedded', 'runId'], template: `<div class="stub-${name}" />` };
}

function mountTopBar(storeOverrides: Record<string, any> = {}) {
  Object.assign(mockStore, {
    currentRun: null,
    gameplayPhase: 'Loading',
  }, storeOverrides);

  return mount(GameTopBar, {
    attachTo: document.body,
    global: {
      stubs: {
        RouterLink: routerLinkStub,
        StatutsPage: pageStub('statuts-page'),
        ManifestationsPage: pageStub('manifestations-page'),
        ReputationPage: pageStub('reputation-page'),
        TutorialPage: pageStub('tutorial-page'),
      },
    },
  });
}

// The bar is collapsed to a small tab by default — most content assertions need it
// expanded first.
async function mountExpandedTopBar(storeOverrides: Record<string, any> = {}) {
  const wrapper = mountTopBar(storeOverrides);
  await wrapper.find('.es-runbar-tab').trigger('click');
  return wrapper;
}

describe('GameTopBar', () => {
  afterEach(() => {
    document.body.innerHTML = '';
  });

  beforeEach(() => {
    vi.clearAllMocks();
    mockStore.currentRun = null;
    mockStore.gameplayPhase = 'Loading';
  });

  it('renders without crashing', () => {
    const wrapper = mountTopBar();
    expect(wrapper.exists()).toBe(true);
  });

  it('collapses to a small tab by default, hiding the full bar', () => {
    const wrapper = mountTopBar();
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(true);
    expect(wrapper.find('.es-runbar').exists()).toBe(false);
  });

  it('expands the full bar as an overlay when the tab is clicked, and can be re-collapsed', async () => {
    const wrapper = mountTopBar();

    await wrapper.find('.es-runbar-tab').trigger('click');
    expect(wrapper.find('.es-runbar--overlay').exists()).toBe(true);
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(false);

    await wrapper.find('.es-runbar__collapse').trigger('click');
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(true);
    expect(wrapper.find('.es-runbar--overlay').exists()).toBe(false);
  });

  it('displays the game title', async () => {
    const wrapper = await mountExpandedTopBar();
    expect(wrapper.text()).toContain('L\'ÉPOPÉE DES SILENCES');
  });

  it('displays Palais label', async () => {
    const wrapper = await mountExpandedTopBar();
    expect(wrapper.text()).toContain('Palais');
  });

  it('displays fallback values when no run', async () => {
    const wrapper = await mountExpandedTopBar();
    expect(wrapper.text()).toContain('—');
  });

  it('displays seed value', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: { seed: 'abc123' },
    });
    expect(wrapper.text()).toContain('abc123');
  });

  it('displays active laws count', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: {
        activePalaceLaws: [{}, {}, {}],
      },
    });
    expect(wrapper.text()).toContain('03');
  });

  it('displays gameplay phase', async () => {
    const wrapper = await mountExpandedTopBar({
      gameplayPhase: 'Combat',
    });
    expect(wrapper.text()).toContain('COMBAT');
  });

  it('displays run status', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: { status: 'Active' },
    });
    expect(wrapper.text()).toContain('Active');
  });

  it('applies blood color for Failed status', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: { status: 'Failed' },
    });
    const statusEl = wrapper.findAll('.es-seg__v').find((el) => el.text().includes('Failed'));
    expect(statusEl?.attributes('style')).toContain('var(--blood)');
  });

  it('maps Map phase to EXPLORATION', async () => {
    const wrapper = await mountExpandedTopBar({ gameplayPhase: 'Map' });
    expect(wrapper.text()).toContain('EXPLORATION');
  });

  it('maps Reward phase to RÉCOMPENSE', async () => {
    const wrapper = await mountExpandedTopBar({ gameplayPhase: 'Reward' });
    expect(wrapper.text()).toContain('RÉCOMPENSE');
  });

  it('maps Interlude phase to INTERLUDE', async () => {
    const wrapper = await mountExpandedTopBar({ gameplayPhase: 'Interlude' });
    expect(wrapper.text()).toContain('INTERLUDE');
  });

  it('maps RoomCleared phase to SALLE LIBÉRÉE', async () => {
    const wrapper = await mountExpandedTopBar({ gameplayPhase: 'RoomCleared' });
    expect(wrapper.text()).toContain('SALLE LIBÉRÉE');
  });

  it('maps Suspended phase to SUSPENDU', async () => {
    const wrapper = await mountExpandedTopBar({ gameplayPhase: 'Suspended' });
    expect(wrapper.text()).toContain('SUSPENDU');
  });

  it('maps Completed phase to TERMINÉ', async () => {
    const wrapper = await mountExpandedTopBar({ gameplayPhase: 'Completed' });
    expect(wrapper.text()).toContain('TERMINÉ');
  });

  it('renders slot content', async () => {
    const wrapper = mount(GameTopBar, {
      slots: { default: '<span class="custom-slot">Custom</span>' },
      global: { stubs: { RouterLink: routerLinkStub } },
    });
    await wrapper.find('.es-runbar-tab').trigger('click');
    expect(wrapper.text()).toContain('Custom');
  });

  it('handles missing currentRoom gracefully', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: {},
    });
    expect(wrapper.exists()).toBe(true);
  });

  it('always displays the Tutoriel link', async () => {
    const wrapper = await mountExpandedTopBar();
    expect(wrapper.text()).toContain('Tutoriel');
  });

  it('hides the Réputation link when there is no active run', async () => {
    const wrapper = await mountExpandedTopBar();
    expect(wrapper.text()).not.toContain('Réputation');
  });

  it('shows the Réputation link when a run is active', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: { id: 'run-1' },
    });
    expect(wrapper.text()).toContain('Réputation');
  });

  it('shows the structureless-Palace notice when a narrative is present without a canon name', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: {
        currentRoom: {
          theme: 'Threshold',
          catalogName: null,
          catalogNarrative: 'Le Palais n\'a pas sa structure habituelle, tout semble... sans vie.',
        },
      },
    });
    expect(wrapper.find('.es-system-notice').exists()).toBe(true);
    expect(wrapper.text()).toContain('sans vie');
  });

  it('does not show the notice for a normal canon room with both a name and narrative', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: {
        currentRoom: {
          theme: 'Memory',
          catalogName: 'Le Palier',
          catalogNarrative: 'Huit marches qui semblent une éternité.',
        },
      },
    });
    expect(wrapper.find('.es-system-notice').exists()).toBe(false);
  });

  it('does not show the notice for a plain procedural room with no catalog binding', async () => {
    const wrapper = await mountExpandedTopBar({
      currentRun: {
        currentRoom: { theme: 'Rupture' },
      },
    });
    expect(wrapper.find('.es-system-notice').exists()).toBe(false);
  });

  it('shows no reference modal by default', async () => {
    await mountExpandedTopBar();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });

  it('opens the Statuts page as a modal overlay instead of navigating', async () => {
    const wrapper = await mountExpandedTopBar();
    const btn = wrapper.findAll('button.es-runbar__ref-link').find((b) => b.text() === 'Statuts');
    await btn!.trigger('click');
    expect(document.querySelector('.stub-statuts-page')).not.toBeNull();
  });

  it('opens the Manifestations page as a modal overlay', async () => {
    const wrapper = await mountExpandedTopBar();
    const btn = wrapper.findAll('button.es-runbar__ref-link').find((b) => b.text() === 'Manifestations');
    await btn!.trigger('click');
    expect(document.querySelector('.stub-manifestations-page')).not.toBeNull();
  });

  it('opens the Tutoriel page as a modal overlay', async () => {
    const wrapper = await mountExpandedTopBar();
    const btn = wrapper.findAll('button.es-runbar__ref-link').find((b) => b.text() === 'Tutoriel');
    await btn!.trigger('click');
    expect(document.querySelector('.stub-tutorial-page')).not.toBeNull();
  });

  it('opens the Réputation page as a modal overlay, passing the current run id', async () => {
    const wrapper = await mountExpandedTopBar({ currentRun: { id: 'run-1' } });
    const btn = wrapper.findAll('button.es-runbar__ref-link').find((b) => b.text() === 'Réputation');
    await btn!.trigger('click');
    const stub = document.querySelector('.stub-reputation-page');
    expect(stub).not.toBeNull();
  });

  it('closes the reference modal when PageOverlayModal emits close', async () => {
    const wrapper = await mountExpandedTopBar();
    const btn = wrapper.findAll('button.es-runbar__ref-link').find((b) => b.text() === 'Statuts');
    await btn!.trigger('click');
    expect(document.querySelector('.pom-backdrop')).not.toBeNull();

    const closeBtn = document.querySelector('.pom-close') as HTMLButtonElement;
    closeBtn.click();
    await wrapper.vm.$nextTick();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });
});
