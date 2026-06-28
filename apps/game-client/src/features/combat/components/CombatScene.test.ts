// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import CombatScene from './CombatScene.vue';
import type { CombatRuntimeDto } from '../types/combatContracts';

const mockStore = {
  combat: null as CombatRuntimeDto | null,
  allies: [] as CombatRuntimeDto['allies'],
  enemies: [] as CombatRuntimeDto['enemies'],
  allCombatants: [] as CombatRuntimeDto['allies'],
  logEntries: [],
  isLoading: false,
  isResolvingAction: false,
  isPlayerTurn: false,
  selectedSkillKey: null as string | null,
  selectedSkill: null,
  selectedItem: null,
  selectedItemId: null as string | null,
  selectedTargetIds: [] as string[],
  validTargets: [],
  itemValidTargets: [],
  terminalEvent: null,
  canSubmit: false,
  canSubmitItem: false,
  error: null as string | null,
  recentlyDamagedIds: [] as string[],
  recentlyGuardedIds: [] as string[],
  recentlyDefeatedIds: [] as string[],
  recentlyActingId: null as string | null,
  thinkingCombatantId: null as string | null,
  currentActor: null,
  isSelectedTarget: () => false,
  isCurrentActor: () => false,
  findCombatantById: () => null,
  selectSkill: vi.fn(),
  selectItem: vi.fn(),
  selectTarget: vi.fn(),
  clearSelection: vi.fn(),
  clearItemSelection: vi.fn(),
  submitAction: vi.fn(),
  submitItemAction: vi.fn(),
  clearCombat: vi.fn(),
  loadCurrentCombat: vi.fn(),
  initCombat: vi.fn(),
  runCombatClock: vi.fn(),
  feedbackEvents: [],
};

vi.mock('../../runs/stores/runStore', () => ({
  useRunStore: vi.fn(() => ({
    combatRuntime: null,
  })),
}));

vi.mock('../stores/useCombatStore', () => ({
  useCombatStore: vi.fn(() => mockStore),
}));

vi.mock('../composables/useCombatLogMetrics', () => ({
  useCombatLogMetrics: vi.fn(() => ({
    state: {
      combatId: null,
      allies: { damageDealt: 0, damageTaken: 0, healingDone: 0, healingReceived: 0, guardAbsorbed: 0, guardGained: 0, netVitalityLoss: 0 },
      enemies: { damageDealt: 0, damageTaken: 0, healingDone: 0, healingReceived: 0, guardAbsorbed: 0, guardGained: 0, netVitalityLoss: 0 },
      contributions: {},
      floatEvents: [],
    },
  })),
}));

function makeCombatant(id: string, displayName: string, side: 'Player' | 'Enemy'): CombatRuntimeDto['allies'][number] {
  return {
    id,
    sourceKey: `source.${id}`,
    displayName,
    side,
    archetype: 'Fighter',
    maxVitality: 100,
    currentVitality: 100,
    guard: 0,
    mana: 0,
    charge: 0,
    status: 'Active',
    skills: [],
  };
}

function resetMockStore() {
  mockStore.combat = null;
  mockStore.allies = [];
  mockStore.enemies = [];
  mockStore.allCombatants = [];
  mockStore.logEntries = [];
  mockStore.isLoading = false;
  mockStore.isResolvingAction = false;
  mockStore.isPlayerTurn = false;
  mockStore.selectedSkillKey = null;
  mockStore.selectedSkill = null;
  mockStore.selectedItem = null;
  mockStore.selectedItemId = null;
  mockStore.selectedTargetIds = [];
  mockStore.validTargets = [];
  mockStore.itemValidTargets = [];
  mockStore.terminalEvent = null;
  mockStore.canSubmit = false;
  mockStore.canSubmitItem = false;
  mockStore.error = null;
  mockStore.recentlyDamagedIds = [];
  mockStore.recentlyGuardedIds = [];
  mockStore.recentlyDefeatedIds = [];
  mockStore.recentlyActingId = null;
  mockStore.thinkingCombatantId = null;
  mockStore.currentActor = null;
  mockStore.isSelectedTarget = () => false;
  mockStore.isCurrentActor = () => false;
  mockStore.findCombatantById = () => null;
  mockStore.feedbackEvents = [];
}

function configureMockStore(overrides: Record<string, any> = {}) {
  Object.assign(mockStore, overrides);
}

function mountScene(runId = 'run-1', combatId = 'combat-1', combatStoreOverrides: Record<string, any> = {}) {
  resetMockStore();
  configureMockStore(combatStoreOverrides);

  return mount(CombatScene, {
    props: { runId, combatId },
    global: {
      stubs: {
        CombatantCard: { template: '<button class="presence"><slot /></button>' },
        AtbGauge: { template: '<div class="atb" />' },
        CombatLogPanel: { template: '<div class="combat-log" />' },
        CombatMetersPanel: { template: '<div class="damage-report" />' },
        CombatOutcomePanel: { template: '<section class="cop-root" />' },
        EmotionalTypeBadge: { template: '<span class="type-badge" />' },
        SkillBar: { template: '<section class="skill-bar" />' },
      },
    },
  });
}

describe('CombatScene', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    resetMockStore();
  });

  it('renders without crashing', () => {
    const wrapper = mountScene();
    expect(wrapper.exists()).toBe(true);
  });

  it('shows loading placeholder when combat is null and loading', () => {
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: null,
      isLoading: true,
    });
    expect(wrapper.text()).toContain("Le seuil s'ouvre");
  });

  it('shows unavailable message when combat is null and not loading', () => {
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: null,
      isLoading: false,
    });
    expect(wrapper.text()).toContain('Confrontation indisponible');
  });

  it('shows initiative list when combat is active', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const enemy = makeCombatant('enemy-1', 'Beast', 'Enemy');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [enemy],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [enemy],
      allCombatants: [ally, enemy],
    });
    expect(wrapper.find('.combat-scene__initiative').exists()).toBe(true);
  });

  it('shows ally side panel when combat is active', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
    });
    expect(wrapper.find('.combat-scene__side--voix').exists()).toBe(true);
  });

  it('shows enemy side panel when combat is active', () => {
    const enemy = makeCombatant('enemy-1', 'Beast', 'Enemy');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [],
        enemies: [enemy],
        usableBattleItems: [],
      },
      allies: [],
      enemies: [enemy],
      allCombatants: [enemy],
    });
    expect(wrapper.find('.combat-scene__side--manifestations').exists()).toBe(true);
  });

  it('shows compose section when combat is active', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
    });
    expect(wrapper.find('.combat-scene__compose').exists()).toBe(true);
  });

  it('shows damage drawer toggle', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
    });
    expect(wrapper.find('.damage-toggle').exists()).toBe(true);
  });

  it('shows resolving indicator when resolving action', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
      isResolvingAction: true,
    });
    expect(wrapper.find('.combat-scene__resolving').exists()).toBe(true);
  });

  it('shows error message when error exists', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
      error: 'Something went wrong',
    });
    expect(wrapper.find('.combat-scene__error').text()).toContain('Something went wrong');
  });

  it('shows cancel button when skill is selected', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
      selectedSkillKey: 'skill.strike',
    });
    expect(wrapper.find('.compose__cancel').exists()).toBe(true);
  });

  it('shows cancel button when item is selected', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
      selectedItemId: 'item.potion',
    });
    expect(wrapper.find('.compose__cancel').exists()).toBe(true);
  });

  it('shows cancel button when targets are selected', () => {
    const ally = makeCombatant('ally-1', 'Hero', 'Player');
    const wrapper = mountScene('run-1', 'combat-1', {
      combat: {
        id: 'combat-1',
        status: 'Active',
        turnNumber: 1,
        activeCombatantId: 'ally-1',
        allies: [ally],
        enemies: [],
        usableBattleItems: [],
      },
      allies: [ally],
      enemies: [],
      allCombatants: [ally],
      selectedTargetIds: ['enemy-1'],
    });
    expect(wrapper.find('.compose__cancel').exists()).toBe(true);
  });
});
