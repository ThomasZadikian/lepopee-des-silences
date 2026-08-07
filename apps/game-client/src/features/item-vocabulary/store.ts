import { ref } from 'vue';

import { itemVocabularyApi } from './api';
import type {
  ItemEffectTypeDefinition,
  ItemRarityDefinition,
  ItemTypeDefinition,
} from './types';

function normalize(code: string): string {
  return code.trim().toLocaleLowerCase('fr');
}

function validateShape<T extends { code: string; displayName: string; glyph: string; color: string }>(
  kind: string,
  definitions: T[],
): void {
  if (definitions.length === 0) throw new Error(`Le catalogue des ${kind} est vide.`);

  const codes = definitions.map((d) => normalize(d.code));
  if (new Set(codes).size !== codes.length) {
    throw new Error(`Le catalogue des ${kind} contient des codes en doublon.`);
  }
  if (definitions.some((d) => !d.code.trim() || !d.displayName.trim() || !d.glyph || !d.color.trim())) {
    throw new Error(`Le catalogue des ${kind} contient une définition incomplète.`);
  }
}

const itemTypeVersion = ref<string | null>(null);
const itemTypeDefinitions = ref<ItemTypeDefinition[]>([]);
const itemRarityVersion = ref<string | null>(null);
const itemRarityDefinitions = ref<ItemRarityDefinition[]>([]);
const itemEffectTypeVersion = ref<string | null>(null);
const itemEffectTypeDefinitions = ref<ItemEffectTypeDefinition[]>([]);

const isLoading = ref(false);
const error = ref<string | null>(null);

function itemTypeOf(code: string | null | undefined): ItemTypeDefinition | null {
  if (!code) return null;
  const target = normalize(code);
  return itemTypeDefinitions.value.find((d) => normalize(d.code) === target) ?? null;
}

function itemRarityOf(code: string | null | undefined): ItemRarityDefinition | null {
  if (!code) return null;
  const target = normalize(code);
  return itemRarityDefinitions.value.find((d) => normalize(d.code) === target) ?? null;
}

function itemEffectTypeOf(code: string | null | undefined): ItemEffectTypeDefinition | null {
  if (!code) return null;
  const target = normalize(code);
  return itemEffectTypeDefinitions.value.find((d) => normalize(d.code) === target) ?? null;
}

function install(
  itemTypes: { version: string; definitions: ItemTypeDefinition[] },
  itemRarities: { version: string; definitions: ItemRarityDefinition[] },
  itemEffectTypes: { version: string; definitions: ItemEffectTypeDefinition[] },
) {
  if (!itemTypes.version.trim()) throw new Error('La version du catalogue des types d\'objet est absente.');
  if (!itemRarities.version.trim()) throw new Error('La version du catalogue des raretés est absente.');
  if (!itemEffectTypes.version.trim()) throw new Error('La version du catalogue des effets d\'objet est absente.');

  validateShape('types d\'objet', itemTypes.definitions);
  validateShape('raretés', itemRarities.definitions);
  validateShape('effets d\'objet', itemEffectTypes.definitions);

  itemTypeVersion.value = itemTypes.version;
  itemTypeDefinitions.value = [...itemTypes.definitions];
  itemRarityVersion.value = itemRarities.version;
  itemRarityDefinitions.value = [...itemRarities.definitions];
  itemEffectTypeVersion.value = itemEffectTypes.version;
  itemEffectTypeDefinitions.value = [...itemEffectTypes.definitions];
}

async function load() {
  if (isLoading.value || itemTypeVersion.value) return;
  isLoading.value = true;
  error.value = null;
  try {
    const [types, rarities, effectTypes] = await Promise.all([
      itemVocabularyApi.getItemTypes(),
      itemVocabularyApi.getItemRarities(),
      itemVocabularyApi.getItemEffectTypes(),
    ]);
    install(types, rarities, effectTypes);
  } catch (caught) {
    error.value = caught instanceof Error
      ? caught.message
      : 'Chargement du vocabulaire d\'objets impossible.';
    throw caught;
  } finally {
    isLoading.value = false;
  }
}

export function useItemVocabulary() {
  return {
    itemTypeVersion,
    itemTypeDefinitions,
    itemRarityVersion,
    itemRarityDefinitions,
    itemEffectTypeVersion,
    itemEffectTypeDefinitions,
    isLoading,
    error,
    itemTypeOf,
    itemRarityOf,
    itemEffectTypeOf,
    install,
    load,
  };
}
