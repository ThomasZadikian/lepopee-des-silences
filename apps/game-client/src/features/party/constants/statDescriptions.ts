import type { PlayerCharacterStatsView, PlayerStatKind } from '../types/playerTypes';

export const statLabels: Record<PlayerStatKind, string> = {
  MaxVitality:   'Vitalité max',
  AttackPower:   'Attaque',
  Defense:       'Défense',
  StartingGuard: 'Garde initiale',
  Speed:         'Vitesse',
  Initiative:    'Initiative',
  Recovery:      'Récupération',
  Focus:         'Focus',
  Mana:          'PP (Mana)',
  Charge:        'Charge',
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
  Recovery:      'recovery',
  Focus:         'focus',
  Mana:          'mana',
  Charge:        'charge',
  MagicAttack:   'magicAttack',
  MagicDefense:  'magicDefense',
};

export const statDescriptions: Record<PlayerStatKind, string> = {
  MaxVitality:   "Points de vie maximum. Détermine combien de dégâts vous pouvez encaisser avant d'être terrassé.",
  AttackPower:   'Puissance brute des attaques physiques. Augmente les dégâts infligés par vos sorts de catégorie Physique.',
  Defense:       'Réduit les dégâts physiques subis lors des attaques ennemies.',
  StartingGuard: "Garde initiale accordée en début de combat, absorbée avant que les dégâts n'atteignent vos points de vie.",
  Speed:         'Vitesse de remplissage de la jauge ATB : plus elle est élevée, plus vous agissez souvent.',
  Initiative:    'Avantage de départ sur la jauge ATB en début de combat — détermine qui joue en premier.',
  Recovery:      'Réduit le temps de récupération après une action, en particulier pour les sorts puissants.',
  Focus:         'Augmente vos chances de coup critique.',
  Mana:          'Réserve d\'énergie consommée pour lancer la plupart des sorts.',
  Charge:        'Réserve consommée par certains sorts puissants nécessitant une charge préalable.',
  MagicAttack:   'Puissance des sorts de catégorie Magique. Symétrique à Attaque, mais pour les dégâts magiques uniquement.',
  MagicDefense:  'Réduit les dégâts magiques subis, symétriquement à Défense pour les dégâts physiques.',
};

export function statValue(stats: PlayerCharacterStatsView | undefined, stat: PlayerStatKind): number {
  return stats ? stats[statValueKeys[stat]] ?? 0 : 0;
}

// Mirrors PlayerCharacterStatBlock.WithIncrementedStat (services/player) — Vitality
// and PP/Mana grant a bigger jump per point than the other stats.
export const statPointIncrements: Record<PlayerStatKind, number> = {
  MaxVitality:   10,
  AttackPower:   1,
  Defense:       1,
  StartingGuard: 1,
  Speed:         1,
  Initiative:    1,
  Recovery:      1,
  Focus:         1,
  Mana:          5,
  Charge:        1,
  MagicAttack:   1,
  MagicDefense:  1,
};
