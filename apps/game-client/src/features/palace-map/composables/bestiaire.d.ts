/**
 * Typage du bestiaire peint fourni par le handoff Claude Design.
 *
 * Le `.js` n'est jamais type-checké (`tsconfig.app.json` n'a pas `allowJs` et n'inclut que
 * `src/**\/*.{ts,tsx,vue}`) : ce fichier porte donc seul le contrat, posé à côté du module pour
 * que TypeScript résolve `./bestiaire` nativement. Même convention que `tilecraft.d.ts`.
 */

export const TOKEN: Record<string, string>;

export const ROLES: Record<string, { label: string; note: string }>;

export const ROSTER_IDS: string[];

export const PROPOSITION_IDS: string[];

export type RosterEntry = {
  name: string;
  side: 'ally' | 'enemy';
  role: string;
  family: string;
  rarity: string;
  quote: string;
  catalogKey: string | null;
  variants: number;
};

export const ROSTER: Record<string, RosterEntry>;

export const ROSTER_PROPOSITIONS: Record<string, RosterEntry>;

export type Family = {
  label: string;
  note: string;
  members: string[];
};

export const FAMILIES: Record<string, Family>;

export function bakeCombatant(id: string, variant?: number): HTMLCanvasElement;

export function getCombatantSprite(id: string, variant?: number): HTMLCanvasElement;

export function combatantInfo(id: string): RosterEntry | null;
