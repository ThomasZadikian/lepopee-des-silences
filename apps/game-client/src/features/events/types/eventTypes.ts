import type {
    ResolveCurrentEventResponse,
    RunDto,
} from '../../runs/types/runTypes';
import type { NarrativeFragmentDto, NpcDialogueViewDto } from '../../runs/types/runTypes';

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

export type ChooseCurrentEventOptionRequest = {
  choiceId?: string;
  optionId?: string;
  eventChoiceId?: string;
};

export type CurrentEventChoiceResultDto = {
  choiceId?: string;
  selectedChoiceId?: string;
  title?: string;
  description?: string;
  outcomeKind?: string;
  state?: string;
  message?: string;
  narrativeFragments?: NarrativeFragmentDto[];
};

export type ChooseCurrentEventOptionResponse =
  | RunDto
  | {
      run: RunDto;
      choiceResult?: CurrentEventChoiceResultDto;
      result?: CurrentEventChoiceResultDto;
    }
  | {
      choiceResult: CurrentEventChoiceResultDto;
      run?: RunDto;
    }
  | {
      result: CurrentEventChoiceResultDto;
      run?: RunDto;
    };

export function unwrapRunFromEventChoiceResponse(
  response: ChooseCurrentEventOptionResponse,
): RunDto | null {
  if (
    typeof response === 'object' &&
    response !== null &&
    'run' in response &&
    response.run
  ) {
    return response.run;
  }

  if (
    typeof response === 'object' &&
    response !== null &&
    'id' in response &&
    'currentRoom' in response
  ) {
    return response as RunDto;
  }

  return null;
}

export function unwrapChoiceResultFromEventChoiceResponse(
  response: ChooseCurrentEventOptionResponse,
): CurrentEventChoiceResultDto | null {
  if (
    typeof response === 'object' &&
    response !== null &&
    'choiceResult' in response &&
    response.choiceResult
  ) {
    return response.choiceResult;
  }

  if (
    typeof response === 'object' &&
    response !== null &&
    'result' in response &&
    response.result
  ) {
    return response.result;
  }

  return null;
}

export function unwrapNpcDialogueFromEventChoiceResponse(
  response: ChooseCurrentEventOptionResponse,
): NpcDialogueViewDto | null {
  if (typeof response === 'object' && response !== null && 'npcDialogue' in response) {
    return (response as { npcDialogue?: NpcDialogueViewDto | null }).npcDialogue ?? null;
  }
  return null;
}

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