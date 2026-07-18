import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { HttpError } from '../../../shared/api/httpClient';
import { combatApi } from '../api/combatApi';

import type {
  CombatantRuntimeDto,
  CombatantSkillRuntimeDto,
  CombatLogEntryDto,
  CombatRuntimeDto,
  CombatUsableItemDto,
  EmotionalType,
  SkillCategory,
  TargetingType,
} from '../types/combatContracts';

export type CombatTerminalEvent =
  | { kind: 'victory' }
  | { kind: 'defeat' }
  | null;

export type CombatFeedbackEvent = {
  id: string;
  combatantId: string;
  type: 'damage' | 'heal' | 'guard' | 'miss' | 'status';
  amount: number;
  category?: SkillCategory;
  isCritical?: boolean;
  /** The casting skill's own "élément" — drives the glyph/color for 'status' events. */
  emotionalType?: EmotionalType;
};

type CombatantStateOverride = Pick<CombatantRuntimeDto, 'currentVitality' | 'guard' | 'status'>;

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
  const recentlyMagicHitIds = ref<string[]>([]);
  const recentlyCriticalHitIds = ref<string[]>([]);
  const recentlyMissedIds = ref<string[]>([]);
  const feedbackEvents = ref<CombatFeedbackEvent[]>([]);
  // "X utilise Y sur Z !" — set for the whole duration of an action (see
  // playCombatLogs) so it stays visible until that action's log sequence
  // finishes playing out, not just for a single hit's brief animation.
  const activeActionBanner = ref<string | null>(null);
  const combatantStateOverrides = ref<Record<string, CombatantStateOverride>>({});
  const processedLogKeys = ref<Set<string>>(new Set());
  const animationTimers: ReturnType<typeof globalThis.setTimeout>[] = [];
  // Targets flagged by a 'CriticalHit' log entry, consumed by the DamageApplied
  // entry(ies) that immediately follow it for the same target within one batch.
  let pendingCriticalTargetIds = new Set<string>();

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
    if (isLoading.value) return false;
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
    if (isLoading.value) return false;
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
    if (isLoading.value) return;
    if (!canSubmitItem.value) return;
    const item = selectedItem.value;
    const combatId = combat.value?.id;
    if (!item || !combatId) return;

    const targetIds =
      item.targetingType === 'Self' ? [] : [...selectedTargetIds.value];

    isLoading.value = true;
    error.value = null;
    try {
      const response = await runExclusive(() => combatApi.useItemAction(runId, combatId, {
        itemId: item.itemId,
        targetIds,
      }));

      selectedItemId.value = null;
      selectedTargetIds.value = [];
      await playCombatLogs(response.logEntries);
      finishCombatResponse(response.combat);
      onCombatApplied?.(response.combat);

      if (response.combatCompleted) terminalEvent.value = { kind: 'victory' };
      else if (response.combatFailed) terminalEvent.value = { kind: 'defeat' };
      // enemy turns are handled by the always-running real-time clock
    } catch (caught) {
      error.value =
        caught instanceof Error ? caught.message : "L'utilisation a échoué.";
      // Same stale-selection retry-loop guard as submitAction (see comment there).
      selectedItemId.value = null;
      selectedTargetIds.value = [];
      const current = await combatApi.getCurrentCombat(runId).catch(() => undefined);
      if (current) applyClockCombat(current);
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
    if (combat.value?.id !== combatData.id) {
      combatantStateOverrides.value = {};
      processedLogKeys.value = new Set();
    }
    combat.value = mergeCombatResponseWithAnimatedState(combatData);
    hasRuntimeCombat.value = true;
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
    terminalEvent.value = null;
  }

  function setCombatFromResponse(combatData: CombatRuntimeDto, newLogs: CombatLogEntryDto[]) {
    if (combat.value?.id !== combatData.id) processedLogKeys.value = new Set();
    combat.value = combatData;
    rememberCombatState(combatData);
    hasRuntimeCombat.value = true;
    logEntries.value = [...logEntries.value, ...newLogs];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
  }

  function finishCombatResponse(combatData: CombatRuntimeDto) {
    combat.value = combatData;
    rememberCombatState(combatData);
    hasRuntimeCombat.value = true;
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
  }

    // Apply a combat snapshot from the real-time clock WITHOUT touching the
  // player's current skill/target selection (the player is mid-decision).
  function applyClockCombat(combatData: CombatRuntimeDto) {
    combat.value = combatData;
    rememberCombatState(combatData);
    hasRuntimeCombat.value = true;
  }

  function mergeCombatResponseWithAnimatedState(combatData: CombatRuntimeDto): CombatRuntimeDto {
    if (!combat.value || combat.value.id !== combatData.id) return combatData;

    return {
      ...combatData,
      allies: mergeCombatants(combatData.allies),
      enemies: mergeCombatants(combatData.enemies),
    };
  }

    // Serialise every combat-mutating server call so the real-time clock and the
  // player's own actions never write the run concurrently (which could resurrect
  // a just-killed combat).
  let mutationGate: Promise<unknown> = Promise.resolve();
  function runExclusive<T>(task: () => Promise<T>): Promise<T> {
    const result = mutationGate.then(task, task);
    mutationGate = result.then(() => undefined, () => undefined);
    return result;
  }

  function mergeCombatants(combatants: CombatantRuntimeDto[]): CombatantRuntimeDto[] {
    return combatants.map((combatant) => {
      const override = combatantStateOverrides.value[combatant.id];
      if (override) return { ...combatant, ...override };

      const animated = findCombatantById(combatant.id);
      if (!animated) return combatant;

      return {
        ...combatant,
        currentVitality: Math.min(combatant.currentVitality, animated.currentVitality),
        guard: Math.min(combatant.guard, animated.guard),
        status: animated.status === 'Defeated' ? 'Defeated' : combatant.status,
      };
    });
  }

  function clearCombat() {
    resetAnimationState();
    combat.value = null;
    hasRuntimeCombat.value = false;
    combatantStateOverrides.value = {};
    processedLogKeys.value = new Set();
    logEntries.value = [];
    selectedSkillKey.value = null;
    selectedItemId.value = null;
    selectedTargetIds.value = [];
    error.value = null;
    terminalEvent.value = null;
  }

  // ── Animations ───────────────────────────────────────────────────────────

  const pendingDelays = new Set<() => void>();

  function delay(milliseconds: number): Promise<void> {
    return new Promise((resolve) => {
      let timerId: ReturnType<typeof globalThis.setTimeout>;
      const done = () => {
        pendingDelays.delete(done);
        const index = animationTimers.indexOf(timerId);
        if (index >= 0) animationTimers.splice(index, 1);
        resolve();
      };
      timerId = globalThis.setTimeout(done, milliseconds);
      animationTimers.push(timerId);
      pendingDelays.add(done);
    });
  }

  async function playCombatLogs(entries: CombatLogEntryDto[]) {
    pendingCriticalTargetIds = new Set<string>();
    const actionBanners = buildActionBanners(entries);

    try {
      for (let index = 0; index < entries.length; index++) {
        const entry = entries[index];
        const logKey = createLogKey(entry);
        if (processedLogKeys.value.has(logKey)) continue;
        processedLogKeys.value = new Set([...processedLogKeys.value, logKey]);

        logEntries.value = [...logEntries.value, entry];

        const banner = actionBanners.get(index);
        if (banner) {
          activeActionBanner.value = banner.text;
          // A DamageApplied/HealApplied/etc. entry almost always follows right after
          // and already awaits its own delay below, which keeps the banner visible
          // for free. Only force a minimum delay here for the rare case where no
          // such entry follows (e.g. a RestoreMana skill on an already-full target
          // produces no log at all) — otherwise a same-tick set-then-clear would
          // never actually render.
          if (banner.needsFallbackDelay) await delay(500);
        }

        if (entry.actorId && (entry.type === 'SkillUsed' || entry.type === 'ItemUsed')) {
          markActing(entry.actorId);
        }

        if (entry.type === 'CriticalHit') {
          for (const targetId of entry.targetIds) pendingCriticalTargetIds.add(targetId);
        }

        if (entry.type === 'EnemyTurnResolved' && entry.actorId) {
          thinkingCombatantId.value = entry.actorId;
          markActing(entry.actorId, 900);
          await delay(2000);
          thinkingCombatantId.value = null;
          await delay(250);
        } else if (entry.targetIds.length > 0 && shouldHighlightTarget(entry)) {
          applyLogDelta(entry);
          markFeedbackFromLog(entry);
          await delay(550);
          await delay(150);
        }
      }
    } finally {
      // The banner spans one action's whole log sequence — clear it only once
      // that sequence (and thus the action's animation) has fully played out.
      activeActionBanner.value = null;
    }
  }

  // An entry that itself causes playCombatLogs to await a delay — used below to
  // decide whether an action banner already gets a "free ride" from the entry
  // immediately following it, or needs its own fallback delay.
  function entryTriggersDelay(entry: CombatLogEntryDto): boolean {
    if (entry.type === 'EnemyTurnResolved' && entry.actorId) return true;
    return entry.targetIds.length > 0 && shouldHighlightTarget(entry);
  }

  type ActionBanner = { text: string; needsFallbackDelay: boolean };

  // Pre-scans one server response's log entries and, for every SkillUsed/
  // ItemUsed entry, builds the "X utilise Y sur Z !" banner text — merging
  // consecutive entries for the same actor+skill (AllEnemies/AllAllies skills
  // emit one SkillUsed entry per target) so the banner lists every target once.
  // Returns a map keyed by the index of each group's FIRST entry.
  function buildActionBanners(entries: CombatLogEntryDto[]): Map<number, ActionBanner> {
    const banners = new Map<number, ActionBanner>();

    for (let index = 0; index < entries.length; index++) {
      const entry = entries[index];
      const isPreviousSameGroup =
        index > 0 &&
        entries[index - 1].type === entry.type &&
        entries[index - 1].actorId === entry.actorId &&
        entries[index - 1].skillKey === entry.skillKey;
      if (isPreviousSameGroup) continue;
      if (entry.type !== 'SkillUsed' && entry.type !== 'ItemUsed') continue;
      if (!entry.actorId) continue;

      const actor = findCombatantById(entry.actorId);
      if (!actor) continue;

      const targetIds = new Set<string>(entry.targetIds);
      let cursor = index;
      while (
        cursor + 1 < entries.length &&
        entries[cursor + 1].type === entry.type &&
        entries[cursor + 1].actorId === entry.actorId &&
        entries[cursor + 1].skillKey === entry.skillKey
      ) {
        cursor += 1;
        for (const id of entries[cursor].targetIds) targetIds.add(id);
      }

      const targetNames = [...targetIds]
        .map((id) => findCombatantById(id)?.displayName)
        .filter((name): name is string => Boolean(name));

      const actionName =
        entry.type === 'ItemUsed'
          ? (combat.value?.usableBattleItems.find((i) => i.definitionKey === entry.skillKey)?.displayName
              ?? entry.skillKey ?? 'un objet')
          : (actor.skills.find((s) => s.key === entry.skillKey)?.displayName ?? entry.skillKey ?? 'une capacité');

      const text = targetNames.length > 0
        ? `${actor.displayName} utilise ${actionName} sur ${targetNames.join(', ')} !`
        : `${actor.displayName} utilise ${actionName} !`;
      const nextEntry = entries[cursor + 1];
      const needsFallbackDelay = !nextEntry || !entryTriggersDelay(nextEntry);

      banners.set(index, { text, needsFallbackDelay });
    }

    return banners;
  }

  function parseLogAmount(message: string): number {
    const matches = message.match(/\d+/g);
    if (!matches?.length) return 0;
    return Number(matches[matches.length - 1]) || 0;
  }

  function applyLogDelta(entry: CombatLogEntryDto) {
    if (!combat.value) return;

    for (const targetId of entry.targetIds) {
      const target = findCombatantById(targetId);
      if (!target) continue;

      if (entry.type === 'TargetDefeated') {
        target.currentVitality = 0;
        target.status = 'Defeated';
        rememberCombatantState(target);
        continue;
      }

      if (entry.type === 'AttackMissed') {
        pushFeedbackEvent(target.id, 'miss', 0);
        continue;
      }

      if (entry.type === 'StatusApplied') {
        pushFeedbackEvent(target.id, 'status', 0, undefined, undefined, resolveSkillEmotionalType(entry));
        continue;
      }

      const amount = parseLogAmount(entry.message);
      if (amount <= 0) continue;

      if (entry.type === 'DamageApplied') {
        const category = resolveSkillCategory(entry);
        const emotionalType = resolveSkillEmotionalType(entry);
        const isCritical = pendingCriticalTargetIds.has(target.id);
        if (isGuardAbsorbLog(entry)) {
          target.guard = Math.max(0, target.guard - amount);
          pushFeedbackEvent(target.id, 'guard', amount);
        } else {
          target.currentVitality = Math.max(0, target.currentVitality - amount);
          pushFeedbackEvent(target.id, 'damage', amount, category, isCritical, emotionalType);
        }
      } else if (entry.type === 'HealApplied') {
        target.currentVitality = Math.min(target.maxVitality, target.currentVitality + amount);
        pushFeedbackEvent(target.id, 'heal', amount);
      } else if (entry.type === 'GuardGained') {
        target.guard += amount;
        pushFeedbackEvent(target.id, 'guard', amount);
      }

      rememberCombatantState(target);
    }
  }

  function rememberCombatantState(combatant: CombatantRuntimeDto) {
    combatantStateOverrides.value = {
      ...combatantStateOverrides.value,
      [combatant.id]: {
        currentVitality: combatant.currentVitality,
        guard: combatant.guard,
        status: combatant.status,
      },
    };
  }

  function rememberCombatState(combatData: CombatRuntimeDto) {
    combatantStateOverrides.value = Object.fromEntries(
      [...combatData.allies, ...combatData.enemies].map((combatant) => [
        combatant.id,
        {
          currentVitality: combatant.currentVitality,
          guard: combatant.guard,
          status: combatant.status,
        },
      ]),
    );
  }

  function createLogKey(entry: CombatLogEntryDto): string {
    return [
      combat.value?.id ?? '',
      combat.value?.turnNumber ?? '',
      entry.type,
      entry.actorId ?? '',
      entry.skillKey ?? '',
      entry.targetIds.join(','),
      entry.message,
    ].join('|');
  }

  // A DamageApplied entry always carries the ActorId/SkillKey of the skill that
  // produced it (see CombatSkillEffectResolver.CreateLog) — no correlation with
  // an earlier SkillUsed entry is needed to know whether the hit was Magic or
  // Physical.
  function resolveSkillCategory(entry: CombatLogEntryDto): SkillCategory | undefined {
    if (!entry.actorId || !entry.skillKey) return undefined;
    const actor = findCombatantById(entry.actorId);
    return actor?.skills.find((s) => s.key === entry.skillKey)?.category;
  }

  // Mirrors resolveSkillCategory — the casting skill's own "élément", used to
  // pick the visual (glyph/color) for a per-family status/damage animation.
  function resolveSkillEmotionalType(entry: CombatLogEntryDto): EmotionalType | undefined {
    if (!entry.actorId || !entry.skillKey) return undefined;
    const actor = findCombatantById(entry.actorId);
    return actor?.skills.find((s) => s.key === entry.skillKey)?.emotionalType ?? undefined;
  }

  function pushFeedbackEvent(
    combatantId: string,
    type: CombatFeedbackEvent['type'],
    amount: number,
    category?: SkillCategory,
    isCritical?: boolean,
    emotionalType?: EmotionalType,
  ) {
    const event = {
      id: `${combatantId}-${type}-${Date.now()}-${feedbackEvents.value.length}`,
      combatantId,
      type,
      amount,
      category,
      isCritical,
      emotionalType,
    };
    feedbackEvents.value = [...feedbackEvents.value, event];
    schedule(() => {
      feedbackEvents.value = feedbackEvents.value.filter((item) => item.id !== event.id);
    }, 1200);
  }

  function markFeedbackFromLog(entry: CombatLogEntryDto) {
    if (entry.type === 'AttackMissed') {
      markMissed(entry.targetIds);
    } else if (entry.type === 'DamageApplied' && isGuardAbsorbLog(entry)) {
      markGuarded(entry.targetIds);
    } else if (entry.type === 'DamageApplied') {
      markDamaged(entry.targetIds);
      if (resolveSkillCategory(entry) === 'Magic') markMagicHit(entry.targetIds);
      if (entry.targetIds.some((id) => pendingCriticalTargetIds.has(id))) markCriticalHit(entry.targetIds);
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

  function markMagicHit(targetIds: string[], duration = 700) {
    flashIds(recentlyMagicHitIds, targetIds, duration);
  }

  function markCriticalHit(targetIds: string[], duration = 700) {
    flashIds(recentlyCriticalHitIds, targetIds, duration);
  }

  function markMissed(targetIds: string[], duration = 600) {
    flashIds(recentlyMissedIds, targetIds, duration);
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
    for (const resolveDelay of [...pendingDelays]) resolveDelay(); // unblock awaited delays
    pendingDelays.clear();
    thinkingCombatantId.value = null;
    recentlyDamagedIds.value = [];
    recentlyGuardedIds.value = [];
    recentlyDefeatedIds.value = [];
    recentlyActingId.value = null;
    recentlyMagicHitIds.value = [];
    recentlyCriticalHitIds.value = [];
    recentlyMissedIds.value = [];
    feedbackEvents.value = [];
    activeActionBanner.value = null;
    pendingCriticalTargetIds = new Set<string>();
  }

  function shouldHighlightTarget(entry: CombatLogEntryDto): boolean {
    return (
      entry.type === 'DamageApplied' ||
      entry.type === 'GuardGained' ||
      entry.type === 'HealApplied' ||
      entry.type === 'TargetDefeated' ||
      entry.type === 'AttackMissed' ||
      entry.type === 'StatusApplied'
    );
  }

  function isGuardAbsorbLog(entry: CombatLogEntryDto): boolean {
    return entry.type === 'DamageApplied' && entry.message.toLowerCase().includes('guard absorbs');
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

    // AllEnemies/AllAllies skills have no single target to click — the target
    // set is fully implied by the skill itself, so auto-populate it here
    // (mirrors the Self-targeting auto-submit special-case for items).
    const skill = selectedSkill.value;
    if (skill && (skill.targetingType === 'AllEnemies' || skill.targetingType === 'AllAllies')) {
      selectedTargetIds.value = validTargets.value.map((t) => t.id);
    }
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

    function activeIsEnemy(): boolean {
    const id = combat.value?.activeCombatantId;
    if (!id) return false;
    const c = allCombatants.value.find((x) => x.id === id);
    return c?.side === 'Enemy' && c.status !== 'Defeated';
  }

  let combatClockRunning = false;
  async function runCombatClock(runId: string, onCombatApplied?: (combat: CombatRuntimeDto) => void) {
    if (combatClockRunning) return;
    combatClockRunning = true;
    const TICK_INTERVAL = 480; // ms between real-time ticks
    const TICK_DELTA = 340;    // ATB ticks advanced per call ("time keeps flowing")
    try {
      while (combat.value?.status === 'Active' && terminalEvent.value === null) {
        await delay(TICK_INTERVAL);
        if (combat.value?.status !== 'Active' || terminalEvent.value !== null) break;

        // The world pauses only while the player's OWN action resolves/animates.
        if (isLoading.value) continue;

        const combatId = combat.value.id;
        let response: Awaited<ReturnType<typeof combatApi.hold>> | null = null;
        try {
          response = await runExclusive(() => combatApi.hold(runId, combatId, TICK_DELTA));
        } catch (caught) {
          // A 409/404 means the server has no active combat for this hold. That can be
          // a transient persistence race during a live fight (atomic-write gap) OR a
          // genuinely finished/cleared combat that left a stale Active snapshot here —
          // in which case the clock would otherwise poll /hold forever on the map.
          // Confirm with current-combat before tearing down so we never kill a live fight.
          if (caught instanceof HttpError && (caught.status === 409 || caught.status === 404)) {
            const current = await combatApi.getCurrentCombat(runId).catch(() => undefined);
            if (current === null) {
              // Server confirms: no active combat anymore. Stop the clock for good.
              combat.value = null;
              hasRuntimeCombat.value = false;
              break;
            }
          }
          continue; // transient — retry next tick
        }
        if (!response) continue; 
        if (terminalEvent.value !== null) break; // player ended combat meanwhile — discard tick

        await playCombatLogs(response.logEntries);
        if (isLoading.value || terminalEvent.value !== null) break;

        applyClockCombat(response.combat);
        onCombatApplied?.(response.combat);

        if (response.combatCompleted) { terminalEvent.value = { kind: 'victory' }; break; }
        if (response.combatFailed)    { terminalEvent.value = { kind: 'defeat' };  break; }
      }
    } finally {
      combatClockRunning = false;
    }
  }

  async function submitAction(runId: string, onCombatApplied?: (combat: CombatRuntimeDto) => void) {
    if (isLoading.value) return;
    if (!canSubmit.value) return;
    const actor = currentActor.value;
    const skill = selectedSkill.value;
    const combatId = combat.value?.id;
    if (!actor || !skill || !combatId) return;

    isLoading.value = true;
    error.value = null;
    try {
      const response = await runExclusive(() => combatApi.useSkillAction(runId, combatId, {
        actorId: actor.id,
        skillKey: skill.key,
        targetIds: selectedTargetIds.value,
      }));

      selectedSkillKey.value = null;
      selectedTargetIds.value = [];
      await playCombatLogs(response.logEntries);
      finishCombatResponse(response.combat);
      onCombatApplied?.(response.combat);

      if (response.combatCompleted) terminalEvent.value = { kind: 'victory' };
      else if (response.combatFailed) terminalEvent.value = { kind: 'defeat' };
      // enemy turns are handled by the always-running real-time clock

    } catch (caught) {
      error.value =
        caught instanceof Error ? caught.message : "L'action a échoué.";
      // Clear the stale selection and resync from the server so the auto-submit
      // watcher in CombatScene.vue can't immediately retry the same now-invalid
      // action (e.g. after a "not this combatant's turn" rejection) — without
      // this, isLoading flipping back to false re-satisfies canSubmit with the
      // exact same stale actor/skill/targets, producing a retry loop.
      selectedSkillKey.value = null;
      selectedTargetIds.value = [];
      const current = await combatApi.getCurrentCombat(runId).catch(() => undefined);
      if (current) applyClockCombat(current);
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
    recentlyMagicHitIds,
    recentlyCriticalHitIds,
    recentlyMissedIds,
    feedbackEvents,
    activeActionBanner,
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
    activeIsEnemy,
    initCombat,
    setCombatFromResponse,
    finishCombatResponse,
    playCombatLogs,
    clearCombat,
    runCombatClock,
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
    markMagicHit,
    markCriticalHit,
    markMissed,
    resetAnimationState,
  };
});
