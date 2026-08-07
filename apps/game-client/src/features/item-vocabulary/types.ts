export type ItemTypeDefinition = {
  code: string;
  displayName: string;
  glyph: string;
  color: string;
};

export type ItemTypeCatalog = {
  version: string;
  definitions: ItemTypeDefinition[];
};

export type ItemRarityDefinition = {
  code: string;
  displayName: string;
  glyph: string;
  color: string;
  palaceShardCost: number;
  himLitShardCost: number;
};

export type ItemRarityCatalog = {
  version: string;
  definitions: ItemRarityDefinition[];
};

export type ItemEffectTypeDefinition = {
  code: string;
  displayName: string;
  glyph: string;
  color: string;
};

export type ItemEffectTypeCatalog = {
  version: string;
  definitions: ItemEffectTypeDefinition[];
};
