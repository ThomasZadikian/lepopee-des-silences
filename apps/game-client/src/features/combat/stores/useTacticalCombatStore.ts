import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { combatApi } from '../api/combatApi';

import type {
  CombatLogEntryDto,
  CombatantSkillRuntimeDto,
  TacticalCombatRuntimeDto,
  TacticalCombatantRuntimeDto,
} from '../types/combatContracts';

/**
 * L'état d'un combat tactique côté client.
 *
 * Séparé du store ATB à dessein : celui-ci porte ~900 lignes de machinerie
 * d'animation accrochées au tempo (jauges, tick, file d'attente d'effets), qui
 * n'ont aucun sens dans un tour par tour. Les fondre obligerait la moitié de
 * chaque store à rester inerte selon le mode.
 */
export const useTacticalCombatStore = defineStore('tacticalCombat', () => {
  const combat = ref<TacticalCombatRuntimeDto | null>(null);
  const logEntries = ref<CombatLogEntryDto[]>([]);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  /** La compétence armée, en attente d'une case. `null` = mode déplacement. */
  const selectedSkillKey = ref<string | null>(null);

  const allCombatants = computed<TacticalCombatantRuntimeDto[]>(() =>
    combat.value ? [...combat.value.allies, ...combat.value.enemies] : [],
  );

  const activeCombatant = computed<TacticalCombatantRuntimeDto | null>(() => {
    const activeId = combat.value?.activeCombatantId;
    if (!activeId) return null;

    return allCombatants.value.find((c) => c.combatant.id === activeId) ?? null;
  });

  /** Le joueur n'a la main que si le combattant actif est de son camp. */
  const isPlayerTurn = computed(() => activeCombatant.value?.combatant.side === 'Player');

  const activeSkills = computed<CombatantSkillRuntimeDto[]>(
    () => activeCombatant.value?.combatant.skills ?? [],
  );

  const selectedSkill = computed<CombatantSkillRuntimeDto | null>(() => {
    if (!selectedSkillKey.value) return null;

    return activeSkills.value.find((s) => s.key === selectedSkillKey.value) ?? null;
  });

  /**
   * L'ordre d'action du round, résolu en combattants. Ce que le joueur doit
   * pouvoir lire d'un coup d'œil : c'est la contrepartie annoncée de l'abandon
   * du tempo ATB.
   */
  const initiativeQueue = computed<TacticalCombatantRuntimeDto[]>(() => {
    if (!combat.value) return [];

    return combat.value.initiativeOrder
      .map((id) => allCombatants.value.find((c) => c.combatant.id === id))
      .filter((c): c is TacticalCombatantRuntimeDto => c !== undefined);
  });

  const occupantAt = (x: number, y: number): TacticalCombatantRuntimeDto | null =>
    allCombatants.value.find(
      (c) => c.x === x && c.y === y && c.combatant.status !== 'Defeated',
    ) ?? null;

  function setCombat(next: TacticalCombatRuntimeDto) {
    combat.value = next;
    // Une compétence armée n'a plus de sens dès que le tour change de main.
    selectedSkillKey.value = null;
  }

  function clearCombat() {
    combat.value = null;
    logEntries.value = [];
    selectedSkillKey.value = null;
    error.value = null;
  }

  function selectSkill(skillKey: string | null) {
    selectedSkillKey.value = selectedSkillKey.value === skillKey ? null : skillKey;
  }

  /**
   * Enveloppe commune aux trois actions. Aucune pré-validation côté client :
   * portée, budget et occupation sont tranchés par le domaine, dont le message
   * d'erreur remonte tel quel — même patron optimiste que `runStore`.
   */
  async function execute(action: () => Promise<{
    combat: TacticalCombatRuntimeDto;
    logEntries: CombatLogEntryDto[];
  }>) {
    isLoading.value = true;
    error.value = null;

    try {
      const response = await action();
      setCombat(response.combat);
      logEntries.value = [...logEntries.value, ...response.logEntries];
    } catch (caught) {
      error.value = caught instanceof Error ? caught.message : String(caught);
    } finally {
      isLoading.value = false;
    }
  }

  const moveTo = (runId: string, x: number, y: number) =>
    execute(() => combatApi.moveTacticalCombatant(runId, x, y));

  const useSkillAt = (runId: string, skillKey: string, x: number, y: number) =>
    execute(() => combatApi.useTacticalSkill(runId, skillKey, x, y));

  const endTurn = (runId: string) => execute(() => combatApi.endTacticalTurn(runId));

  return {
    combat,
    logEntries,
    isLoading,
    error,
    selectedSkillKey,
    selectedSkill,
    activeCombatant,
    activeSkills,
    allCombatants,
    initiativeQueue,
    isPlayerTurn,
    occupantAt,
    setCombat,
    clearCombat,
    selectSkill,
    moveTo,
    useSkillAt,
    endTurn,
  };
});
