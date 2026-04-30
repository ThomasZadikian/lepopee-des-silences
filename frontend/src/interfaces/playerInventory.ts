export interface PlayerInventory {
  id: number;
  playerId: number;
  itemId: number;
  quantity: number;
  isEquipped: boolean;
  item: Item;
}

export interface Item {
  id: number;
  name: string;
  type: string;
  category: string | null;
  description: string | null;
  price: number;
  effectValue: number | null;
}
