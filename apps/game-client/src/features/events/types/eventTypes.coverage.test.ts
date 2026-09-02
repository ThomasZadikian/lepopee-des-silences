import { describe, expect, it } from 'vitest';
import {
  getOutcomeChoices,
  getOutcomeFamily,
  isChoiceOutcome,
  isCombatOutcome,
  isRewardLikeOutcome,
  unwrapChoiceResultFromEventChoiceResponse,
  unwrapNpcDialogueFromEventChoiceResponse,
  unwrapRunFromEventChoiceResponse,
} from './eventTypes';

const run = { id: 'run-1', currentRoom: {} } as any;

describe('event response normalizers coverage margin', () => {
  it('unwraps nested and direct runs and rejects all invalid shapes', () => {
    expect(unwrapRunFromEventChoiceResponse({ run } as any)).toBe(run);
    expect(unwrapRunFromEventChoiceResponse({ run: null } as any)).toBeNull();
    expect(unwrapRunFromEventChoiceResponse(run)).toBe(run);
    expect(unwrapRunFromEventChoiceResponse({ id: 'run-1' } as any)).toBeNull();
    expect(unwrapRunFromEventChoiceResponse({ currentRoom: {} } as any)).toBeNull();
    expect(unwrapRunFromEventChoiceResponse(null as any)).toBeNull();
    expect(unwrapRunFromEventChoiceResponse('invalid' as any)).toBeNull();
  });

  it('unwraps every supported choice result shape', () => {
    const choiceResult = { choiceId: 'choice-a' };
    const result = { choiceId: 'choice-b' };

    expect(unwrapChoiceResultFromEventChoiceResponse({ choiceResult, run } as any)).toBe(choiceResult);
    expect(unwrapChoiceResultFromEventChoiceResponse({ choiceResult: null, result } as any)).toBe(result);
    expect(unwrapChoiceResultFromEventChoiceResponse({ result, run } as any)).toBe(result);
    expect(unwrapChoiceResultFromEventChoiceResponse({ result: null } as any)).toBeNull();
    expect(unwrapChoiceResultFromEventChoiceResponse(null as any)).toBeNull();
    expect(unwrapChoiceResultFromEventChoiceResponse('invalid' as any)).toBeNull();
  });

  it('unwraps optional npc dialogue only when the property exists', () => {
    const dialogue = { nodeKey: 'node-a' } as any;
    expect(unwrapNpcDialogueFromEventChoiceResponse({ npcDialogue: dialogue } as any)).toBe(dialogue);
    expect(unwrapNpcDialogueFromEventChoiceResponse({ npcDialogue: null } as any)).toBeNull();
    expect(unwrapNpcDialogueFromEventChoiceResponse({} as any)).toBeNull();
    expect(unwrapNpcDialogueFromEventChoiceResponse(null as any)).toBeNull();
  });
});

describe('event choice normalization coverage margin', () => {
  it('returns no choices when the outcome payload is not an array', () => {
    expect(getOutcomeChoices({ choices: undefined } as any)).toEqual([]);
    expect(getOutcomeChoices({ choices: 'invalid' } as any)).toEqual([]);
  });

  it('filters invalid entries and exercises every id and label fallback', () => {
    const choices = getOutcomeChoices({
      choices: [
        null,
        'invalid',
        {},
        { id: 'id-a', label: 'Label A', description: 'Description A', isEnabled: false },
        { choiceId: 'id-b', title: 'Title B' },
        { key: 'id-c' },
        { id: 'id-d' },
      ],
    } as any);

    expect(choices).toEqual([
      { id: 'id-a', label: 'Label A', description: 'Description A', isEnabled: false },
      {
        id: 'id-b',
        label: 'Title B',
        description: 'Aucune description disponible pour ce choix.',
        isEnabled: true,
      },
      {
        id: 'id-c',
        label: 'id-c',
        description: 'Aucune description disponible pour ce choix.',
        isEnabled: true,
      },
      {
        id: 'id-d',
        label: 'Choix sans nom',
        description: 'Aucune description disponible pour ce choix.',
        isEnabled: true,
      },
    ]);
  });
});

describe('event outcome classifiers coverage margin', () => {
  it.each([
    'CombatStarted',
    'EliteEncounterStarted',
    'RoomBossEncounterStarted',
    'FinalBossEncounterStarted',
  ])('recognizes combat outcome %s', (kind) => {
    expect(isCombatOutcome(kind)).toBe(true);
  });

  it('rejects non-combat outcomes', () => {
    expect(isCombatOutcome('RewardGranted')).toBe(false);
  });

  it.each(['RewardGranted', 'RareEventResolved', 'TomePageUnlocked'])(
    'recognizes reward-like outcome %s',
    (kind) => expect(isRewardLikeOutcome(kind)).toBe(true),
  );

  it('rejects non-reward outcomes', () => {
    expect(isRewardLikeOutcome('RestResolved')).toBe(false);
  });

  it('recognizes explicit and implicit choice outcomes', () => {
    expect(isChoiceOutcome({ requiresPlayerChoice: true, choices: [] } as any)).toBe(true);
    expect(isChoiceOutcome({ requiresPlayerChoice: false, choices: [{ id: 'a' }] } as any)).toBe(true);
    expect(isChoiceOutcome({ requiresPlayerChoice: false, choices: [] } as any)).toBe(false);
  });

  it.each([
    ['RewardGranted', 'Récompense'],
    ['RestResolved', 'Repos'],
    ['NarrativeFragmentRevealed', 'Narration'],
    ['PalaceLawOffered', 'Loi du Palais'],
    ['TradeOffered', 'Marchand'],
    ['CurseOffered', 'Malédiction'],
    ['RareEventResolved', 'Événement rare'],
    ['TomePageUnlocked', 'Tome'],
    ['CombatStarted', 'Combat'],
    ['EliteEncounterStarted', 'Combat'],
    ['RoomBossEncounterStarted', 'Combat'],
    ['FinalBossEncounterStarted', 'Combat'],
    ['Unknown', 'Événement'],
  ])('maps %s to %s', (kind, expected) => {
    expect(getOutcomeFamily(kind)).toBe(expected);
  });
});
