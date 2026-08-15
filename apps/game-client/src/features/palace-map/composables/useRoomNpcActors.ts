import { ROSTER_IDS } from './bestiaire';

const rosterIds = new Set<string>(ROSTER_IDS);

/**
 * Resolves a positioned RoomNpc's catalog key to the bestiaire figure + déclinaison that paints
 * it, following `salle-casting.js`'s own convention (see HallEntreeCasting.cs's doc comment):
 * an id like `habitant#3` or `emotion#5` names a shared figure plus its variant; no `#` means
 * variant 0 (`majordome`, `chien`). Returns `null` when the key doesn't resolve to any figure
 * yet — Catalog authoring for that NPC isn't done — so the caller can fall back to a generic
 * prop instead of drawing nothing.
 */
export function roomNpcActorFor(catalogNpcKey: string): { figureId: string; variant: number } | null {
  const withoutPrefix = catalogNpcKey.startsWith('npc.') ? catalogNpcKey.slice(4) : catalogNpcKey;
  const hashIndex = withoutPrefix.indexOf('#');
  const figureId = hashIndex === -1 ? withoutPrefix : withoutPrefix.slice(0, hashIndex);

  if (!rosterIds.has(figureId)) return null;
  if (hashIndex === -1) return { figureId, variant: 0 };

  const variant = Number(withoutPrefix.slice(hashIndex + 1));
  return { figureId, variant: Number.isFinite(variant) ? variant : 0 };
}
