import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { combatApi } from '../api/combatApi';
import type {
  CombatantRuntimeDto,
  CombatantSkillRuntimeDto,
  CombatLogEntryDto,
  CombatRuntimeDto,
  TargetingType,
} from '../types/combatContracts';

export type CombatTerminalEvent =
  | { kind: 'victory' }
  | { kind: 'defeat' }
  | null;

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
  const impactedCombatantId = ref<string | null>(null);

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

  function initCombat(combatData: CombatRuntimeDto) {
    combat.value = combatData;
    hasRuntimeCombat.value = true;
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedTargetIds.value = [];
    error.value = null;
    terminalEvent.value = null;
    thinkingCombatantId.value = null;
    impactedCombatantId.value = null;
  }

  function setCombatFromResponse(combatData: CombatRuntimeDto, newLogs: CombatLogEntryDto[]) {
    combat.value = combatData;
    hasRuntimeCombat.value = true;
    logEntries.value = [...logEntries.value, ...newLogs];
    selectedSkillKey.value = null;
    selectedTargetIds.value = [];
    error.value = null;
  }

  function finishCombatResponse(combatData: CombatRuntimeDto) {
    combat.value = combatData;
    hasRuntimeCombat.value = true;
    selectedSkillKey.value = null;
    selectedTargetIds.value = [];
    error.value = null;
  }

  function delay(milliseconds: number): Promise<void> {
    return new Promise((resolve) => window.setTimeout(resolve, milliseconds));
  }

  async function playCombatLogs(entries: CombatLogEntryDto[]) {
    for (const entry of entries) {
      logEntries.value = [...logEntries.value, entry];

      if (entry.type === 'EnemyTurnResolved' && entry.actorId) {
        thinkingCombatantId.value = entry.actorId;
        await delay(2000);
        thinkingCombatantId.value = null;
        await delay(250);
      } else if (entry.targetIds.length > 0 && shouldHighlightTarget(entry)) {
        applyLogEffect(entry);
        impactedCombatantId.value = entry.targetIds[0];
        await delay(550);
        impactedCombatantId.value = null;
        await delay(150);
      }
    }
  }

  function shouldHighlightTarget(entry: CombatLogEntryDto): boolean {
    return entry.type === 'DamageApplied'
      || entry.type === 'GuardGained'
      || entry.type === 'TargetDefeated';
  }

  function applyLogEffect(entry: CombatLogEntryDto) {
    const targetId = entry.targetIds[0];
    if (!combat.value || !targetId) return;

    combat.value = {
      ...combat.value,
      allies: combat.value.allies.map((c) => applyEntryToCombatant(c, targetId, entry)),
      enemies: combat.value.enemies.map((c) => applyEntryToCombatant(c, targetId, entry)),
    };
  }

  function applyEntryToCombatant(
    combatant: CombatantRuntimeDto,
    targetId: string,
    entry: CombatLogEntryDto,
  ): CombatantRuntimeDto {
    if (combatant.id !== targetId) return combatant;

    if (entry.type === 'TargetDefeated') {
      return { ...combatant, currentVitality: 0, status: 'Defeated' };
    }

    const amount = extractFirstNumber(entry.message);
    if (amount === null) return combatant;

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

  function clearCombat() {
    combat.value = null;
    hasRuntimeCombat.value = false;
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedTargetIds.value = [];
    error.value = null;
    terminalEvent.value = null;
    thinkingCombatantId.value = null;
    impactedCombatantId.value = null;
  }

  function selectSkill(skillKey: string) {
    if (selectedSkillKey.value === skillKey) {
      selectedSkillKey.value = null;
      selectedTargetIds.value = [];
      return;
    }
    selectedSkillKey.value = skillKey;
    selectedTargetIds.value = [];
  }

  function selectTarget(targetId: string) {
    const skill = selectedSkill.value;
    if (!skill) return;

    if (skill.targetingType === 'SingleEnemy' || skill.targetingType === 'SingleAlly' || skill.targetingType === 'Self') {
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
      error.value = caught instanceof Error ? caught.message : 'Impossible de charger le combat.';
      combat.value = null;
    } finally {
      isLoading.value = false;
    }
  }

  async function submitAction(runId: string) {
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

      if (response.combatCompleted) {
        terminalEvent.value = { kind: 'victory' };
      } else if (response.combatFailed) {
        terminalEvent.value = { kind: 'defeat' };
      }
    } catch (caught) {
      error.value = caught instanceof Error ? caught.message : 'L\'action a échoué.';
    } finally {
      thinkingCombatantId.value = null;
      impactedCombatantId.value = null;
      isLoading.value = false;
    }
  }

  return {
    combat,
    logEntries,
    selectedSkillKey,
    selectedTargetIds,
    isLoading,
    error,
    terminalEvent,
    thinkingCombatantId,
    impactedCombatantId,
    allies,
    enemies,
    allCombatants,
    activeCombatantId,
    currentActor,
    isPlayerTurn,
    selectedSkill,
    validTargets,
    canSubmit,
    isVictory,
    isDefeat,
    initCombat,
    setCombatFromResponse,
    clearCombat,
    selectSkill,
    selectTarget,
    clearSelection,
    isSelectedTarget,
    isCurrentActor,
    loadCurrentCombat,
    submitAction,
    findCombatantById,
    hasRuntimeCombat,
  };
});
