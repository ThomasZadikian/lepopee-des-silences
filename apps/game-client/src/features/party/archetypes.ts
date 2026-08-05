/**
 * Mirrors the backend's CharacterArchetypeProvider (services/game-engine/.../Players/
 * CharacterArchetypeProvider.cs) — a lightweight beta tuning table, promotable to
 * catalog/seed later. Used client-side only to grey out incompatible skills in the
 * Grimoire before submit; the server (SkillArchetypeGate) is the actual authority.
 */
export const ADAPTIVE_ARCHETYPE = 'Adaptive';

const ARCHETYPES_BY_DEFINITION_KEY: Record<string, string> = {
  'character.player.self': ADAPTIVE_ARCHETYPE,
  'character.thomas': 'Tank',
  'character.mane': 'GlassCannon',
  'character.mina': 'Support',
  'character.elise': 'Hybrid',
  'character.john': 'Opportunist',
};

const ARCHETYPE_LABELS: Record<string, string> = {
  Adaptive: 'Adaptable',
  Tank: 'Tank',
  GlassCannon: 'Glass cannon',
  Support: 'Soutien',
  Hybrid: 'Hybride',
  Opportunist: 'Opportuniste',
};

export function characterArchetype(definitionKey: string | undefined | null): string {
  if (!definitionKey) return ADAPTIVE_ARCHETYPE;
  return ARCHETYPES_BY_DEFINITION_KEY[definitionKey] ?? ADAPTIVE_ARCHETYPE;
}

export function archetypeLabel(archetype: string): string {
  return ARCHETYPE_LABELS[archetype] ?? archetype;
}

/** True when a skill's AllowedArchetypes list excludes this character's archetype. */
export function isArchetypeIncompatible(
  allowedArchetypes: readonly string[] | undefined | null,
  characterDefinitionKey: string | undefined | null,
): boolean {
  if (!allowedArchetypes || allowedArchetypes.length === 0) return false;
  const archetype = characterArchetype(characterDefinitionKey);
  if (archetype === ADAPTIVE_ARCHETYPE) return false;
  return !allowedArchetypes.some((a) => a.toLowerCase() === archetype.toLowerCase());
}
