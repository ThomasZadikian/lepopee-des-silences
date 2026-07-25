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
  it('keeps the authored prop for node types that have scenery of their own', () => {
    expect(propKindFor(makeNode('Rest'))).toBe('campfire');
    expect(propKindFor(makeNode('Npc'))).toBe('npc');
    expect(propKindFor(makeNode('Item'))).toBe('star');
  });

  it('stands a figure on an ambush — a contact node with no scenery of its own', () => {
    expect(propKindFor(makeNode('Combat', { contactBehavior: 'TriggerOnEnter' }))).toBe('npc');
  });

  it('leaves a node bare when nothing about it fires on contact', () => {
    expect(propKindFor(makeNode('Combat'))).toBeNull();
    expect(propKindFor(makeNode('Elite', { contactBehavior: 'Blocking' }))).toBeNull();
  });

  it('leaves the boss bare even on contact — its tile already carries a glow', () => {
    expect(propKindFor(makeNode('RoomBoss', { isBoss: true, contactBehavior: 'TriggerOnEnter' })))
      .toBeNull();
  });
});
