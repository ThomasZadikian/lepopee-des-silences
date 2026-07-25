import type { NodeDto } from '../../runs/types/runTypes';

// ── Node tile tone: color-codes a node's tile by what it means, not just that it
// holds a node — combat sits on blood, treasure/commerce/decrees on gold, presences
// on frost, respite on sap. Makes the board read at a glance, FFT-style.
export const NODE_TILE_TONE: Record<string, string> = {
  Combat: 'blood', Elite: 'blood', Rare: 'blood', RoomBoss: 'blood', FinalBoss: 'blood',
  Curse: 'blood',
  Item: 'gold', Merchant: 'gold', Law: 'gold',
  Npc: 'frost', Memory: 'frost',
  Rest: 'sap',
};

export const SIGIL_KIND_BY_NODE_TYPE: Record<string, string> = {
  Combat: 'combat',
  Elite: 'elite',
  Rare: 'rare',
  RoomBoss: 'boss',
  FinalBoss: 'boss',
  Item: 'objet',
  Npc: 'pnj',
  Rest: 'repos',
  Merchant: 'marchand',
  Law: 'loi',
  Curse: 'malediction',
};

/**
 * The painted 3D-ish prop that stands on a node's tile, per node type. These read from across
 * the board in a way the small flat sigil never could — a campfire, a hooded figure, a
 * suspended shard — so an objective is legible before you are on top of it.
 *
 * Only event nodes that represent *something physically there* get one. Combat/Elite/Rare and
 * the boss deliberately do not: their tile already carries a danger tell and a glow, and a
 * decorative statue on an ambush would give it away.
 */
export const PROP_KIND_BY_NODE_TYPE: Record<string, 'npc' | 'star' | 'campfire'> = {
  Npc: 'npc',
  Merchant: 'npc',
  Rest: 'campfire',
  Item: 'star',
  Memory: 'star',
  Law: 'star',
};

// Short label — used both for the hover tooltip (type only, as requested) and as the
// side panel's kicker.
export const NODE_TYPE_LABEL: Record<string, string> = {
  Combat: 'Combat',
  Elite: 'Élite',
  Rare: 'Rencontre rare',
  RoomBoss: 'Gardien de salle',
  FinalBoss: 'Confrontation finale',
  Item: 'Objet',
  Npc: 'Présence',
  Memory: 'Souvenir',
  Rest: 'Repos',
  Merchant: 'Marchand',
  Law: 'Décret du Palais',
  Curse: 'Malédiction',
};

// Fuller flavor text for the side panel's description.
export const NODE_TYPE_DESCRIPTION: Record<string, string> = {
  Combat: 'Un affrontement direct vous attend dans les profondeurs du Palais.',
  Elite: "Un adversaire d'élite barre le passage. La victoire sera coûteuse.",
  Rare: "Une présence rare s'est manifestée — imprévisible et potentiellement précieuse.",
  RoomBoss: 'Le Gardien de cette salle attend. Aucun passage sans combat.',
  FinalBoss: 'La présence finale du Palais. Tout converge ici.',
  Rest: 'Un refuge temporaire. Reprendre souffle avant de continuer.',
  Item: 'Un objet a été laissé ici. Son origine reste obscure.',
  Npc: "Quelqu'un — ou quelque chose — souhaite vous parler.",
  Merchant: "Un marchand propose ses services dans l'ombre du Palais.",
  Law: 'Une règle du Palais inscrite dans ses murs. La lire vous changera.',
  Curse: 'Une malédiction latente. Y toucher a un coût.',
  Memory: "Un écho du passé. Ce souvenir n'est pas le vôtre.",
};

export const RISK_TIER_DISPLAY: Record<string, { text: string; cls: string }> = {
  Calme: { text: 'Calme', cls: 'tgrid__risk--low' },
  Tendu: { text: 'Tendu', cls: 'tgrid__risk--moderate' },
  Dangereux: { text: 'Dangereux', cls: 'tgrid__risk--high' },
  Perilleux: { text: 'Périlleux', cls: 'tgrid__risk--critical' },
  Fatal: { text: 'Fatal', cls: 'tgrid__risk--fatal' },
};

export function useNodePresentation() {
  function nodeTileToneClass(node: NodeDto | null): string {
    if (!node) return '';
    const tone = NODE_TILE_TONE[node.type];
    return tone ? `tgrid__cell--tone-${tone}` : '';
  }

  function sigilKindFor(node: NodeDto): string {
    return SIGIL_KIND_BY_NODE_TYPE[node.type] ?? 'objet';
  }

  function nodeTypeLabel(node: NodeDto): string {
    return NODE_TYPE_LABEL[node.type] ?? node.type;
  }

  function nodeTypeDescription(node: NodeDto): string {
    return NODE_TYPE_DESCRIPTION[node.type] ?? 'Un nœud inconnu du Palais.';
  }

  return {
    nodeTileToneClass,
    sigilKindFor,
    nodeTypeLabel,
    nodeTypeDescription,
  };
}
