import type { PlayerCharacterStatsView, PlayerStatKind } from '../types/playerTypes';

export const statLabels: Record<PlayerStatKind, string> = {
  MaxVitality:   'Vitalité max',
  AttackPower:   'Attaque',
  Defense:       'Défense',
  StartingGuard: 'Garde initiale',
  Speed:         'Vitesse',
  Initiative:    'Initiative',
  Focus:         'Focus',
  Mana:          'Mana',
  MagicAttack:   'Attaque magique',
  MagicDefense:  'Défense magique',
};

export const statOrder = Object.keys(statLabels) as PlayerStatKind[];

export const statValueKeys: Record<PlayerStatKind, keyof PlayerCharacterStatsView> = {
  MaxVitality:   'maxVitality',
  AttackPower:   'attackPower',
  Defense:       'defense',
  StartingGuard: 'startingGuard',
  Speed:         'speed',
  Initiative:    'initiative',
  Focus:         'focus',
  Mana:          'mana',
  MagicAttack:   'magicAttack',
  MagicDefense:  'magicDefense',
};

export const statDescriptions: Record<PlayerStatKind, string> = {
  MaxVitality:   "Points de vie maximum. Détermine combien de dégâts vous pouvez encaisser avant d'être terrassé.",
  AttackPower:   'Puissance brute des attaques physiques. Augmente les dégâts infligés par vos sorts de catégorie Physique.',
  Defense:       'Réduit les dégâts physiques subis lors des attaques ennemies.',
  StartingGuard: "Garde initiale accordée en début de combat, absorbée avant que les dégâts n'atteignent vos points de vie.",
  Speed:         "Détermine votre place dans l'ordre d'initiative de chaque round.",
  Initiative:    'Avantage de départ qui départage les combattants de même vitesse.',
  Focus:         'Augmente vos chances de coup critique.',
  Mana:          'Réserve d\'énergie consommée pour lancer la plupart des sorts.',
  MagicAttack:   'Puissance des sorts de catégorie Magique. Symétrique à Attaque, mais pour les dégâts magiques uniquement.',
  MagicDefense:  'Réduit les dégâts magiques subis, symétriquement à Défense pour les dégâts physiques.',
};

export function statValue(stats: PlayerCharacterStatsView | undefined, stat: PlayerStatKind): number {
  return stats ? stats[statValueKeys[stat]] ?? 0 : 0;
}

// Mirrors PlayerCharacterStatBlock.WithIncrementedStat (services/player) — Vitality
// and Mana grant a bigger jump per point than the other stats.
export const statPointIncrements: Record<PlayerStatKind, number> = {
  MaxVitality:   10,
  AttackPower:   1,
  Defense:       1,
  StartingGuard: 1,
  Speed:         1,
  Initiative:    1,
  Focus:         1,
  Mana:          5,
  MagicAttack:   1,
  MagicDefense:  1,
};

// Purely cosmetic per-stat ceiling used to normalize the radar diagram's 12 axes to
// a common 0-1 scale (each stat has a wildly different natural range). Not a game
// rule and not enforced anywhere — just a reasonable "comfortably filled" reference
// point, tunable independently of balance.
export const statRadarMax: Record<PlayerStatKind, number> = {
  MaxVitality:   400,
  AttackPower:   50,
  Defense:       40,
  StartingGuard: 30,
  Speed:         40,
  Initiative:    40,
  Focus:         30,
  Mana:          150,
  MagicAttack:   40,
  MagicDefense:  40,
};
