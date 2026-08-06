import { computed, ref } from 'vue';

import { emotionalRegistersApi } from './api';
import type { EmotionalRegisterDefinition } from './types';

function normalize(code: string): string {
  return code.trim().toLocaleLowerCase('fr');
}

const version = ref<string | null>(null);
const definitions = ref<EmotionalRegisterDefinition[]>([]);
const isLoading = ref(false);
const error = ref<string | null>(null);

const byCode = computed(() => new Map(
  definitions.value.map((definition) => [normalize(definition.code), definition]),
));

function definitionOf(code: string | null | undefined): EmotionalRegisterDefinition | null {
  if (!code) return null;
  return byCode.value.get(normalize(code)) ?? null;
}

function install(nextVersion: string, nextDefinitions: EmotionalRegisterDefinition[]) {
    if (!nextVersion.trim()) throw new Error('La version du catalogue des registres est absente.');
    if (nextDefinitions.length === 0) throw new Error('Le catalogue des registres est vide.');

    const codes = nextDefinitions.map((definition) => normalize(definition.code));
    if (new Set(codes).size !== codes.length) {
      throw new Error('Le catalogue contient des codes de registre en doublon.');
    }
    if (nextDefinitions.some((definition) =>
      !definition.code.trim()
      || !definition.displayName.trim()
      || !definition.glyph
      || !definition.color.trim()
      || !Array.isArray(definition.incomingAffinities))) {
      throw new Error('Le catalogue contient une définition de registre incomplète.');
    }

    const knownCodes = new Set(codes);
    for (const definition of nextDefinitions) {
      const incomingCodes = definition.incomingAffinities.map((affinity) =>
        normalize(affinity.incomingRegister));
      if (incomingCodes.length !== codes.length
        || new Set(incomingCodes).size !== codes.length
        || incomingCodes.some((code) => !knownCodes.has(code))
        || definition.incomingAffinities.some((affinity) =>
          !Number.isFinite(affinity.multiplier) || affinity.multiplier < 0)) {
        throw new Error(`Le profil d’affinité du registre « ${definition.code} » est incomplet.`);
      }
    }

    version.value = nextVersion;
    definitions.value = [...nextDefinitions];
}

async function load() {
    if (isLoading.value || version.value) return;
    isLoading.value = true;
    error.value = null;
    try {
      const response = await emotionalRegistersApi.getCatalog();
      install(response.version, response.definitions);
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Chargement des registres émotionnels impossible.';
      throw caught;
    } finally {
      isLoading.value = false;
    }
}

export function useEmotionalRegisterCatalog() {
  return { version, definitions, isLoading, error, definitionOf, install, load };
}
