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
  recentlyMagicHitIds: [] as string[],
  recentlyCriticalHitIds: [] as string[],
  recentlyMissedIds: [] as string[],
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

const mockRunStore: { combatRuntime: CombatRuntimeDto | null; currentRoom: { depth: number } | null } = {
  combatRuntime: null,
  currentRoom: null,
};

vi.mock('../../runs/stores/runStore', () => ({
  useRunStore: vi.fn(() => mockRunStore),
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
  mockRunStore.combatRuntime = null;
  mockRunStore.currentRoom = null;
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
  mockStore.recentlyMagicHitIds = [];
  mockStore.recentlyCriticalHitIds = [];
  mockStore.recentlyMissedIds = [];
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

function mountScene(
  runId = 'run-1',
  combatId = 'combat-1',
  combatStoreOverrides: Record<string, any> = {},
  runStoreOverrides: Partial<typeof mockRunStore> = {},
) {
  resetMockStore();
  configureMockStore(combatStoreOverrides);
  Object.assign(mockRunStore, runStoreOverrides);

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

  it('shows ally section when combat is active', () => {
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
    expect(wrapper.find('.combat-scene__side-title--allies').exists()).toBe(true);
  });

  it('shows enemy section when combat is active', () => {
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
    expect(wrapper.find('.combat-scene__side-title--foe').exists()).toBe(true);
  });

  it('shows the depth-scaling badge when the current room is past depth 1', () => {
    const enemy = makeCombatant('enemy-1', 'Beast', 'Enemy');
    const wrapper = mountScene(
      'run-1',
      'combat-1',
      {
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
      },
      { currentRoom: { depth: 4 } },
    );
    const badge = wrapper.find('.combat-scene__difficulty-badge');
    expect(badge.exists()).toBe(true);
    expect(badge.text()).toContain('×2.5');
  });

  it('hides the depth-scaling badge at room depth 1', () => {
    const enemy = makeCombatant('enemy-1', 'Beast', 'Enemy');
    const wrapper = mountScene(
      'run-1',
      'combat-1',
      {
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
      },
      { currentRoom: { depth: 1 } },
    );
    expect(wrapper.find('.combat-scene__difficulty-badge').exists()).toBe(false);
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

  it('opens the damage meter modal from its toggle button', async () => {
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
    expect(document.body.querySelector('.damage-report')).toBeNull();
    await wrapper.find('.compose__meter-toggle').trigger('click');
    expect(document.body.querySelector('.damage-report')).not.toBeNull();
    wrapper.unmount();
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
