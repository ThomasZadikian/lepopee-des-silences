import { ROSTER_IDS, getCombatantSprite } from '../../palace-map/composables/bestiaire';

/**
 * Fait le pont entre les clés de combattant du serveur et les figures peintes du bestiaire.
 *
 * Les deux vocabulaires ont été écrits séparément et se rejoignent presque : le catalogue
 * nomme ses créatures `enemy.sentinelle-seuil`, le bestiaire peint les nomme
 * `sentinelle-seuil`. Retirer le préfixe suffit dans l'immense majorité des cas — le reste est
 * du rattrapage, et le rattrapage doit rester visible plutôt que silencieux.
 */

/** Le protagoniste n'a pas de clé de catalogue : il porte la sienne, en dur côté moteur. */
const PROTAGONIST_KEY = 'player.self';
const PROTAGONIST_FIGURE = 'porteur';

/** Préfixes de domaine que le bestiaire ne connaît pas. */
const DOMAIN_PREFIXES = ['enemy.', 'companion.', 'ally.', 'npc.', 'boss.'];

const rosterIds = new Set(ROSTER_IDS);

/**
 * Normalise un libellé en identifiant candidat : minuscules, accents retirés, tout ce qui
 * n'est pas alphanumérique replié en tirets. « Sentinelle du Seuil » → `sentinelle-du-seuil`.
 */
function slugify(label: string): string {
  return label
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
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
 * Le sprite peint d'un combattant, ou `null` si le bestiaire ne le couvre pas encore.
 *
 * La variante dérive de l'identifiant du combattant plutôt que d'un compteur : deux créatures
 * de la même espèce sur le même champ de bataille doivent différer, et se ressembler encore
 * après un rechargement de partie.
 */
export function combatantSprite(
  sourceKey: string,
  displayName: string,
  combatantId: string,
): HTMLCanvasElement | null {
  const figureId = figureIdFor(sourceKey, displayName);
  if (!figureId) return null;

  let hash = 0;
  for (let i = 0; i < combatantId.length; i += 1) {
    hash = (hash * 31 + combatantId.charCodeAt(i)) | 0;
  }

  return getCombatantSprite(figureId, Math.abs(hash) % 3);
}
