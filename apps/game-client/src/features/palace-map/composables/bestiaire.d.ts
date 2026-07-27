/**
 * Typage du bestiaire peint fourni par le handoff Claude Design.
 *
 * Le `.js` n'est jamais type-checké (`tsconfig.app.json` n'a pas `allowJs` et n'inclut que
 * `src/**\/*.{ts,tsx,vue}`) : ce fichier porte donc seul le contrat, posé à côté du module pour
 * que TypeScript résolve `./bestiaire` nativement. Même convention que `tilecraft.d.ts`.
 *
 * Seule la surface réellement consommée est déclarée. Le module exporte davantage — tables de
 * rôles, propositions non canoniques, fiches lisibles — qui n'a pas de consommateur aujourd'hui.
 */

/** Palette partagée du handoff : les teintes que les figures et le décor ont en commun. */
export const TOKEN: Record<string, string>;

/** Les sept registres émotionnels, glyphe et couleur compris. */
export const REGISTRES: Record<string, { label: string; glyph: string; col: string }>;

/** Identifiants canoniques du bestiaire — kebab-case, sans préfixe de domaine. */
export const ROSTER_IDS: string[];

/**
 * Le sprite peint d'un combattant, mémoïsé.
 *
 * Cuit sur la toile haute (`PROP_SPRITE_H`) : à blitter avec l'ancre des décors, pas celle du
 * sol. Renvoie une toile vide si l'identifiant est inconnu — jamais `null`, pour qu'un
 * combattant non authoré laisse un trou visible plutôt que de faire tomber le rendu.
 */
export function getCombatantSprite(id: string, variant?: number): HTMLCanvasElement;
