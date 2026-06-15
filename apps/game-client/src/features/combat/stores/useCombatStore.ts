import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { combatApi } from '../api/combatApi';
import type {
  CombatantRuntimeDto,
  CombatantSkillRuntimeDto,
  CombatLogEntryDto,
  CombatRuntimeDto,
  CombatUsableItemDto,
  TargetingType,
} from '../types/combatContracts';

export type CombatTerminalEvent =
  | { kind: 'victory' }
  | { kind: 'defeat' }
  | null;

export type CombatFeedbackEvent = {
  id: string;
  combatantId: string;
  type: 'damage' | 'heal' | 'guard';
  amount: number;
};

export const useCombatStore = defineStore('combatRuntime', () => {
  const combat = ref<CombatRuntimeDto | null>(null);
  const logEntries = ref<CombatLogEntryDto[]>([]);
  const selectedSkillKey = ref<string | null>(null);
  const selectedTargetIds = ref<string[]>([]);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const terminalEvent = ref<CombatTerminalEvent>(null);
  const hasRuntimeCombat = ref(false);
  const thinkingCombatantId = ref<string | null>(null);
  const recentlyDamagedIds = ref<string[]>([]);
  const recentlyGuardedIds = ref<string[]>([]);
  const recentlyDefeatedIds = ref<string[]>([]);
  const recentlyActingId = ref<string | null>(null);
  const feedbackEvents = ref<CombatFeedbackEvent[]>([]);
  const animationTimers: ReturnType<typeof globalThis.setTimeout>[] = [];

  // ── Skill selection ─────────────────────────────────────────────────────

  const allies = computed<CombatantRuntimeDto[]>(() => combat.value?.allies ?? []);
  const enemies = computed<CombatantRuntimeDto[]>(() => combat.value?.enemies ?? []);
  const allCombatants = computed<CombatantRuntimeDto[]>(() => [...allies.value, ...enemies.value]);

  const activeCombatantId = computed<string | null>(() => combat.value?.activeCombatantId ?? null);

  const currentActor = computed<CombatantRuntimeDto | null>(() => {
    if (!activeCombatantId.value) return null;
    return allCombatants.value.find((c) => c.id === activeCombatantId.value) ?? null;
  });

  const isPlayerTurn = computed<boolean>(() => {
    return currentActor.value?.side === 'Player';
  });

  const selectedSkill = computed<CombatantSkillRuntimeDto | null>(() => {
    if (!selectedSkillKey.value || !currentActor.value) return null;
    return currentActor.value.skills.find((s) => s.key === selectedSkillKey.value) ?? null;
  });

  const validTargets = computed<CombatantRuntimeDto[]>(() => {
    const skill = selectedSkill.value;
    if (!skill) return [];
    return getValidTargets(skill.targetingType, currentActor.value, allCombatants.value);
  });

  const canSubmit = computed<boolean>(() => {
    if (!selectedSkill.value) return false;
    if (selectedTargetIds.value.length === 0) return false;
    if (!isPlayerTurn.value) return false;
    return true;
  });

  const isVictory = computed<boolean>(() => combat.value?.status === 'Completed');
  const isDefeat = computed<boolean>(() => combat.value?.status === 'Failed');
  const isResolvingAction = computed<boolean>(() => isLoading.value);

  // ── Item selection ───────────────────────────────────────────────────────

  const selectedItemId = ref<string | null>(null);

  const selectedItem = computed<CombatUsableItemDto | null>(() => {
    if (!selectedItemId.value || !combat.value) return null;
    return (
      combat.value.usableBattleItems.find((i) => i.itemId === selectedItemId.value) ?? null
    );
  });

  const itemValidTargets = computed<CombatantRuntimeDto[]>(() => {
    const item = selectedItem.value;
    if (!item) return [];
    if (item.targetingType === 'Self') {
      return allies.value.filter((a) => a.status !== 'Defeated');
    }
    return allies.value.filter((a) => a.status !== 'Defeated');
  });

  const canSubmitItem = computed<boolean>(() => {
    if (!selectedItem.value) return false;
    if (!isPlayerTurn.value) return false;
    // Self : aucune cible explicite requise
    if (selectedItem.value.targetingType === 'Self') return true;
    // SingleAlly : une cible requise
    return selectedTargetIds.value.length > 0;
  });

  function selectItem(itemId: string) {
    if (selectedItemId.value === itemId) {
      selectedItemId.value = null;
      selectedTargetIds.value = [];
      return;
    }
    selectedItemId.value = itemId;
    selectedSkillKey.value = null;
    selectedTargetIds.value = [];
  }

  function clearItemSelection() {
    selectedItemId.value = null;
    selectedTargetIds.value = [];
  }

  async function submitItemAction(runId: string, onCombatApplied?: (combat: CombatRuntimeDto) => void) {
    const item = selectedItem.value;
    const combatId = combat.value?.id;
    if (!item || !combatId) return;

    const targetIds =
      item.targetingType === 'Self' ? [] : [...selectedTargetIds.value];

    isLoading.value = true;
    error.value = null;
    try {
      const response = await combatApi.useItemAction(runId, combatId, {
        itemId: item.itemId,
        targetIds,
      });

      selectedItemId.value = null;
      selectedTargetIds.value = [];
      await playCombatLogs(response.logEntries);
      finishCombatResponse(response.combat);
      onCombatApplied?.(response.combat);

      if (response.combatCompleted) terminalEvent.value = { kind: 'victory' };
      else if (response.combatFailed) terminalEvent.value = { kind: 'defeat' };
    } catch (caught) {
      error.value =
        caught instanceof Error ? caught.message : "L'utilisation a échoué.";
    } finally {
      isLoading.value = false;
    }
  }

  // ── Helpers ──────────────────────────────────────────────────────────────

  function getValidTargets(
    targetingType: TargetingType,
    actor: CombatantRuntimeDto | null,
    combatants: CombatantRuntimeDto[],
  ): CombatantRuntimeDto[] {
    if (!actor) return [];

    switch (targetingType) {
      case 'Self':
        return [actor];
      case 'SingleEnemy':
        return combatants.filter((c) => c.side !== actor.side && c.status === 'Active');
      case 'SingleAlly':
        return combatants.filter((c) => c.side === actor.side && c.status === 'Active');
      case 'AllEnemies':
        return combatants.filter((c) => c.side !== actor.side && c.status === 'Active');
      case 'AllAllies':
        return combatants.filter((c) => c.side === actor.side && c.status === 'Active');
    }
  }

  function findCombatantById(id: string): CombatantRuntimeDto | null {
    return allCombatants.value.find((c) => c.id === id) ?? null;
  }

  // ── State management ─────────────────────────────────────────────────────

  function initCombat(combatData: CombatRuntimeDto) {
    resetAnimationState();
    combat.value = combatData;
    hasRuntimeCombat.value = true;
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
    terminalEvent.value = null;
  }

  function setCombatFromResponse(combatData: CombatRuntimeDto, newLogs: CombatLogEntryDto[]) {
    combat.value = combatData;
    hasRuntimeCombat.value = true;
    logEntries.value = [...logEntries.value, ...newLogs];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
  }

  function finishCombatResponse(combatData: CombatRuntimeDto) {
    combat.value = combatData;
    hasRuntimeCombat.value = true;
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
  }

  function clearCombat() {
    resetAnimationState();
    combat.value = null;
    hasRuntimeCombat.value = false;
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
    terminalEvent.value = null;
  }

  // ── Animations ───────────────────────────────────────────────────────────

  function delay(milliseconds: number): Promise<void> {
    return new Promise((resolve) => {
      const timerId = globalThis.setTimeout(() => {
        const index = animationTimers.indexOf(timerId);
        if (index >= 0) animationTimers.splice(index, 1);
        resolve();
      }, milliseconds);
      animationTimers.push(timerId);
    });
  }

  async function playCombatLogs(entries: CombatLogEntryDto[]) {
    for (const entry of entries) {
      logEntries.value = [...logEntries.value, entry];

      if (entry.actorId && (entry.type === 'SkillUsed' || entry.type === 'ItemUsed')) {
        markActing(entry.actorId);
      }

      if (entry.type === 'EnemyTurnResolved' && entry.actorId) {
        thinkingCombatantId.value = entry.actorId;
        markActing(entry.actorId, 900);
        await delay(2000);
        thinkingCombatantId.value = null;
        await delay(250);
      } else if (entry.targetIds.length > 0 && shouldHighlightTarget(entry)) {
        applyLogDelta(entry);
        applyLogEffect(entry);
        markFeedbackFromLog(entry);
        await delay(550);
        await delay(150);
      }
    }
  }

  function parseLogAmount(message: string): number {
    const matches = message.match(/\d+/g);
    if (!matches?.length) return 0;
    return Number(matches[matches.length - 1]) || 0;
  }

  function applyLogDelta(entry: CombatLogEntryDto) {
    if (!combat.value) return;
    const amount = parseLogAmount(entry.message);
    if (amount <= 0) return;

    for (const targetId of entry.targetIds) {
      const target = findCombatantById(targetId);
      if (!target) continue;

      if (entry.type === 'DamageApplied') {
        const blocked = Math.min(target.guard, amount);
        const remaining = Math.max(0, amount - blocked);
        target.guard = Math.max(0, target.guard - blocked);
        target.currentVitality = Math.max(0, target.currentVitality - remaining);
        if (blocked > 0) pushFeedbackEvent(target.id, 'guard', blocked);
        if (remaining > 0) pushFeedbackEvent(target.id, 'damage', remaining);
      } else if (entry.type === 'HealApplied') {
        target.currentVitality = Math.min(target.maxVitality, target.currentVitality + amount);
        pushFeedbackEvent(target.id, 'heal', amount);
      } else if (entry.type === 'GuardGained') {
        target.guard += amount;
        pushFeedbackEvent(target.id, 'guard', amount);
      }
    }
  }

  function pushFeedbackEvent(combatantId: string, type: CombatFeedbackEvent['type'], amount: number) {
    const event = {
      id: `${combatantId}-${type}-${Date.now()}-${feedbackEvents.value.length}`,
      combatantId,
      type,
      amount,
    };
    feedbackEvents.value = [...feedbackEvents.value, event];
    schedule(() => {
      feedbackEvents.value = feedbackEvents.value.filter((item) => item.id !== event.id);
    }, 1200);
  }

  function markFeedbackFromLog(entry: CombatLogEntryDto) {
    if (entry.type === 'DamageApplied') {
      markDamaged(entry.targetIds);
    } else if (entry.type === 'GuardGained') {
      markGuarded(entry.targetIds);
    } else if (entry.type === 'HealApplied') {
    markGuarded(entry.targetIds, 900);
    } else if (entry.type === 'TargetDefeated') {
      markDefeated(entry.targetIds);
    }
  }

  function markDamaged(targetIds: string[], duration = 700) {
    flashIds(recentlyDamagedIds, targetIds, duration);
  }

  function markGuarded(targetIds: string[], duration = 800) {
    flashIds(recentlyGuardedIds, targetIds, duration);
  }

  function markDefeated(targetIds: string[], duration = 900) {
    flashIds(recentlyDefeatedIds, targetIds, duration);
  }

  function markActing(actorId: string, duration = 650) {
    recentlyActingId.value = actorId;
    schedule(() => {
      if (recentlyActingId.value === actorId) {
        recentlyActingId.value = null;
      }
    }, duration);
  }

  function flashIds(state: { value: string[] }, ids: string[], duration: number) {
    const nextIds = ids.filter((id) => !state.value.includes(id));
    state.value = [...state.value, ...nextIds];
    schedule(() => {
      state.value = state.value.filter((id) => !ids.includes(id));
    }, duration);
  }

  function schedule(callback: () => void, milliseconds: number) {
    const timerId = globalThis.setTimeout(() => {
      const index = animationTimers.indexOf(timerId);
      if (index >= 0) animationTimers.splice(index, 1);
      callback();
    }, milliseconds);
    animationTimers.push(timerId);
  }

  function resetAnimationState() {
    for (const timerId of animationTimers.splice(0)) {
      globalThis.clearTimeout(timerId);
    }
    thinkingCombatantId.value = null;
    recentlyDamagedIds.value = [];
    recentlyGuardedIds.value = [];
    recentlyDefeatedIds.value = [];
    recentlyActingId.value = null;
    feedbackEvents.value = [];
  }

  function shouldHighlightTarget(entry: CombatLogEntryDto): boolean {
    return (
      entry.type === 'DamageApplied' ||
      entry.type === 'GuardGained' ||
      entry.type === 'HealApplied' ||
      entry.type === 'TargetDefeated'
    );
  }

  function applyLogEffect(entry: CombatLogEntryDto) {
    if (!combat.value || entry.targetIds.length === 0) return;

    combat.value = {
      ...combat.value,
      allies: combat.value.allies.map((c) => applyEntryToCombatant(c, entry)),
      enemies: combat.value.enemies.map((c) => applyEntryToCombatant(c, entry)),
    };
  }

function applyEntryToCombatant(
  combatant: CombatantRuntimeDto,
  entry: CombatLogEntryDto,
): CombatantRuntimeDto {
  if (!entry.targetIds.includes(combatant.id)) return combatant;

  if (entry.type === 'TargetDefeated') {
    return { ...combatant, currentVitality: 0, status: 'Defeated' };
  }

  const amount = extractFirstNumber(entry.message);
  if (amount === null) return combatant;

  if (entry.type === 'HealApplied') {
    return {
      ...combatant,
      currentVitality: Math.min(combatant.maxVitality, combatant.currentVitality + amount),
    };
  }

  if (entry.type === 'GuardGained') {
    return { ...combatant, guard: combatant.guard + amount };
  }

  if (entry.type === 'DamageApplied' && entry.message.includes('guard absorbs')) {
    return { ...combatant, guard: Math.max(0, combatant.guard - amount) };
  }

  if (entry.type === 'DamageApplied') {
    return { ...combatant, currentVitality: Math.max(0, combatant.currentVitality - amount) };
  }

  return combatant;
}

  function extractFirstNumber(message: string): number | null {
    const match = message.match(/\d+/);
    return match ? Number(match[0]) : null;
  }

  // ── Skill actions ────────────────────────────────────────────────────────

  function selectSkill(skillKey: string) {
    if (selectedSkillKey.value === skillKey) {
      selectedSkillKey.value = null;
      selectedTargetIds.value = [];
      return;
    }
    selectedSkillKey.value = skillKey;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
  }

  function selectTarget(targetId: string) {
    const skill = selectedSkill.value;
    if (!skill) return;

    if (
      skill.targetingType === 'SingleEnemy' ||
      skill.targetingType === 'SingleAlly' ||
      skill.targetingType === 'Self'
    ) {
      if (selectedTargetIds.value[0] === targetId) {
        selectedTargetIds.value = [];
      } else {
        selectedTargetIds.value = [targetId];
      }
    }
  }

  function clearSelection() {
    selectedSkillKey.value = null;
    selectedTargetIds.value = [];
  }

  function isSelectedTarget(combatantId: string): boolean {
    return selectedTargetIds.value.includes(combatantId);
  }

  function isCurrentActor(combatantId: string): boolean {
    return activeCombatantId.value === combatantId;
  }

  async function loadCurrentCombat(runId: string) {
    isLoading.value = true;
    error.value = null;
    try {
      const result = await combatApi.getCurrentCombat(runId);
      if (result === null) {
        combat.value = null;
        hasRuntimeCombat.value = false;
      } else {
        initCombat(result);
      }
    } catch (caught) {
      error.value =
        caught instanceof Error ? caught.message : 'Impossible de charger le combat.';
      combat.value = null;
    } finally {
      isLoading.value = false;
    }
  }

  async function submitAction(runId: string, onCombatApplied?: (combat: CombatRuntimeDto) => void) {
    const actor = currentActor.value;
    const skill = selectedSkill.value;
    const combatId = combat.value?.id;
    if (!actor || !skill || !combatId) return;

    isLoading.value = true;
    error.value = null;
    try {
      const response = await combatApi.useSkillAction(runId, combatId, {
        actorId: actor.id,
        skillKey: skill.key,
        targetIds: selectedTargetIds.value,
      });

      selectedSkillKey.value = null;
      selectedTargetIds.value = [];
      await playCombatLogs(response.logEntries);
      finishCombatResponse(response.combat);
      onCombatApplied?.(response.combat);

      if (response.combatCompleted) {
        terminalEvent.value = { kind: 'victory' };
      } else if (response.combatFailed) {
        terminalEvent.value = { kind: 'defeat' };
      }
    } catch (caught) {
      error.value =
        caught instanceof Error ? caught.message : "L'action a échoué.";
    } finally {
      isLoading.value = false;
    }
  }

  // ── Return ───────────────────────────────────────────────────────────────

  return {
    combat,
    logEntries,
    selectedSkillKey,
    selectedTargetIds,
    isLoading,
    error,
    terminalEvent,
    thinkingCombatantId,
    recentlyDamagedIds,
    recentlyGuardedIds,
    recentlyDefeatedIds,
    recentlyActingId,
    feedbackEvents,
    allies,
    enemies,
    allCombatants,
    activeCombatantId,
    currentActor,
    isPlayerTurn,
    selectedSkill,
    validTargets,
    canSubmit,
    isResolvingAction,
    isVictory,
    isDefeat,
    hasRuntimeCombat,
    // Items
    selectedItemId,
    selectedItem,
    itemValidTargets,
    canSubmitItem,
    selectItem,
    clearItemSelection,
    submitItemAction,
    // Combat state
    initCombat,
    setCombatFromResponse,
    finishCombatResponse,
    playCombatLogs,
    clearCombat,
    // Skills
    selectSkill,
    selectTarget,
    clearSelection,
    isSelectedTarget,
    isCurrentActor,
    loadCurrentCombat,
    submitAction,
    findCombatantById,
    // Animations
    markDamaged,
    markGuarded,
    markDefeated,
    markActing,
    resetAnimationState,
  };
});
