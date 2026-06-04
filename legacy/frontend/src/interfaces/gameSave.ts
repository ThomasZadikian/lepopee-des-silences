export interface GameSave {
  id: number;
  playerId: number;
  currentZone: string;
  positionX: number;
  positionY: number;
  inventoryData: string | null; // JSON
  questFlags: string | null; // JSON
  savedAt: string;
}

export interface CreateGameSaveDto {
  playerId: number;
  currentZone: string;
  playerLevel: number;
}
