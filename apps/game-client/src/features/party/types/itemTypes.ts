export type ItemDefinitionView = {
  key: string;
  displayName: string;
  description: string;
  category: string;
  itemType: string;
  rarity: string;
  effectRunType: string | null;
  effectValue: number;
  readablePages?: string[] | null;
  tacticalRange?: number;
  tacticalAreaShape?: string;
  requiresLineOfSight?: boolean;
  basicAttackPower?: number | null;
  basicAttackCategory?: string | null;
  equipmentEffects?: Array<{
    kind: string;
    statKind?: string | null;
    amount?: number | null;
    skillKey?: string | null;
    affinityRegister?: string | null;
  }>;
  isContainer?: boolean;
  containerCapacity?: number | null;
  isLiquid?: boolean;
  palaceShardCost?: number;
  himLitShardCost?: number;
};
