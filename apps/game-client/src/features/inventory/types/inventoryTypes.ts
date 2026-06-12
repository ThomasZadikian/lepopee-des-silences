export interface RunItemDto {
  id: string;
  definitionKey: string;
  displayName: string;
  description: string;
  type: 'Consumable' | 'Passive' | 'Fragment';
  rarity: 'Common' | 'Uncommon' | 'Rare' | 'Epic';
  quantity: number;
  effectType: string;
  effectAmount: number;
}

export interface GetRunInventoryResponse {
  runId: string;
  items: RunItemDto[];
}
