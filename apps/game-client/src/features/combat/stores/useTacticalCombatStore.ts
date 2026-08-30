import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { HttpError } from '../../../shared/api/httpClient';
import { combatApi } from '../api/combatApi';
import { useCombatPlayback } from '../composables/useCombatPlayback';
import { battleCellKey, reachableCellsFrom } from '../composables/useTacticalBattlePlan';

import type {
  CombatLogEntryDto,
  CombatUsableItemDto,
  CombatantSkillRuntimeDto,
  TacticalCombatEventDto,
  TacticalCombatRuntimeDto,
  TacticalCombatantRuntimeDto,
} from '../types/combatContracts';

/**
 * L'état d'un combat tactique côté client.
 *
 * Store canonique du combat tour par tour : positions, initiative, ressources
 * d'activation et chronologie de mise en scène.
 */
export const useTacticalCombatStore = defineStore('tacticalCombat', () => {
  const playback = useCombatPlayback();

  const combat = ref<TacticalCombatRuntimeDto | null>(null);
  const logEntries = ref<CombatLogEntryDto[]>([]);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const isExecuting = ref(false); // Verrou pour éviter les actions concurrentielles

  /** La compétence armée, en attente d'une case. `null` = mode déplacement. */
  const selectedSkillKey = ref<string | null>(null);
  const selectedItemId = ref<string | null>(null);

  // Cache pour les cases atteignables (optimisation O-002)
  const reachableCellsCache = ref<Map<string, Set<string>>>(new Map());

  const allCombatants = computed<TacticalCombatantRuntimeDto[]>(() =>
    combat.value ? [...combat.value.allies, ...combat.value.enemies] : [],
  );

  const activeCombatant = computed<TacticalCombatantRuntimeDto | null>(() => {
    const activeId = combat.value?.activeCombatantId;
    if (!activeId) return null;

    return allCombatants.value.find((c) => c.combatant.id === activeId) ?? null;
  });

  /**
   * Le joueur n'a la main que si le combattant actif est de son camp <b>et</b> que plus rien ne
   * se joue à l'écran : cliquer au milieu d'une marche adverse produirait un ordre fondé sur un
   * plateau que le joueur ne voit pas encore.
   */
  const isPlayerTurn = computed(
    () => activeCombatant.value?.combatant.side === 'Player' && !playback.isPlaying.value,
  );

  const activeSkills = computed<CombatantSkillRuntimeDto[]>(
    () => activeCombatant.value?.combatant.skills ?? [],
  );

  const selectedSkill = computed<CombatantSkillRuntimeDto | null>(() => {
    if (!selectedSkillKey.value) return null;

    return activeSkills.value.find((s) => s.key === selectedSkillKey.value) ?? null;
  });

  const usableItems = computed<CombatUsableItemDto[]>(
    () => combat.value?.usableBattleItems ?? [],
  );

  const selectedItem = computed<CombatUsableItemDto | null>(() =>
    usableItems.value.find((item) => item.itemId === selectedItemId.value) ?? null,
  );

  /**
   * L'ordre d'action du round, résolu en combattants. Ce que le joueur doit
   * pouvoir lire d'un coup d'œil : c'est la contrepartie annoncée de l'abandon
   * de l'ancien tempo temps-réel.
   */
  const initiativeQueue = computed<TacticalCombatantRuntimeDto[]>(() => {
    if (!combat.value) return [];

    return combat.value.initiativeOrder
      .map((id) => allCombatants.value.find((c) => c.combatant.id === id))
      .filter((c): c is TacticalCombatantRuntimeDto => c !== undefined);
  });

  /**
   * Cases occupées par des combattants encore en vie (pour éviter les déplacements invalides).
   */
  const occupiedCells = computed<Set<string>>(() => {
    const occupied = new Set<string>();
    for (const combatant of allCombatants.value) {
      if (combatant.combatant.status !== 'Defeated') {
        occupied.add(battleCellKey(combatant.x, combatant.y));
      }
    }
    return occupied;
  });

  /**
   * Cases atteignables par le combattant actif (avec cache pour optimisation).
   */
  const reachableCells = computed<Set<string>>(() => {
    const combatant = activeCombatant.value;
    if (!combatant) return new Set();

    const cacheKey = `${combatant.combatant.id}-${combatant.movementBudget}`;
    if (reachableCellsCache.value.has(cacheKey)) {
      return reachableCellsCache.value.get(cacheKey)!;
    }

    const battlefieldInput = {
      gridWidth: combat.value?.battlefield.width ?? 0,
      gridHeight: combat.value?.battlefield.height ?? 0,
      elevation: combat.value?.battlefield.elevation ?? [],
      walkable: combat.value?.battlefield.walkable ?? [],
    };

    const reachable = reachableCellsFrom(
      battlefieldInput,
      { x: combatant.x, y: combatant.y },
      combatant.movementBudget,
      occupiedCells.value
    );

    reachableCellsCache.value.set(cacheKey, reachable);
    return reachable;
  });

  /**
   * Indique si une action est en cours de sélection (U-001).
   */
  const hasPendingAction = computed<boolean>(() => {
    return selectedSkillKey.value !== null || selectedItemId.value !== null;
  });

  const occupantAt = (x: number, y: number): TacticalCombatantRuntimeDto | null =>
    allCombatants.value.find(
      (c) => c.x === x && c.y === y && c.combatant.status !== 'Defeated',
    ) ?? null;

  function setCombat(next: TacticalCombatRuntimeDto) {
    combat.value = next;
    // Une compétence armée n'a plus de sens dès que le tour change de main.
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    // Réinitialiser le cache des cases atteignables
    reachableCellsCache.value.clear();
  }

  function clearCombat() {
    playback.reset();
    combat.value = null;
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    error.value = null;
    reachableCellsCache.value.clear();
  }

  function selectSkill(skillKey: string | null) {
    selectedSkillKey.value = selectedSkillKey.value === skillKey ? null : skillKey;
    selectedItemId.value = null;
  }

  function selectItem(itemId: string | null) {
    selectedItemId.value = selectedItemId.value === itemId ? null : itemId;
    selectedSkillKey.value = null;
  }

  /**
   * Annule l'action en cours (U-001).
   */
  function cancelAction() {
    selectedSkillKey.value = null;
    selectedItemId.value = null;
  }

  /**
   * Enveloppe commune aux trois actions. Aucune pré-validation côté client :
   * portée, budget et occupation sont tranchés par le domaine, dont le message
   * d'erreur remonte tel quel — même patron optimiste que `runStore`.
   *
   * Modifié pour éviter les appels concurrentiels (O-004).
   */
  async function execute(action: () => Promise<{
    combat: TacticalCombatRuntimeDto;
    logEntries: CombatLogEntryDto[];
    events: TacticalCombatEventDto[];
  }>) {
    // Si une action est déjà en cours, ignorer (O-004)
    if (isExecuting.value) return;

    isExecuting.value = true;
    isLoading.value = true;
    error.value = null;

    try {
      // Épingler AVANT d'appliquer : le nouvel état place déjà chaque figure à son arrivée, et
      // sans ce relevé la marche partirait de sa destination.
      playback.pinBefore(combat.value);

      const response = await action();
      setCombat(response.combat);
      logEntries.value = [...logEntries.value, ...response.logEntries];

      await playback.play(response.events ?? [], response.combat, () => performance.now());
    } catch (caught) {
      playback.stop();

      // Le code HTTP accompagne le message : un refus du domaine (409) et une panne serveur
      // (500) demandent des réactions opposées, et « aucun message » n'aide personne à
      // trancher entre les deux.
      error.value = caught instanceof HttpError
        ? `[${caught.status}] ${caught.message}`
        : caught instanceof Error ? caught.message : String(caught);
    } finally {
      isExecuting.value = false;
      isLoading.value = false;
    }
  }

  const moveTo = (runId: string, x: number, y: number) =>
    execute(() => combatApi.moveTacticalCombatant(runId, x, y));

  const useSkillAt = (
    runId: string,
    skillKey: string,
    x: number,
    y: number,
    confirmVitalitySacrifice = false,
  ) => execute(() => combatApi.useTacticalSkill(
    runId, skillKey, x, y, confirmVitalitySacrifice,
  ));

  const useItemAt = (
    runId: string,
    itemId: string,
    x: number,
    y: number,
    targetCombatantId?: string,
  ) => execute(() => combatApi.useTacticalItem(
    runId, itemId, x, y, targetCombatantId,
  ));

  const endTurn = (runId: string) => execute(() => combatApi.endTacticalTurn(runId));

  /**
   * Rejoue les tours ennemis déjà résolus à l'ouverture du combat. Pas d'appel réseau : le
   * serveur les a joués en créant le combat, on n'en montre que la mise en scène.
   */
  async function playOpening(
    events: TacticalCombatEventDto[],
    state: TacticalCombatRuntimeDto,
  ) {
    if (events.length === 0) return;

    await playback.play(events, state, () => performance.now());
  }

  return {
    combat,
    logEntries,
    isLoading,
    error,
    isExecuting, // Exposé pour l'UI (ex: désactiver les boutons)
    selectedSkillKey,
    selectedSkill,
    selectedItemId,
    selectedItem,
    usableItems,
    hasPendingAction, // Exposé pour l'UI (U-001)
    activeCombatant,
    activeSkills,
    allCombatants,
    initiativeQueue,
    isPlayerTurn,
    occupantAt,
    occupiedCells, // Exposé pour l'UI (O-006)
    reachableCells, // Exposé pour l'UI (O-006)
    playback,
    setCombat,
    clearCombat,
    selectSkill,
    selectItem,
    cancelAction, // Exposé pour l'UI (U-001)
    moveTo,
    useSkillAt,
    useItemAt,
    endTurn,
    playOpening,
  };
});
