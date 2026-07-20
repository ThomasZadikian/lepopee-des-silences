// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import GameTopBar from './GameTopBar.vue';

const mockStore = {
  currentRun: null,
  gameplayPhase: 'Loading',
  isTacticalMode: false,
};

vi.mock('../runs/stores/runStore', () => ({
  useRunStore: vi.fn(() => mockStore),
}));

const routerLinkStub = { template: '<a><slot /></a>', props: ['to'] };

function mountTopBar(storeOverrides: Record<string, any> = {}) {
  Object.assign(mockStore, {
    currentRun: null,
    gameplayPhase: 'Loading',
    isTacticalMode: false,
  }, storeOverrides);

  return mount(GameTopBar, {
    global: { stubs: { RouterLink: routerLinkStub } },
  });
}

describe('GameTopBar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockStore.currentRun = null;
    mockStore.gameplayPhase = 'Loading';
    mockStore.isTacticalMode = false;
  });

  it('renders without crashing', () => {
    const wrapper = mountTopBar();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the game title', () => {
    const wrapper = mountTopBar();
    expect(wrapper.text()).toContain('L\'ÉPOPÉE DES SILENCES');
  });

  it('displays Palais label', () => {
    const wrapper = mountTopBar();
    expect(wrapper.text()).toContain('Palais');
  });

  it('displays fallback values when no run', () => {
    const wrapper = mountTopBar();
    expect(wrapper.text()).toContain('—');
  });

  it('displays room name when run exists', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { theme: 'Salle sombre', roomType: 'Combat' },
      },
    });
    expect(wrapper.text()).toContain('Salle sombre');
  });

  it('falls back to roomType when theme is absent', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { roomType: 'Combat' },
      },
    });
    expect(wrapper.text()).toContain('Combat');
  });

  it('displays seed value', () => {
    const wrapper = mountTopBar({
      currentRun: { seed: 'abc123' },
    });
    expect(wrapper.text()).toContain('abc123');
  });

  it('displays depth progression', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { currentNodeDepth: 1, maxNodeDepth: 4 },
      },
    });
    expect(wrapper.text()).toContain('02 / 05');
  });

  it('displays active laws count', () => {
    const wrapper = mountTopBar({
      currentRun: {
        activePalaceLaws: [{}, {}, {}],
      },
    });
    expect(wrapper.text()).toContain('03');
  });

  it('displays gameplay phase', () => {
    const wrapper = mountTopBar({
      gameplayPhase: 'Combat',
    });
    expect(wrapper.text()).toContain('COMBAT');
  });

  it('displays run status', () => {
    const wrapper = mountTopBar({
      currentRun: { status: 'Active' },
    });
    expect(wrapper.text()).toContain('Active');
  });

  it('applies blood color for Failed status', () => {
    const wrapper = mountTopBar({
      currentRun: { status: 'Failed' },
    });
    const statusEl = wrapper.findAll('.es-seg__v').find((el) => el.text().includes('Failed'));
    expect(statusEl?.attributes('style')).toContain('var(--blood)');
  });

  it('maps Map phase to EXPLORATION', () => {
    const wrapper = mountTopBar({ gameplayPhase: 'Map' });
    expect(wrapper.text()).toContain('EXPLORATION');
  });

  it('maps Reward phase to RÉCOMPENSE', () => {
    const wrapper = mountTopBar({ gameplayPhase: 'Reward' });
    expect(wrapper.text()).toContain('RÉCOMPENSE');
  });

  it('maps Interlude phase to INTERLUDE', () => {
    const wrapper = mountTopBar({ gameplayPhase: 'Interlude' });
    expect(wrapper.text()).toContain('INTERLUDE');
  });

  it('maps RoomCleared phase to SALLE LIBÉRÉE', () => {
    const wrapper = mountTopBar({ gameplayPhase: 'RoomCleared' });
    expect(wrapper.text()).toContain('SALLE LIBÉRÉE');
  });

  it('maps Suspended phase to SUSPENDU', () => {
    const wrapper = mountTopBar({ gameplayPhase: 'Suspended' });
    expect(wrapper.text()).toContain('SUSPENDU');
  });

  it('maps Completed phase to TERMINÉ', () => {
    const wrapper = mountTopBar({ gameplayPhase: 'Completed' });
    expect(wrapper.text()).toContain('TERMINÉ');
  });

  it('renders slot content', () => {
    const wrapper = mount(GameTopBar, {
      slots: { default: '<span class="custom-slot">Custom</span>' },
      global: { stubs: { RouterLink: routerLinkStub } },
    });
    expect(wrapper.text()).toContain('Custom');
  });

  it('hides the depth segment in Tactical mode', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { currentNodeDepth: 1, maxNodeDepth: 4 },
      },
      isTacticalMode: true,
    });
    expect(wrapper.text()).not.toContain('Profondeur');
  });

  it('shows the depth segment in Classic mode', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { currentNodeDepth: 1, maxNodeDepth: 4 },
      },
      isTacticalMode: false,
    });
    expect(wrapper.text()).toContain('Profondeur');
  });

  it('collapses to a small tab by default in Tactical mode, hiding the full bar', () => {
    const wrapper = mountTopBar({ isTacticalMode: true });
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(true);
    expect(wrapper.find('.es-runbar').exists()).toBe(false);
  });

  it('does not collapse by default in Classic mode', () => {
    const wrapper = mountTopBar({ isTacticalMode: false });
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(false);
    expect(wrapper.find('.es-runbar').exists()).toBe(true);
  });

  it('expands the full bar as an overlay when the tab is clicked, and can be re-collapsed', async () => {
    const wrapper = mountTopBar({ isTacticalMode: true });

    await wrapper.find('.es-runbar-tab').trigger('click');
    expect(wrapper.find('.es-runbar--overlay').exists()).toBe(true);
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(false);

    await wrapper.find('.es-runbar__collapse').trigger('click');
    expect(wrapper.find('.es-runbar-tab').exists()).toBe(true);
    expect(wrapper.find('.es-runbar--overlay').exists()).toBe(false);
  });

  it('pads depth values with leading zeros', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { currentNodeDepth: 0, maxNodeDepth: 9 },
      },
    });
    expect(wrapper.text()).toContain('01 / 10');
  });

  it('handles missing currentRoom gracefully', () => {
    const wrapper = mountTopBar({
      currentRun: {},
    });
    expect(wrapper.exists()).toBe(true);
  });

  it('always displays the Tutoriel link', () => {
    const wrapper = mountTopBar();
    expect(wrapper.text()).toContain('Tutoriel');
  });

  it('hides the Réputation link when there is no active run', () => {
    const wrapper = mountTopBar();
    expect(wrapper.text()).not.toContain('Réputation');
  });

  it('shows the Réputation link when a run is active', () => {
    const wrapper = mountTopBar({
      currentRun: { id: 'run-1' },
    });
    expect(wrapper.text()).toContain('Réputation');
  });

  it('shows the structureless-Palace notice when a narrative is present without a canon name', () => {
    const wrapper = mountTopBar({
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

  it('does not show the notice for a normal canon room with both a name and narrative', () => {
    const wrapper = mountTopBar({
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

  it('does not show the notice for a plain procedural room with no catalog binding', () => {
    const wrapper = mountTopBar({
      currentRun: {
        currentRoom: { theme: 'Rupture' },
      },
    });
    expect(wrapper.find('.es-system-notice').exists()).toBe(false);
  });
});
