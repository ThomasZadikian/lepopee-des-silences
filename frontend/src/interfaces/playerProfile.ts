import api from "@/api/auth";

export interface PlayerProfileResponse {
  id: number;
  userId: number;
  characterName: string;
  level: number;
  currentHP: number;
  maxHP: number;
  currentMP: number;
  maxMP: number;
  strength: number;
  intelligence: number;
  speed: number;
  experience: number;
  gold: number;
  updatedAt: string;

  // Stats de combat (null si pas encore joué)
  totalCombats: number | null;
  combatsWon: number | null;
  combatsLost: number | null;
  totalDamageDealt: number | null;
  totalDamageTaken: number | null;
  totalPlaytimeMinutes: number | null;

  // Compteurs pour le dashboard
  savesCount: number;
  inventoryCount: number;
  skillsCount: number;
  bestiaryCount: number;
}

export const playerProfileApi = {
  getMe: () => api.get<PlayerProfileResponse>("/playerprofile/me"),
};
