export type ItemDefinitionView = {
  key: string;
  displayName: string;
  description: string;
  category: string;
  flavorTag: string;
  rarity: string;
  effectRunType: string | null;
  effectValue: number;
  /** Transitional single-slot convenience. Prefer allowedSlots. */
  equipSlot?: string | null;
  allowedSlots?: string[];
  uniqueEquipGroup?: string | null;
  proficiencyTags?: string[];
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
