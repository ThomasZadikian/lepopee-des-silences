import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { combatApi } from '../api/combatApi';
import type {
    CombatActionResultDto,
    CombatInstanceDto
} from '../types/combatTypes';
import { unwrapCombatResponse } from '../types/combatTypes';

export const useCombatStore = defineStore('combat', () => {
  const activeCombat = ref<CombatInstanceDto | null>(null);
  const lastActionResult = ref<CombatActionResultDto | null>(null);
  const selectedTargetId = ref<string | null>(null);

  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const currentActor = computed(() => {
    if (!activeCombat.value?.currentActorId) {
      return null;
    }

    return activeCombat.value.combatants.find(
      (combatant) => combatant.id === activeCombat.value?.currentActorId,
    ) ?? null;
  });

  const playerCombatants = computed(() =>
    activeCombat.value?.combatants.filter((combatant) => combatant.side === 'Player') ?? [],
  );

  const enemyCombatants = computed(() =>
    activeCombat.value?.combatants.filter((combatant) => combatant.side === 'Enemy') ?? [],
  );

  const aliveEnemies = computed(() =>
    enemyCombatants.value.filter((combatant) => !combatant.isDefeated),
  );

  const canSubmitBasicAttack = computed(() => {
    if (!activeCombat.value || activeCombat.value.state !== 'InProgress') {
      return false;
    }

    if (!currentActor.value || currentActor.value.side !== 'Player') {
      return false;
    }

    return Boolean(selectedTargetId.value);
  });

  async function execute(action: () => Promise<void>) {
    isLoading.value = true;
    error.value = null;

    try {
      await action();
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Une erreur inconnue est survenue pendant le combat.';

      throw caught;
    } finally {
      isLoading.value = false;
    }
  }

  async function loadCombat(runId: string, combatId: string) {
    await execute(async () => {
      const response = await combatApi.getCombat(runId, combatId);
      activeCombat.value = unwrapCombatResponse(response);

      if (!selectedTargetId.value) {
        selectedTargetId.value = aliveEnemies.value[0]?.id ?? null;
      }
    });
  }

  function setCombat(combat: CombatInstanceDto | null) {
    activeCombat.value = combat;
    lastActionResult.value = null;
    selectedTargetId.value = combat?.combatants.find(
      (combatant) => combatant.side === 'Enemy' && !combatant.isDefeated,
    )?.id ?? null;
  }

  function selectTarget(targetId: string) {
    const target = aliveEnemies.value.find((enemy) => enemy.id === targetId);

    if (!target) {
      return;
    }

    selectedTargetId.value = targetId;
  }

  async function submitBasicAttack(runId: string, combatId: string) {
    if (!activeCombat.value || !currentActor.value || !selectedTargetId.value) {
      return;
    }

    await execute(async () => {
      const response = await combatApi.submitAction(runId, combatId, {
        actorId: currentActor.value!.id,
        targetId: selectedTargetId.value!,
        actionType: 'BasicAttack',
      });

      if (response.combat) {
        activeCombat.value = response.combat;
      } else {
        const refreshed = await combatApi.getCombat(runId, combatId);
        activeCombat.value = unwrapCombatResponse(refreshed);
      }

      lastActionResult.value = response.result ?? null;

      const stillValidTarget = aliveEnemies.value.some(
        (enemy) => enemy.id === selectedTargetId.value,
      );

      if (!stillValidTarget) {
        selectedTargetId.value = aliveEnemies.value[0]?.id ?? null;
      }
    });
  }

  return {
    activeCombat,
    lastActionResult,
    selectedTargetId,
    currentActor,
    playerCombatants,
    enemyCombatants,
    aliveEnemies,
    canSubmitBasicAttack,
    isLoading,
    error,
    loadCombat,
    setCombat,
    selectTarget,
    submitBasicAttack,
  };
});