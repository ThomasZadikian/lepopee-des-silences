import { computed, ref, shallowRef } from 'vue';

import type {
  CombatantStatusEffectDto,
  TacticalCombatEventDto,
  TacticalCombatRuntimeDto,
} from '../types/combatContracts';

/**
 * Le rythme du combat tactique.
 *
 * Le serveur résout un tour entier d'un coup ; sans mise en scène, le joueur verrait des
 * figures se téléporter et des dégâts surgir sans cause. Ce lecteur déroule la chronologie
 * renvoyée à côté de l'état final, en respectant les temps de la référence de conception.
 *
 * Aucune de ces durées n'est cosmétique. Le temps de réflexion donne à l'adversaire l'air de
 * décider plutôt que de réagir ; le pas de marche rend le trajet lisible, en particulier quand
 * il contourne un mur ; la pause finale laisse le coup se déposer avant que la main revienne.
 *
 * Optimisé pour O-014 : timings dynamiques en fonction de la distance.
 * Optimisé pour O-017 : transitions entre les tours (fondu).
 * Optimisé pour O-018 : timings spécifiques pour les ennemis.
 */

/**
 * Multiplicateur global de rythme, appliqué à toutes les durées de mise en scène du combat
 * (ici, mais aussi useSortEffects et TacticalCombatScene, qui l'importent) — un seul cadran
 * plutôt que de retoucher chaque BALANCE KNOB séparément. >1 ralentit l'ensemble, <1
 * l'accélère. // BALANCE KNOB
 */
export const PACE = 1.25;

/** Avant qu'un ennemi n'entame son tour : il regarde le terrain. // BALANCE KNOB */
export const THINK_MS = Math.round(520 * PACE);

/** Par case franchie (base). // BALANCE KNOB */
export const BASE_STEP_MS = Math.round(145 * PACE);

/** Après le geste d'un ennemi, avant de passer la main. // BALANCE KNOB */
export const SETTLE_MS = Math.round(620 * PACE);

/** Durée de vie d'un chiffre flottant, et hauteur de sa montée. */
export const FLOAT_MS = Math.round(1100 * PACE);
export const FLOAT_RISE_PX = 30;

/**
 * Durée de vie d'une onde d'impact — partagée entre la file qui la conserve ici (pruneImpacts)
 * et le rendu qui la fait s'estomper (TacticalCombatScene.paintImpacts). Deux copies de cette
 * valeur qui divergent font disparaître l'impact d'un coup, avant la fin de son fondu.
 */
export const IMPACT_MS = Math.round(800 * PACE);

/** Durée de la transition entre les tours (fondu). // O-017 */
export const TURN_TRANSITION_MS = Math.round(300 * PACE);

/**
 * Pause après un tick de DoT/HoT — assez pour que le chiffre qui vient de s'envoler se lise
 * avant que la chronologie n'enchaîne, sans le temps de réflexion ni le settle d'une vraie
 * action : personne n'a rien décidé, un statut vient seulement de faire ce pour quoi il a été
 * posé. // BALANCE KNOB
 */
export const TICK_SETTLE_MS = Math.round(400 * PACE);

/**
 * Pause entre deux impacts d'un même tick — sans elle, plusieurs DoT/HoT empilés sur la même
 * cible (ou deux cibles différentes touchées à l'instant du même tick) font s'envoler leurs
 * chiffres au même pixel et au même instant, et l'un efface visuellement l'autre. Chaque
 * impact se lit donc l'un après l'autre plutôt que tous à la fois. // BALANCE KNOB
 */
export const TICK_IMPACT_STAGGER_MS = Math.round(260 * PACE);

/**
 * Durée pendant laquelle la zone d'un geste adverse reste allumée avant qu'il ne parte.
 *
 * C'est un temps de lecture, pas une temporisation : sans lui, l'adversaire se déplace et
 * frappe dans le même souffle, et le joueur découvre la portée d'une compétence en la
 * subissant. Calé assez haut pour qu'on ait le temps de compter les cases, assez bas pour
 * qu'une file de six créatures ne devienne pas une attente. // BALANCE KNOB
 */
export const TELEGRAPH_MS = Math.round(1200 * PACE);

/**
 * L'annonce d'un geste à venir : ce que l'adversaire s'apprête à couvrir.
 *
 * Vit ici et non dans le rendu parce que sa durée fait partie du rythme du combat, au même
 * titre que le pas de marche ou la retombée d'un coup.
 */
export type Telegraph = {
  /**
   * `Move` trace un trajet, une action trace une zone d'impact — deux lectures distinctes.
   * `Tick` ne s'annonce jamais (voir `play()`) mais figure ici pour que ce type reste celui
   * de `TacticalCombatEventDto['kind']` sans conversion.
   */
  kind: 'Move' | 'Skill' | 'Item' | 'Tick';
  cells: Array<{ x: number; y: number }>;
  label: string;
};

/** Multiplicateur de timing pour les ennemis (O-018). */
export const ENEMY_STEP_MULTIPLIER = 1.2; // 20% plus lent
/** Multiplicateur de timing pour le settle des ennemis (O-018). */
export const ENEMY_SETTLE_MULTIPLIER = 1.5; // 50% plus long

export function dynamicStepDurationMs(pathLength: number, isEnemy = false): number {
  const baseStep = isEnemy
    ? Math.floor(BASE_STEP_MS * ENEMY_STEP_MULTIPLIER)
    : BASE_STEP_MS;

  if (pathLength <= 2) return Math.floor(baseStep * 0.7);
  if (pathLength >= 5) return Math.floor(baseStep * 1.3);
  return baseStep;
}

export type FloatingNumber = {
  id: number;
  x: number;
  y: number;
  text: string;
  color: string;
  bornAt: number;
  /** `guard` paints a small shield glyph beside the number (see TacticalCombatScene's
   * paintFloatingNumbers) — what Garde absorbed reads as a distinct event from a real hit,
   * never as a duller version of the same one. `effectiveness` is the Faiblesse/Résistance/
   * Immunisé label from the type system, painted higher up so it never overlaps the
   * damage number it explains. */
  kind?: 'damage' | 'guard' | 'effectiveness';
};

export type ImpactEffect = {
  id: number;
  x: number;
  y: number;
  color: string;
  bornAt: number;
};

export type PendingSort = {
  skillKey: string;
  x: number;
  y: number;
  casterX: number;
  casterY: number;
};

export type WalkAnimation = {
  combatantId: string;
  path: Array<{ x: number; y: number }>;
  startedAt: number;
  isEnemy: boolean;
};

const ALLY_HIT_COLOR = '#ff8f7a';
const ENEMY_HIT_COLOR = '#ffd98a';
const HEAL_COLOR = '#86dcb4';
/** Gris froid : un coup manqué n'est pas une quantité, il ne doit pas se lire comme un chiffre. */
const MISS_COLOR = '#c3c0d6';
/** Jaune franc, associé au petit bouclier peint à côté (voir paintFloatingNumbers) — jamais
 * confondu avec ENEMY_HIT_COLOR malgré la parenté de teinte, l'icône fait la différence. */
const GUARD_COLOR = '#ffcc33';
/** Rouge vif : une Faiblesse amplifie les dégâts, elle doit lire comme une bonne nouvelle
 * pour qui frappe. */
const WEAKNESS_COLOR = '#ff5c4d';
/** Bleu froid : une Résistance atténue les dégâts, à l'opposé de la Faiblesse. */
const RESISTANT_COLOR = '#7fb2e0';
/** Gris neutre, même famille que MISS_COLOR : une immunité n'est pas un chiffre non plus. */
const IMMUNE_COLOR = '#c3c0d6';

const EFFECTIVENESS_LABELS: Record<'Weak' | 'Resistant' | 'Immune', string> = {
  Weak: 'Faiblesse',
  Resistant: 'Résistance',
  Immune: 'Immunisé',
};

const EFFECTIVENESS_COLORS: Record<'Weak' | 'Resistant' | 'Immune', string> = {
  Weak: WEAKNESS_COLOR,
  Resistant: RESISTANT_COLOR,
  Immune: IMMUNE_COLOR,
};

export function useCombatPlayback() {
  const walk = shallowRef<WalkAnimation | null>(null);
  const floats = ref<FloatingNumber[]>([]);
  const impacts = ref<ImpactEffect[]>([]);
  const pendingSorts = ref<PendingSort[]>([]);
  const isPlaying = ref(false);

  const actionBanner = ref<string | null>(null);

  const pinned = ref<Record<string, { x: number; y: number }>>({});

  /**
   * Vitalité affichée pendant une chronologie, par combattant.
   *
   * Le serveur résout un tour entier (les deux ennemis compris) avant de répondre : sans ce
   * relevé, `combat.value` porte déjà le total final dès la réponse, et la barre de vie du
   * joueur s'effondre au premier instant du tour ennemi — avant même que le premier ennemi
   * n'ait animé son geste. Reconstruit la vitalité de départ en additionnant les
   * `vitalityDelta` de la chronologie (positif = perdu) par-dessus la valeur finale, puis les
   * retranche un à un au fil de la lecture, pour que la barre ne bouge qu'au moment exact où
   * le coup qui l'explique atterrit.
   */
  const displayVitals = ref<Record<string, number>>({});

  /**
   * États actifs affichés pendant une chronologie, par combattant.
   *
   * Contrairement à la vitalité, la chronologie ne détaille pas quel geste applique, empile ou
   * expire un état (`TacticalImpactDto` ne porte qu'un delta de vitalité) — impossible de la
   * reconstruire coup par coup. On fige donc la liste telle qu'elle était juste avant l'action
   * (voir `pinBefore`) pendant toute la lecture, et on la relâche une fois la chronologie
   * terminée : un stack de dégât continu ou un affaiblissement n'apparaît/ne change qu'une fois
   * le geste qui l'explique effectivement joué à l'écran, jamais dès la réponse serveur.
   */
  const displayStatusEffects = ref<Record<string, CombatantStatusEffectDto[]>>({});

  // O-017: État de transition entre les tours
  const isTransitioning = ref(false);
  const transitionPhase = ref<'fadeOut' | 'fadeIn' | null>(null);

  /** La zone annoncée par le geste adverse en cours de préparation. */
  const telegraph = shallowRef<Telegraph | null>(null);

  let floatSeq = 0;
  let impactSeq = 0;
  let timers: ReturnType<typeof globalThis.setTimeout>[] = [];

  const wait = (ms: number) =>
    new Promise<void>((resolve) => {
      timers.push(globalThis.setTimeout(resolve, ms));
    });

  function stop() {
    timers.forEach(globalThis.clearTimeout);
    timers = [];
    walk.value = null;
    pinned.value = {};
    displayVitals.value = {};
    displayStatusEffects.value = {};
    actionBanner.value = null;
    pendingSorts.value = [];
    telegraph.value = null;
    isPlaying.value = false;
    // O-017: Réinitialiser l'état de transition
    isTransitioning.value = false;
    transitionPhase.value = null;
  }

  /** La vitalité d'un combattant à cet instant : reconstruite pendant la lecture, réelle sinon. */
  function vitalsOf(combatantId: string, settled: number): number {
    const tracked = displayVitals.value[combatantId];
    return tracked === undefined ? settled : tracked;
  }

  /** Les états actifs d'un combattant à cet instant : figés pendant la lecture, réels sinon. */
  function statusEffectsOf(
    combatantId: string,
    settled: CombatantStatusEffectDto[],
  ): CombatantStatusEffectDto[] {
    const tracked = displayStatusEffects.value[combatantId];
    return tracked === undefined ? settled : tracked;
  }

  function reset() {
    stop();
    floats.value = [];
    impacts.value = [];
  }

  function pruneFloats(now: number) {
    if (floats.value.length === 0) return;

    floats.value = floats.value.filter((f) => now - f.bornAt < FLOAT_MS);
  }

  function pruneImpacts(now: number) {
    if (impacts.value.length === 0) return;

    impacts.value = impacts.value.filter((i) => now - i.bornAt < IMPACT_MS);
  }

  function pushImpact(x: number, y: number, targetIsAlly: boolean, now: number) {
    impacts.value = [
      ...impacts.value,
      {
        id: (impactSeq += 1),
        x,
        y,
        color: targetIsAlly ? ALLY_HIT_COLOR : ENEMY_HIT_COLOR,
        bornAt: now,
      },
    ];
  }

  /**
   * Calcule le timing dynamique pour un pas de déplacement (O-014 + O-018).
   * - Déplacements courts (1-2 cases) : plus rapides (70% de BASE_STEP_MS).
   * - Déplacements longs (5+ cases) : plus lents (130% de BASE_STEP_MS).
   * - Ennemis : 20% plus lents (ENEMY_STEP_MULTIPLIER).
   */
  /**
   * La position d'un combattant à cet instant : posée case par case s'il marche, épinglée
   * sinon — jamais interpolée en continu entre deux cases.
   *
   * Aligné sur `usePartyTokenPath` (exploration) : la figure tient chaque case le temps de
   * `stepMs` avant de sauter d'un bloc à la suivante, plutôt que de glisser en continu d'une
   * case à l'autre. Un glissé continu se lit comme flou/approximatif à côté du pas net de
   * l'exploration ; la case par case est le rythme voulu partout, pas seulement hors combat.
   */
  function positionOf(
    combatantId: string,
    settled: { x: number; y: number },
    now: number,
  ): { x: number; y: number } {
    const current = walk.value;

    if (current && current.combatantId === combatantId && current.path.length > 0) {
      const stepMs = dynamicStepDurationMs(current.path.length - 1, current.isEnemy);
      const progress = (now - current.startedAt) / stepMs;

      // `now` vient de l'horodatage de `requestAnimationFrame`, qui date du DÉBUT de la frame
      // et peut donc précéder le `performance.now()` relevé au lancement de la marche. Sans ce
      // plancher, la progression passe négative, l'index aussi, et la lecture d'une case
      // inexistante fait tomber toute la boucle de rendu.
      const step = Math.max(0, Math.floor(progress));
      const at = Math.min(current.path.length - 1, step);

      return { x: current.path[at].x, y: current.path[at].y };
    }

    return pinned.value[combatantId] ?? settled;
  }

  function pushFloat(
    x: number, y: number, delta: number, targetIsAlly: boolean, now: number, missed = false,
  ) {
    // Un soin monte en vert avec un signe explicite : « +8 » et « −8 » ne doivent jamais se
    // confondre d'un coup d'œil.
    const healed = delta < 0;

    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        // Un coup manqué monte en toutes lettres et en gris : c'est un événement, pas une
        // quantité, et rien ne doit laisser croire qu'il a coûté quelque chose.
        text: missed ? 'Manqué' : healed ? `+${Math.abs(delta)}` : `−${delta}`,
        color: missed
          ? MISS_COLOR
          : healed ? HEAL_COLOR : targetIsAlly ? ALLY_HIT_COLOR : ENEMY_HIT_COLOR,
        bornAt: now,
      },
    ];
  }

  /**
   * Ce que la Garde vient d'encaisser — un événement en soi, jamais une variante plus terne
   * d'un coup normal : sans lui, un coup entièrement absorbé (vitalité inchangée) ne laissait
   * absolument rien voir, comme si l'action n'avait eu aucun effet.
   */
  function pushGuardFloat(x: number, y: number, amount: number, now: number) {
    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        text: `−${amount}`,
        color: GUARD_COLOR,
        bornAt: now,
        kind: 'guard',
      },
    ];
  }

  /** Le petit mot "Faiblesse"/"Résistance"/"Immunisé" du système de types émotionnels — un
   * float à part, jamais fondu dans le chiffre de dégâts qu'il explique. */
  function pushEffectivenessFloat(
    x: number, y: number, effectiveness: 'Weak' | 'Resistant' | 'Immune', now: number,
  ) {
    floats.value = [
      ...floats.value,
      {
        id: (floatSeq += 1),
        x,
        y,
        text: EFFECTIVENESS_LABELS[effectiveness],
        color: EFFECTIVENESS_COLORS[effectiveness],
        bornAt: now,
        kind: 'effectiveness',
      },
    ];
  }

  /**
   * Allume la zone du geste à venir, laisse au joueur le temps de la lire, puis l'éteint.
   *
   * Un geste sans cases annoncées (chronologie tronquée, ancien serveur) retombe sur le simple
   * temps de réflexion : mieux vaut une pause muette qu'une annonce vide.
   */
  async function announce(event: TacticalCombatEventDto) {
    const cells = event.telegraphCells ?? [];

    if (cells.length === 0) {
      await wait(THINK_MS);
      return;
    }

    telegraph.value = {
      kind: event.kind,
      cells: cells.map((cell) => ({ x: cell.x, y: cell.y })),
      label: event.kind === 'Move'
        ? `${event.actorName} se déplace`
        : `${event.actorName} prépare « ${event.skillName ?? '…'} »`,
    };

    await wait(TELEGRAPH_MS);
    telegraph.value = null;
  }

  /**
   * Déroule une chronologie. Résout quand tout est joué — l'appelant peut alors rendre la main
   * au joueur en sachant que plus rien ne bouge.
   *
   * Optimisé pour O-014 : utilise des timings dynamiques pour les déplacements.
   * Optimisé pour O-017 : ajoute des transitions entre les tours.
   * Optimisé pour O-018 : timings spécifiques pour les ennemis.
   */
  async function play(
    events: readonly TacticalCombatEventDto[],
    finalState: TacticalCombatRuntimeDto,
    now: () => number,
  ): Promise<void> {
    if (events.length === 0) {
      // Rien à rejouer : relâche immédiatement le gel posé par `pinBefore`, sans quoi les
      // états actifs resteraient figés sur leur valeur d'avant l'action indéfiniment.
      displayStatusEffects.value = {};
      return;
    }

    const allyIds = new Set(finalState.allies.map((a) => a.combatant.id));
    let previousActorId: string | null = null;

    // Reconstruit la vitalité de départ (avant cette chronologie) en additionnant, par
    // combattant, tout ce que la chronologie lui fera perdre ou lui rendra par-dessus l'état
    // final déjà reçu — voir la doc de `displayVitals` plus haut.
    const startingVitals: Record<string, number> = {};
    for (const combatant of [...finalState.allies, ...finalState.enemies]) {
      startingVitals[combatant.combatant.id] = combatant.combatant.currentVitality;
    }
    for (const event of events) {
      for (const impact of event.impacts) {
        if (impact.missed) continue;
        startingVitals[impact.combatantId] =
          (startingVitals[impact.combatantId] ?? 0) + impact.vitalityDelta;
      }
    }
    displayVitals.value = startingVitals;

    isPlaying.value = true;

    try {
      for (const event of events) {
        const actorIsAlly = allyIds.has(event.actorId);

        const beginsEnemyTurn = !actorIsAlly && event.actorId !== previousActorId;

        // Un déplacement et la compétence qui le suit appartiennent au même tour : la
        // transition et le temps de réflexion ne se jouent qu'une fois pour cette paire.
        if (beginsEnemyTurn) {
          isTransitioning.value = true;
          transitionPhase.value = 'fadeOut';
          await wait(TURN_TRANSITION_MS / 2);
          transitionPhase.value = 'fadeIn';
          await wait(TURN_TRANSITION_MS / 2);
          isTransitioning.value = false;
          transitionPhase.value = null;
        }

        // L'adversaire annonce avant d'agir — chaque geste, pas seulement le premier du tour :
        // un déplacement puis une frappe sont deux zones différentes, et c'est justement la
        // seconde que le joueur doit pouvoir lire. Un allié agit sur ordre du joueur, qui vient
        // de désigner sa cible : lui montrer sa propre zone ne lui apprendrait rien et le faire
        // attendre passerait pour de la latence. Un tick n'est ni l'un ni l'autre — personne ne
        // vient de choisir quoi que ce soit, il n'a pas de zone à annoncer.
        if (!actorIsAlly && event.kind !== 'Tick') await announce(event);
        previousActorId = event.actorId;

        if (event.kind === 'Tick') {
          const tickImpacts = event.impacts;
          for (let i = 0; i < tickImpacts.length; i += 1) {
            const impact = tickImpacts[i];
            const at = now();
            const targetIsAlly = allyIds.has(impact.combatantId);
            const guardAbsorbed = impact.guardAbsorbed ?? 0;
            if (guardAbsorbed > 0) pushGuardFloat(impact.x, impact.y, guardAbsorbed, at);
            // Une garde qui absorbe tout laisse la vitalité intacte — un « −0 » ne dirait rien
            // que le chiffre de garde ci-dessus ne dise déjà mieux.
            if (impact.missed || impact.vitalityDelta !== 0) {
              pushFloat(impact.x, impact.y, impact.vitalityDelta, targetIsAlly, at, impact.missed);
            }
            if (!impact.missed) {
              displayVitals.value = {
                ...displayVitals.value,
                [impact.combatantId]:
                  (displayVitals.value[impact.combatantId] ?? 0) - impact.vitalityDelta,
              };
              pushImpact(impact.x, impact.y, targetIsAlly, at);
            }
            // Plusieurs ticks à la fois (plusieurs DoT/HoT empilés) se lisent l'un après
            // l'autre, jamais tous d'un coup au même pixel — voir TICK_IMPACT_STAGGER_MS.
            if (i < tickImpacts.length - 1) await wait(TICK_IMPACT_STAGGER_MS);
          }
          await wait(TICK_SETTLE_MS);
          continue;
        }

        if (event.kind === 'Move' && event.path.length > 0) {
          // Le chemin serveur ne contient que les cases foulées, origine exclue. Celle-ci vient
          // du relevé pris avant l'appel ; à défaut, on part de la première case du trajet —
          // une case d'écart, jamais un saut à travers le plateau.
          const origin = pinned.value[event.actorId] ?? event.path[0];

          const path = [
            { x: origin.x, y: origin.y },
            ...event.path.map((s) => ({ x: s.x, y: s.y })),
          ];

          // O-014 + O-018: Utiliser un timing dynamique basé sur la longueur du chemin et le type (allié/ennemi)
          const stepMs = dynamicStepDurationMs(path.length - 1, !actorIsAlly);
          walk.value = {
            combatantId: event.actorId,
            path,
            startedAt: now(),
            isEnemy: !actorIsAlly,
          };
          await wait((path.length - 1) * stepMs); // -1 car origin n'est pas comptée dans le chemin
          walk.value = null;

          // Arrivé : la figure reste où le serveur l'a mise.
          const { [event.actorId]: _removed, ...rest } = pinned.value;
          pinned.value = rest;
          continue;
        }

        if (event.kind === 'Skill' || event.kind === 'Item') {
          const at = now();

          actionBanner.value = event.skillName
            ? `${event.actorName} — ${event.skillName}`
            : event.actorName;

          const actorPos = pinned.value[event.actorId] ?? event.path[0] ?? null;

          for (const impact of event.impacts) {
            const targetIsAlly = allyIds.has(impact.combatantId);
            const guardAbsorbed = impact.guardAbsorbed ?? 0;
            if (guardAbsorbed > 0) pushGuardFloat(impact.x, impact.y, guardAbsorbed, at);
            // Un coup entièrement absorbé laisse la vitalité intacte — un « −0 » ne dirait rien
            // que le chiffre de garde ci-dessus ne dise déjà mieux.
            if (impact.missed || impact.vitalityDelta !== 0) {
              pushFloat(impact.x, impact.y, impact.vitalityDelta, targetIsAlly, at, impact.missed);
            }
            if (impact.effectiveness) {
              pushEffectivenessFloat(impact.x, impact.y, impact.effectiveness, at);
            }
            // La barre ne s'effondre qu'ici, au moment exact où ce coup précis atterrit —
            // jamais avant, quel que soit l'ordre dans lequel `combat.value` a déjà tout reçu.
            if (!impact.missed) {
              displayVitals.value = {
                ...displayVitals.value,
                [impact.combatantId]:
                  (displayVitals.value[impact.combatantId] ?? 0) - impact.vitalityDelta,
              };
            }
            // Un coup manqué n'a rien percuté : pas d'onde d'impact, seulement la mention.
            if (!impact.missed) pushImpact(impact.x, impact.y, targetIsAlly, at);
          }

          if (
            event.kind === 'Skill'
              &&
            event.skillKey
              && typeof event.targetX === 'number'
              && typeof event.targetY === 'number'
          ) {
            pendingSorts.value = [
              ...pendingSorts.value,
              {
                skillKey: event.skillKey,
                x: event.targetX,
                y: event.targetY,
                casterX: actorPos?.x ?? event.targetX,
                casterY: actorPos?.y ?? event.targetY,
              },
            ];
          }

          // O-018: Temps de settle plus long pour les ennemis. L'allié gardait la moitié du
          // temps de base (le joueur "sait déjà" ce qu'il vient de faire) — mais divisé par
          // deux plutôt que simplement égal au temps de base, l'écart avec l'ennemi (×1.5)
          // atteignait un facteur 3 : le coup du héros se lisait bien plus vite que celui d'un
          // ennemi même après le ralenti global (PACE). Retour au temps de base, sans réduction.
          const settleMs = actorIsAlly ? SETTLE_MS : SETTLE_MS * ENEMY_SETTLE_MULTIPLIER;
          await wait(settleMs);
          actionBanner.value = null;
        }
      }
    } finally {
      walk.value = null;
      pinned.value = {};
      // Relâche le gel : les états actifs affichés redeviennent la vérité reçue du serveur,
      // maintenant que la mise en scène qui y menait a fini de jouer.
      displayStatusEffects.value = {};
      actionBanner.value = null;
      telegraph.value = null;
      isPlaying.value = false;
      // O-017: Réinitialiser l'état de transition
      isTransitioning.value = false;
      transitionPhase.value = null;
    }
  }

  /**
   * Épingle les positions ET les états actifs de départ AVANT que le nouvel état ne soit
   * appliqué — voir la doc de `displayStatusEffects` pour pourquoi ces derniers ne peuvent pas
   * se reconstruire coup par coup comme la vitalité.
   */
  function pinBefore(state: TacticalCombatRuntimeDto | null) {
    if (!state) return;

    const pins: Record<string, { x: number; y: number }> = {};
    const statusPins: Record<string, CombatantStatusEffectDto[]> = {};
    for (const unit of [...state.allies, ...state.enemies]) {
      pins[unit.combatant.id] = { x: unit.x, y: unit.y };
      statusPins[unit.combatant.id] = unit.combatant.statusEffects ?? [];
    }

    pinned.value = pins;
    displayStatusEffects.value = statusPins;
  }

  function consumeSorts(): PendingSort[] {
    const sorts = pendingSorts.value;
    pendingSorts.value = [];
    return sorts;
  }

  return {
    walk,
    floats,
    impacts,
    pendingSorts,
    actionBanner,
    telegraph,
    isPlaying: computed(() => isPlaying.value),
    isTransitioning: computed(() => isTransitioning.value), // O-017
    transitionPhase: computed(() => transitionPhase.value), // O-017
    positionOf,
    vitalsOf,
    statusEffectsOf,
    pruneFloats,
    pruneImpacts,
    consumeSorts,
    play,
    pinBefore,
    reset,
    stop,
  };
}
