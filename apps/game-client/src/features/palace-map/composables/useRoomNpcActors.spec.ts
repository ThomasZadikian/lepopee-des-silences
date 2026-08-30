import { describe, expect, it } from 'vitest';
import { roomNpcActorFor } from './useRoomNpcActors';

describe('roomNpcActorFor', () => {
  it('resolves a bare catalog key (no variant suffix) to variant 0', () => {
    expect(roomNpcActorFor('npc.majordome')).toEqual({ figureId: 'majordome', variant: 0 });
    expect(roomNpcActorFor('npc.chien')).toEqual({ figureId: 'chien', variant: 0 });
  });

  it('splits the salle-casting.js "figureId#variant" convention', () => {
    expect(roomNpcActorFor('npc.habitant#3')).toEqual({ figureId: 'habitant', variant: 3 });
    expect(roomNpcActorFor('npc.emotion#5')).toEqual({ figureId: 'emotion', variant: 5 });
    expect(roomNpcActorFor('npc.chat#0')).toEqual({ figureId: 'chat', variant: 0 });
  });

  it('resolves a hyphenated figure id with no "npc." prefix stripped needed beyond the domain one', () => {
    expect(roomNpcActorFor('npc.veilleur-tapis')).toEqual({ figureId: 'veilleur-tapis', variant: 0 });
  });

  it('returns null for a catalog key the bestiaire does not cover yet', () => {
    expect(roomNpcActorFor('npc.not-yet-authored')).toBeNull();
  });

  it('is tolerant of a key with no "npc." prefix at all', () => {
    expect(roomNpcActorFor('majordome')).toEqual({ figureId: 'majordome', variant: 0 });
  });
});
