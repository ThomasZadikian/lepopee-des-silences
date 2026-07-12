import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import { combatApi } from '../api/combatApi';
import { useCombatStore } from './useCombatStore';
import type { CombatantRuntimeDto, CombatLogEntryDto, CombatRuntimeDto } from '../types/combatContracts';

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
      { key: 'skill.a', displayName: 'Frappe', skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 0, chargeCost: 0, basePower: 10, tags: [], category: 'Physical' },
      { key: 'skill.magic', displayName: 'Flamme froide', skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage', manaCost: 8, chargeCost: 0, basePower: 22, tags: [], category: 'Magic' },
      { key: 'skill.silence', displayName: 'Silence', skillType: 'Disrupt', targetingType: 'SingleEnemy', effectType: 'Disrupt', manaCost: 4, chargeCost: 0, basePower: 0, tags: [], category: 'Magic', emotionalType: 'Silence' },
      { key: 'skill.aoe', displayName: 'Déluge du Styx', skillType: 'Damage', targetingType: 'AllEnemies', effectType: 'Damage', manaCost: 10, chargeCost: 0, basePower: 6, tags: [], category: 'Magic' },
      { key: 'skill.rally', displayName: 'Rempart', skillType: 'Guard', targetingType: 'AllAllies', effectType: 'Guard', manaCost: 6, chargeCost: 0, basePower: 7, tags: [], category: 'Physical' },
    ],
    ...overrides,
  };
}

function logEntry(overrides: Partial<CombatLogEntryDto>): CombatLogEntryDto {
  return {
    occurredAtUtc: new Date().toISOString(),
    type: 'DamageApplied',
    message: '',
    actorId: null,
    skillKey: null,
    targetIds: [],
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

  describe('selectSkill', () => {
    it('auto-populates selectedTargetIds with every enemy for an AllEnemies skill, making it immediately submittable', () => {
      const store = useCombatStore();
      store.initCombat(baseCombat({
        enemies: [
          baseCombatant({ id: 'enemy-1', side: 'Enemy', displayName: 'Ombre' }),
          baseCombatant({ id: 'enemy-2', side: 'Enemy', displayName: 'Spectre' }),
        ],
      }));

      store.selectSkill('skill.aoe');

      expect(store.selectedTargetIds).toEqual(expect.arrayContaining(['enemy-1', 'enemy-2']));
      expect(store.selectedTargetIds).toHaveLength(2);
      expect(store.canSubmit).toBe(true);
    });

    it('auto-populates selectedTargetIds with every ally for an AllAllies skill', () => {
      const store = useCombatStore();
      store.initCombat(baseCombat({
        allies: [
          baseCombatant({ id: 'ally-1' }),
          baseCombatant({ id: 'ally-2', displayName: 'Compagnon' }),
        ],
      }));

      store.selectSkill('skill.rally');

      expect(store.selectedTargetIds).toEqual(expect.arrayContaining(['ally-1', 'ally-2']));
      expect(store.canSubmit).toBe(true);
    });

    it('leaves selectedTargetIds empty for a SingleEnemy skill, requiring an explicit target click', () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      store.selectSkill('skill.a');

      expect(store.selectedTargetIds).toEqual([]);
      expect(store.canSubmit).toBe(false);
    });
  });

  describe('playCombatLogs', () => {
    // The flash-state arrays (recentlyMissedIds, etc.) are transient: they're
    // set synchronously (before playCombatLogs' internal per-entry delay) and
    // auto-clear a few hundred ms later. Assert on them right after invoking
    // (the synchronous prefix has already run by then) rather than after
    // awaiting full playback, or the flash will already have cleared —
    // then await the promise so its real timers drain before the test ends.
    it('pushes a miss feedback event and flashes recentlyMissedIds, without touching vitality', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      const playback = store.playCombatLogs([
        logEntry({
          type: 'AttackMissed', actorId: 'ally-1', skillKey: 'skill.a', targetIds: ['enemy-1'],
          message: "Le Porteur's Frappe misses Ombre.",
        }),
      ]);

      expect(store.feedbackEvents).toContainEqual(expect.objectContaining({ combatantId: 'enemy-1', type: 'miss' }));
      expect(store.recentlyMissedIds).toContain('enemy-1');
      expect(store.findCombatantById('enemy-1')?.currentVitality).toBe(100);

      await playback;
    });

    it('marks a DamageApplied hit as critical when it follows a CriticalHit entry for the same target', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      const playback = store.playCombatLogs([
        logEntry({ type: 'CriticalHit', actorId: 'ally-1', skillKey: 'skill.a', targetIds: ['enemy-1'], message: 'Critical hit on Ombre!' }),
        logEntry({ type: 'DamageApplied', actorId: 'ally-1', skillKey: 'skill.a', targetIds: ['enemy-1'], message: 'Ombre takes 15 damage.' }),
      ]);

      expect(store.recentlyCriticalHitIds).toContain('enemy-1');
      expect(store.feedbackEvents).toContainEqual(
        expect.objectContaining({ combatantId: 'enemy-1', type: 'damage', amount: 15, isCritical: true }),
      );

      await playback;
    });

    it('does not mark a DamageApplied hit as critical without a preceding CriticalHit entry', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      const playback = store.playCombatLogs([
        logEntry({ type: 'DamageApplied', actorId: 'ally-1', skillKey: 'skill.a', targetIds: ['enemy-1'], message: 'Ombre takes 10 damage.' }),
      ]);

      expect(store.recentlyCriticalHitIds).not.toContain('enemy-1');
      expect(store.feedbackEvents).toContainEqual(
        expect.objectContaining({ combatantId: 'enemy-1', type: 'damage', isCritical: false }),
      );

      await playback;
    });

    it('tags a DamageApplied hit with the Magic category when the actor used a Magic skill', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      const playback = store.playCombatLogs([
        logEntry({ type: 'DamageApplied', actorId: 'ally-1', skillKey: 'skill.magic', targetIds: ['enemy-1'], message: 'Ombre takes 22 damage.' }),
      ]);

      expect(store.recentlyMagicHitIds).toContain('enemy-1');
      expect(store.feedbackEvents).toContainEqual(
        expect.objectContaining({ combatantId: 'enemy-1', type: 'damage', category: 'Magic' }),
      );

      await playback;
    });

    it('pushes a status feedback event carrying the casting skill\'s emotionalType, without touching vitality or guard', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      const playback = store.playCombatLogs([
        logEntry({
          type: 'StatusApplied', actorId: 'ally-1', skillKey: 'skill.silence', targetIds: ['enemy-1'],
          message: 'Ombre is silenced.',
        }),
      ]);

      expect(store.feedbackEvents).toContainEqual(
        expect.objectContaining({ combatantId: 'enemy-1', type: 'status', emotionalType: 'Silence' }),
      );
      expect(store.findCombatantById('enemy-1')?.currentVitality).toBe(100);
      expect(store.findCombatantById('enemy-1')?.guard).toBe(0);

      await playback;
    });

    it('does not tag a Physical skill hit with the Magic category', async () => {
      const store = useCombatStore();
      store.initCombat(baseCombat());

      const playback = store.playCombatLogs([
        logEntry({ type: 'DamageApplied', actorId: 'ally-1', skillKey: 'skill.a', targetIds: ['enemy-1'], message: 'Ombre takes 10 damage.' }),
      ]);

      expect(store.recentlyMagicHitIds).not.toContain('enemy-1');

      await playback;
    });
  });
});
