import {
  ROSTER,
  ROSTER_IDS,
  getCombatantSprite,
} from '../../palace-map/composables/bestiaire';

import type { PropKind } from '../../palace-map/composables/useTerrainSprites';

/**
 * Fait le pont entre les clés de combattant du serveur et les figures peintes du bestiaire.
 *
 * Les deux vocabulaires ont été écrits séparément et se rejoignent presque : le catalogue
 * nomme ses créatures `enemy.sentinelle-seuil`, le bestiaire peint les nomme
 * `sentinelle-seuil`. Retirer le préfixe suffit dans l'immense majorité des cas — le reste est
 * du rattrapage, et le rattrapage doit rester visible plutôt que silencieux.
 *
 * Optimisé pour O-012 : Lazy Loading des sprites + cache.
 */

/** Le protagoniste n'a pas de clé de catalogue : il porte la sienne, en dur côté moteur. */
const PROTAGONIST_KEY = 'player.self';
const PROTAGONIST_FIGURE = 'porteur';

/** Préfixes de domaine que le bestiaire ne connaît pas. */
const DOMAIN_PREFIXES = [
  'canon.enemy.',
  'character.',
  'enemy.',
  'companion.',
  'ally.',
  'npc.',
  'boss.',
];

const rosterIds = new Set(ROSTER_IDS);
const figureByCatalogKey = new Map(
  Object.entries(ROSTER)
    .filter((entry): entry is [string, (typeof ROSTER)[string] & { catalogKey: string }] =>
      typeof entry[1].catalogKey === 'string' && entry[1].catalogKey.length > 0)
    .map(([figureId, entry]) => [entry.catalogKey, figureId]),
);

/** Clés déjà signalées, pour ne pas noyer la console à soixante images par seconde. */
const warned = new Set<string>();

/** Cache des sprites déjà chargés (O-012). */
const spriteCache = new Map<string, HTMLCanvasElement | null>();

/**
 * Normalise un libellé en identifiant candidat : minuscules, accents retirés, tout ce qui
 * n'est pas alphanumérique replié en tirets. « Sentinelle du Seuil » → `sentinelle-du-seuil`.
 */
function slugify(label: string): string {
  return label
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/** Les mots-outils français, qui séparent les deux vocabulaires plus souvent qu'ils n'aident. */
const FILLER = /-(du|de|des|la|le|les|d|l|au|aux)-/g;

/**
 * L'identifiant de bestiaire correspondant à un combattant, ou `null` si aucune figure ne lui
 * répond.
 *
 * Trois passes, de la plus sûre à la plus indulgente :
 *   1. la clé source, préfixe retiré — le chemin normal ;
 *   2. le nom affiché, translittéré — rattrape un catalogue et un bestiaire nommés
 *      indépendamment (`enemy.sentinelle-du-seuil` d'un côté, `sentinelle-seuil` de l'autre) ;
 *   3. le même nom débarrassé de ses mots-outils.
 *
 * Une figure absente n'est pas une erreur : le bestiaire peint ne couvre pas encore tout le
 * catalogue. L'appelant retombe alors sur une silhouette générique.
 */
export function figureIdFor(sourceKey: string, displayName: string): string | null {
  if (sourceKey === PROTAGONIST_KEY) return PROTAGONIST_FIGURE;

  const catalogFigure = figureByCatalogKey.get(sourceKey);
  if (catalogFigure) return catalogFigure;

  const stripped = DOMAIN_PREFIXES.reduce(
    (key, prefix) => (key.startsWith(prefix) ? key.slice(prefix.length) : key),
    sourceKey,
  );

  if (rosterIds.has(stripped)) return stripped;

  const fromName = slugify(displayName);
  if (rosterIds.has(fromName)) return fromName;

  const condensed = fromName.replace(FILLER, '-');
  if (rosterIds.has(condensed)) return condensed;

  // Dernier recours : les variantes du catalogue portent un suffixe que le bestiaire ne connaît
  // pas — `enemy.imperatrice-vipere` face à la figure `imperatrice`. On retire les segments par
  // la fin jusqu'à retomber sur la famille. Une variante ressemble à sa souche : lui prêter sa
  // figure est bien plus juste que la silhouette anonyme du repli.
  const segments = stripped.split('-');
  for (let keep = segments.length - 1; keep >= 1; keep -= 1) {
    const prefix = segments.slice(0, keep).join('-');
    if (rosterIds.has(prefix)) return prefix;
  }

  return null;
}

/**
 * La silhouette d'exploration qui tient lieu de figure quand le bestiaire ne couvre pas encore
 * une créature.
 *
 * Le Palais possède déjà des figures pour ses cases de rencontre : c'est ce que le joueur voit
 * sur la carte avant d'engager. Les réemployer garde le combat dans le même monde visuel,
 * là où une forme abstraite signalerait surtout que quelque chose manque.
 */
export function fallbackPropFor(archetype: string, isBoss: boolean): PropKind {
  if (isBoss) return 'boss';

  return archetype.toLowerCase() === 'elite' ? 'elite' : 'monster';
}

/**
 * Le sprite peint d'un combattant, ou `null` si le bestiaire ne le couvre pas encore.
 *
 * La variante dérive de l'identifiant du combattant plutôt que d'un compteur : deux créatures
 * de la même espèce sur le même champ de bataille doivent différer, et se ressembler encore
 * après un rechargement de partie.
 *
 * Optimisé pour O-012 : Lazy Loading + cache des sprites.
 */
export function combatantSprite(
  sourceKey: string,
  displayName: string,
  combatantId: string,
): HTMLCanvasElement | null {
  // O-012: Vérifier le cache en premier
  const cacheKey = `${sourceKey}-${combatantId}`;
  if (spriteCache.has(cacheKey)) {
    return spriteCache.get(cacheKey)!;
  }

  const figureId = figureIdFor(sourceKey, displayName);

  if (!figureId) {
    // Signalé une fois par clé : c'est la seule façon de savoir QUELLE créature manque, plutôt
    // que de le déduire d'une capture d'écran.
    if (!warned.has(sourceKey)) {
      warned.add(sourceKey);
      console.warn(
        `[bestiaire] aucune figure peinte pour « ${displayName} » (${sourceKey}) — `
        + 'repli sur la silhouette d\'exploration.',
      );
    }

    // O-012: Stocker null dans le cache pour éviter de recalculer
    spriteCache.set(cacheKey, null);
    return null;
  }

  let hash = 0;
  for (let i = 0; i < combatantId.length; i += 1) {
    hash = (hash * 31 + combatantId.charCodeAt(i)) | 0;
  }

  const sprite = getCombatantSprite(figureId, Math.abs(hash) % 3);

  // O-012: Stocker dans le cache pour les prochains appels
  spriteCache.set(cacheKey, sprite);

  return sprite;
}

/**
 * Efface le cache des sprites (utile pour les tests ou après un changement de thème).
 */
export function clearSpriteCache(): void {
  spriteCache.clear();
}

/**
 * Précharge les sprites pour une liste de combattants (utile pour éviter le lazy loading en combat).
 */
export function preloadSprites(combatants: Array<{ sourceKey: string; displayName: string; id: string }>): void {
  for (const combatant of combatants) {
    combatantSprite(combatant.sourceKey, combatant.displayName, combatant.id);
  }
}
