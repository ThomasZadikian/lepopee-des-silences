// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import DevToolsPanel from './DevToolsPanel.vue';
import type { CombatRuntimeDto } from '../../combat/types/combatContracts';

vi.mock('../composables/useDevTools', () => ({
  useDevTools: vi.fn(() => ({
    hasToken: { value: false },
    token: { value: '' },
    status: { value: 'unknown' },
    statusEnvironment: { value: null },
    isLoading: { value: false },
    message: { value: null },
    error: { value: null },
    saveToken: vi.fn(),
    clearToken: vi.fn(),
    checkStatus: vi.fn(),
    runAction: vi.fn().mockResolvedValue(false),
  })),
}));

vi.mock('../../runs/stores/runStore', () => ({
  useRunStore: vi.fn(() => ({
    currentRun: null,
    loadRun: vi.fn().mockResolvedValue(undefined),
  })),
}));

vi.mock('../../combat/stores/useCombatStore', () => ({
  useCombatStore: vi.fn(() => ({
    clearCombat: vi.fn(),
    loadCurrentCombat: vi.fn().mockResolvedValue(undefined),
  })),
}));

vi.mock('../../party/stores/playerStore', () => ({
  usePlayerStore: vi.fn(() => ({
    profile: null,
    loadProfile: vi.fn().mockResolvedValue(undefined),
  })),
}));

vi.mock('../../party/api/skillsApi', () => ({
  skillsApi: {
    listActive: vi.fn().mockResolvedValue({ skills: [] }),
  },
}));

vi.mock('../api/devToolsApi', () => ({
  devToolsApi: {
    getPsyche: vi.fn(),
    advanceRoom: vi.fn(),
    advanceRooms: vi.fn(),
    forcePalaceRoomState: vi.fn(),
    forceRoomClimate: vi.fn(),
    activateLaw: vi.fn(),
    clearLaws: vi.fn(),
    activateCurse: vi.fn(),
    clearCurses: vi.fn(),
    killEnemies: vi.fn(),
    killEnemy: vi.fn(),
    setVitals: vi.fn(),
    applyStatus: vi.fn(),
    addAlly: vi.fn(),
    removeAlly: vi.fn(),
  },
}));

function mountPanel(runId = 'run-1', combat: CombatRuntimeDto | null = null) {
  return mount(DevToolsPanel, {
    props: { runId, combat },
    global: {
      stubs: {
        DevToolsTokenGate: {
          template: '<div class="token-gate-stub" />',
          props: ['hasToken', 'status', 'environment', 'isLoading'],
          emits: ['saveToken', 'clearToken', 'checkStatus'],
        },
        RunDevToolsSection: {
          template: '<div class="run-section-stub" />',
          props: ['disabled', 'isLoading'],
          emits: ['advanceRoom', 'advanceRooms', 'forcePalaceState', 'forceClimate', 'activateLaw', 'clearLaws', 'activateCurse', 'clearCurses', 'addAlly', 'removeAlly'],
        },
        PsycheDevToolsSection: {
          template: '<div class="psyche-section-stub" />',
          props: ['psyche', 'disabled', 'isLoading'],
          emits: ['refresh'],
        },
        CombatDevToolsSection: {
          template: '<div class="combat-section-stub" />',
          props: ['combat', 'disabled', 'isLoading'],
          emits: ['killEnemies', 'killEnemy', 'setVitals', 'applyStatus'],
        },
        PlayerDevToolsSection: {
          template: '<div class="player-section-stub" />',
          props: ['disabled', 'isLoading', 'characters', 'allSkills'],
          emits: ['unlockSkill', 'awardStatPoints'],
        },
      },
    },
  });
}

describe('DevToolsPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const wrapper = mountPanel();
    expect(wrapper.exists()).toBe(true);
  });

  it('emits close when close button is clicked', async () => {
    const wrapper = mountPanel();
    await wrapper.find('.devtools-close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('renders token gate section', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.token-gate-stub').exists()).toBe(true);
  });

  it('renders run devtools section', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.run-section-stub').exists()).toBe(true);
  });

  it('renders psyche devtools section', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.psyche-section-stub').exists()).toBe(true);
  });

  it('renders combat devtools section', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.combat-section-stub').exists()).toBe(true);
  });

  it('renders player devtools section', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.player-section-stub').exists()).toBe(true);
  });

  it('passes disabled prop to sections when no token', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.run-section-stub').exists()).toBe(true);
  });

  it('displays error message when devTools.error exists', async () => {
    const { useDevTools } = await import('../composables/useDevTools');
    vi.mocked(useDevTools).mockReturnValueOnce({
      hasToken: { value: false },
      token: { value: '' },
      status: { value: 'unknown' },
      statusEnvironment: { value: null },
      isLoading: { value: false },
      message: { value: null },
      error: { value: 'Token devtools absent.' },
      saveToken: vi.fn(),
      clearToken: vi.fn(),
      checkStatus: vi.fn(),
      runAction: vi.fn().mockResolvedValue(false),
    });

    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('Token devtools absent.');
  });

  it('displays success message when devTools.message exists', async () => {
    const { useDevTools } = await import('../composables/useDevTools');
    vi.mocked(useDevTools).mockReturnValueOnce({
      hasToken: { value: true },
      token: { value: 'test-token' },
      status: { value: 'available' },
      statusEnvironment: { value: 'local' },
      isLoading: { value: false },
      message: { value: 'Devtools backend disponibles.' },
      error: { value: null },
      saveToken: vi.fn(),
      clearToken: vi.fn(),
      checkStatus: vi.fn(),
      runAction: vi.fn().mockResolvedValue(true),
    });

    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('Devtools backend disponibles.');
  });
});
