import { describe, expect, it } from 'vitest';
import { propKindFor, useNodePresentation } from './useNodePresentation';
import type { NodeDto } from '../../runs/types/runTypes';

function makeNode(type: string, overrides: Partial<NodeDto> = {}): NodeDto {
  return {
    id: 'node-1',
    type,
    row: 0,
    lane: 0,
    riskLevel: 50,
    rewardProfile: 'combat-common',
    parentNodeIds: [],
    state: 'Available',
    isBoss: false,
    isInitial: false,
    hasChosenEventOption: false,
    ...overrides,
  };
}

const NODE_TYPES = [
  'Combat', 'Elite', 'Rare', 'RoomBoss', 'FinalBoss',
  'Item', 'Npc', 'Rest', 'Merchant', 'Law', 'Curse', 'Memory',
];

describe('useNodePresentation', () => {
  const { nodeTileToneClass, sigilKindFor, nodeTypeLabel, nodeTypeDescription } = useNodePresentation();

  it('returns an empty tone class for a null node', () => {
    expect(nodeTileToneClass(null)).toBe('');
  });

  it.each(NODE_TYPES)('has a sigil kind, label, and description for node type %s', (type) => {
    const node = makeNode(type);
    expect(sigilKindFor(node)).toBeTruthy();
    expect(nodeTypeLabel(node)).toBeTruthy();
    expect(nodeTypeDescription(node)).toBeTruthy();
  });

  it('falls back to "objet" sigil kind for an unknown node type', () => {
    expect(sigilKindFor(makeNode('SomethingNew'))).toBe('objet');
  });

  it('falls back to the raw type string as the label for an unknown node type', () => {
    expect(nodeTypeLabel(makeNode('SomethingNew'))).toBe('SomethingNew');
  });

  it('falls back to a generic description for an unknown node type', () => {
    expect(nodeTypeDescription(makeNode('SomethingNew'))).toBe('Un nœud inconnu du Palais.');
  });

  it('assigns the blood tone to every combat-flavored node type', () => {
    for (const type of ['Combat', 'Elite', 'Rare', 'RoomBoss', 'FinalBoss', 'Curse']) {
      expect(nodeTileToneClass(makeNode(type))).toBe('tgrid__cell--tone-blood');
    }
  });

  it('assigns the gold tone to commerce/decree node types', () => {
    for (const type of ['Item', 'Merchant', 'Law']) {
      expect(nodeTileToneClass(makeNode(type))).toBe('tgrid__cell--tone-gold');
    }
  });

  it('assigns the frost tone to presence node types', () => {
    for (const type of ['Npc', 'Memory']) {
      expect(nodeTileToneClass(makeNode(type))).toBe('tgrid__cell--tone-frost');
    }
  });

  it('assigns the sap tone to Rest', () => {
    expect(nodeTileToneClass(makeNode('Rest'))).toBe('tgrid__cell--tone-sap');
  });
});

describe('propKindFor', () => {
  it('gives every authored node type scenery of its own', () => {
    // The merchant no longer borrows the hooded figure, and the curse/enemy/boss types have
    // stopped being bare tiles — the v2 asset set added one prop per type.
    expect(propKindFor(makeNode('Rest'))).toBe('campfire');
    expect(propKindFor(makeNode('Npc'))).toBe('npc');
    expect(propKindFor(makeNode('Merchant'))).toBe('merchant');
    expect(propKindFor(makeNode('Item'))).toBe('star');
    expect(propKindFor(makeNode('Curse'))).toBe('curse');
    expect(propKindFor(makeNode('Combat'))).toBe('monster');
  });

  it('gives Elite and Rare the horned beast rather than the plain one', () => {
    expect(propKindFor(makeNode('Elite'))).toBe('elite');
    expect(propKindFor(makeNode('Rare'))).toBe('elite');
  });

  it('gives the boss its own silhouette — it is what the room is for', () => {
    // Reverses an earlier rule of ours. The boss tile used to be left bare on the grounds that
    // its glow was enough; the design's boss prop is built to read from across the board, which
    // is the better answer for the room's objective.
    expect(propKindFor(makeNode('RoomBoss', { isBoss: true }))).toBe('boss');
    expect(propKindFor(makeNode('FinalBoss', { isBoss: true }))).toBe('boss');
  });

  it('falls back to the beast for an unauthored type that fires on contact', () => {
    expect(propKindFor(makeNode('SomeFutureType', { contactBehavior: 'TriggerOnEnter' })))
      .toBe('monster');
  });

  it('leaves an unauthored, non-contact type bare rather than guessing', () => {
    expect(propKindFor(makeNode('SomeFutureType'))).toBeNull();
  });
});
