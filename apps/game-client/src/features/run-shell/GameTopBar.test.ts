// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
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

function mountTopBar(storeOverrides: Record<string, any> = {}) {
  Object.assign(mockStore, {
    currentRun: null,
    gameplayPhase: 'Loading',
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
});
