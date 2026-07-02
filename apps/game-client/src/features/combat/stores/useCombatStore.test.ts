import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import { combatApi } from '../api/combatApi';
import { useCombatStore } from './useCombatStore';
import type { CombatantRuntimeDto, CombatRuntimeDto } from '../types/combatContracts';

vi.mock('../api/combatApi', () => ({
  combatApi: {
    getCurrentCombat: vi.fn(),
    useSkillAction: vi.fn(),
    useItemAction: vi.fn(),
    hold: vi.fn(),
    advanceCombat: vi.fn(),
  },
}));

function baseCombatant(overrides: Partial<CombatantRuntimeDto> = {}): CombatantRuntimeDto {
  return {
    id: 'ally-1',
    sourceKey: 'player.self',
    displayName: 'Le Porteur',
    side: 'Player',
    archetype: 'Fighter',
    maxVitality: 100,
    currentVitality: 100,
    guard: 0,
    mana: 0,
    charge: 0,
    status: 'Active',
    skills: [
      { key: 'skill.a', displayName: 'Frappe', skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10, tags: [] },
    ],
    ...overrides,
  };
}

function baseCombat(overrides: Partial<CombatRuntimeDto> = {}): CombatRuntimeDto {
  return {
    id: 'combat-1',
    status: 'Active',
    turnNumber: 1,
    activeCombatantId: 'ally-1',
    allies: [baseCombatant()],
    enemies: [baseCombatant({ id: 'enemy-1', side: 'Enemy', displayName: 'Ombre' })],
    usableBattleItems: [],
    ...overrides,
  };
}

describe('useCombatStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  describe('submitAction', () => {
    it('clears selectedSkillKey/selectedTargetIds on a failed useSkillAction call', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());
      store.selectSkill('skill.a');
      store.selectTarget('enemy-1');
      vi.mocked(combatApi.useSkillAction).mockRejectedValue(new Error("It is not this combatant's turn."));
      vi.mocked(combatApi.getCurrentCombat).mockResolvedValue(baseCombat({ activeCombatantId: 'enemy-1' }));

      await store.submitAction('run-1');

      expect(store.selectedSkillKey).toBeNull();
      expect(store.selectedTargetIds).toEqual([]);
    });

    it('resyncs combat.value from getCurrentCombat after a failed useSkillAction call', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());
      store.selectSkill('skill.a');
      store.selectTarget('enemy-1');
      vi.mocked(combatApi.useSkillAction).mockRejectedValue(new Error("It is not this combatant's turn."));
      const resynced = baseCombat({ activeCombatantId: 'enemy-1' });
      vi.mocked(combatApi.getCurrentCombat).mockResolvedValue(resynced);

      await store.submitAction('run-1');

      expect(store.combat?.activeCombatantId).toBe('enemy-1');
    });

    it('does not immediately re-satisfy canSubmit after a failed submitAction (no auto-retry loop)', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());
      store.selectSkill('skill.a');
      store.selectTarget('enemy-1');
      vi.mocked(combatApi.useSkillAction).mockRejectedValue(new Error("It is not this combatant's turn."));
      vi.mocked(combatApi.getCurrentCombat).mockResolvedValue(baseCombat({ activeCombatantId: 'enemy-1' }));

      await store.submitAction('run-1');

      expect(store.canSubmit).toBe(false);
    });

    it('sets an error message on failure', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());
      store.selectSkill('skill.a');
      store.selectTarget('enemy-1');
      vi.mocked(combatApi.useSkillAction).mockRejectedValue(new Error('boom'));
      vi.mocked(combatApi.getCurrentCombat).mockResolvedValue(baseCombat());

      await store.submitAction('run-1');

      expect(store.error).toBe('boom');
    });
  });

  describe('submitItemAction', () => {
    it('clears selectedItemId/selectedTargetIds on a failed useItemAction call', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat({
        usableBattleItems: [
          { itemId: 'item-1', definitionKey: 'potion', displayName: 'Potion', effectType: 'Heal', effectAmount: 10, quantity: 1, targetingType: 'SingleAlly' },
        ],
      }));
      store.selectItem('item-1');
      store.selectedTargetIds = ['ally-1'];
      vi.mocked(combatApi.useItemAction).mockRejectedValue(new Error("It is not this combatant's turn."));
      vi.mocked(combatApi.getCurrentCombat).mockResolvedValue(baseCombat({ activeCombatantId: 'enemy-1' }));

      await store.submitItemAction('run-1');

      expect(store.selectedItemId).toBeNull();
      expect(store.selectedTargetIds).toEqual([]);
    });
  });
});
