import type { ResolveCurrentEventResponse } from '../../runs/types/runTypes';

export type EventOutcomeDto = ResolveCurrentEventResponse['outcome'];

export type EventChoiceDto = {
  id?: string;
  choiceId?: string;
  key?: string;
  label?: string;
  title?: string;
  description?: string;
  isEnabled?: boolean;
};

export type NormalizedEventChoice = {
  id: string;
  label: string;
  description: string;
  isEnabled: boolean;
};

export function getOutcomeChoices(outcome: EventOutcomeDto): NormalizedEventChoice[] {
  const rawChoices = Array.isArray(outcome.choices)
    ? outcome.choices
    : [];

  return rawChoices
    .map((choice): NormalizedEventChoice | null => {
      if (
        typeof choice !== 'object' ||
        choice === null
      ) {
        return null;
      }

      const candidate = choice as EventChoiceDto;

      const id = candidate.id
        ?? candidate.choiceId
        ?? candidate.key
        ?? '';

      if (!id) {
        return null;
      }

      return {
        id,
        label:
          candidate.label
          ?? candidate.title
          ?? candidate.key
          ?? 'Choix sans nom',
        description:
          candidate.description
          ?? 'Aucune description disponible pour ce choix.',
        isEnabled: candidate.isEnabled ?? true,
      };
    })
    .filter((choice): choice is NormalizedEventChoice => choice !== null);
}

export function isCombatOutcome(resolutionKind: string): boolean {
  return resolutionKind === 'CombatStarted' ||
    resolutionKind === 'EliteEncounterStarted' ||
    resolutionKind === 'RoomBossEncounterStarted' ||
    resolutionKind === 'FinalBossEncounterStarted';
}

export function isRewardLikeOutcome(resolutionKind: string): boolean {
  return resolutionKind === 'RewardGranted' ||
    resolutionKind === 'RareEventResolved' ||
    resolutionKind === 'TomePageUnlocked';
}

export function isChoiceOutcome(outcome: EventOutcomeDto): boolean {
  return outcome.requiresPlayerChoice || getOutcomeChoices(outcome).length > 0;
}

export function getOutcomeFamily(resolutionKind: string): string {
  switch (resolutionKind) {
    case 'RewardGranted':
      return 'Récompense';

    case 'RestResolved':
      return 'Repos';

    case 'NarrativeFragmentRevealed':
      return 'Narration';

    case 'PalaceLawOffered':
      return 'Loi du Palais';

    case 'TradeOffered':
      return 'Marchand';

    case 'CurseOffered':
      return 'Malédiction';

    case 'RareEventResolved':
      return 'Événement rare';

    case 'TomePageUnlocked':
      return 'Tome';

    case 'CombatStarted':
    case 'EliteEncounterStarted':
    case 'RoomBossEncounterStarted':
    case 'FinalBossEncounterStarted':
      return 'Combat';

    default:
      return 'Événement';
  }
}