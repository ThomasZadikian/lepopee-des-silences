export type EmotionalRegisterDefinition = {
  code: string;
  displayName: string;
  glyph: string;
  color: string;
  incomingAffinities: BaseEmotionalAffinity[];
};

export type BaseEmotionalAffinity = {
  incomingRegister: string;
  outcome: 'Neutral' | 'Weak' | 'Resistant' | 'Immune';
  multiplier: number;
};

export type EmotionalRegisterCatalog = {
  version: string;
  definitions: EmotionalRegisterDefinition[];
};
