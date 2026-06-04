// src/tests/api/playerProfile.test.ts
import { beforeEach, describe, expect, it, vi } from "vitest";

// Mock Axios avant tout import
vi.mock("@/api/auth", () => ({
  default: {
    get: vi.fn(),
    interceptors: {
      request: { use: vi.fn() },
      response: { use: vi.fn() },
    },
  },
}));

import api from "@/api/auth";
import {
  playerProfileApi,
  type PlayerProfileResponse,
} from "@/interfaces/playerProfile";

describe("playerProfileApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("getMe", () => {
    it("appelle GET /playerprofile/me", async () => {
      const mockResponse: PlayerProfileResponse = {
        id: 10,
        userId: 1,
        characterName: "Aragorn",
        level: 5,
        currentHP: 80,
        maxHP: 100,
        currentMP: 30,
        maxMP: 50,
        strength: 15,
        intelligence: 12,
        speed: 11,
        experience: 1250,
        gold: 300,
        updatedAt: "2026-04-30T10:00:00Z",
        totalCombats: 20,
        combatsWon: 15,
        combatsLost: 5,
        totalDamageDealt: 8000,
        totalDamageTaken: 3000,
        totalPlaytimeMinutes: 120,
        savesCount: 2,
        inventoryCount: 1,
        skillsCount: 3,
        bestiaryCount: 1,
      };

      vi.mocked(api.get).mockResolvedValue({ data: mockResponse });

      await playerProfileApi.getMe();

      expect(api.get).toHaveBeenCalledWith("/playerprofile/me");
    });

    it("retourne les données du profil", async () => {
      const mockResponse: PlayerProfileResponse = {
        id: 10,
        userId: 1,
        characterName: "Aragorn",
        level: 5,
        currentHP: 80,
        maxHP: 100,
        currentMP: 30,
        maxMP: 50,
        strength: 15,
        intelligence: 12,
        speed: 11,
        experience: 1250,
        gold: 300,
        updatedAt: "2026-04-30T10:00:00Z",
        totalCombats: 20,
        combatsWon: 15,
        combatsLost: 5,
        totalDamageDealt: 8000,
        totalDamageTaken: 3000,
        totalPlaytimeMinutes: 120,
        savesCount: 2,
        inventoryCount: 1,
        skillsCount: 3,
        bestiaryCount: 1,
      };

      vi.mocked(api.get).mockResolvedValue({ data: mockResponse });

      const result = await playerProfileApi.getMe();

      expect(result.data.characterName).toBe("Aragorn");
      expect(result.data.level).toBe(5);
    });

    it("gère un profil sans stats de combat (null)", async () => {
      const mockResponse: PlayerProfileResponse = {
        id: 10,
        userId: 1,
        characterName: "Newbie",
        level: 1,
        currentHP: 100,
        maxHP: 100,
        currentMP: 50,
        maxMP: 50,
        strength: 10,
        intelligence: 10,
        speed: 10,
        experience: 0,
        gold: 0,
        updatedAt: "2026-04-30T10:00:00Z",
        totalCombats: null,
        combatsWon: null,
        combatsLost: null,
        totalDamageDealt: null,
        totalDamageTaken: null,
        totalPlaytimeMinutes: null,
        savesCount: 0,
        inventoryCount: 0,
        skillsCount: 0,
        bestiaryCount: 0,
      };

      vi.mocked(api.get).mockResolvedValue({ data: mockResponse });

      const result = await playerProfileApi.getMe();

      expect(result.data.totalCombats).toBeNull();
      expect(result.data.savesCount).toBe(0);
    });
  });
});

// ── Tests de la structure des interfaces ─────────────────────────────────────
describe("PlayerProfileResponse interface", () => {
  it("contient tous les champs obligatoires", () => {
    const profile: PlayerProfileResponse = {
      id: 1,
      userId: 1,
      characterName: "Test",
      level: 1,
      currentHP: 100,
      maxHP: 100,
      currentMP: 50,
      maxMP: 50,
      strength: 10,
      intelligence: 10,
      speed: 10,
      experience: 0,
      gold: 0,
      updatedAt: "2026-01-01T00:00:00Z",
      totalCombats: null,
      combatsWon: null,
      combatsLost: null,
      totalDamageDealt: null,
      totalDamageTaken: null,
      totalPlaytimeMinutes: null,
      savesCount: 0,
      inventoryCount: 0,
      skillsCount: 0,
      bestiaryCount: 0,
    };

    expect(profile.id).toBeDefined();
    expect(profile.userId).toBeDefined();
    expect(profile.characterName).toBeDefined();
    expect(profile.savesCount).toBeDefined();
    expect(profile.inventoryCount).toBeDefined();
    expect(profile.skillsCount).toBeDefined();
    expect(profile.bestiaryCount).toBeDefined();
  });

  it("accepte des stats de combat nulles", () => {
    const profile: PlayerProfileResponse = {
      id: 1,
      userId: 1,
      characterName: "Test",
      level: 1,
      currentHP: 100,
      maxHP: 100,
      currentMP: 50,
      maxMP: 50,
      strength: 10,
      intelligence: 10,
      speed: 10,
      experience: 0,
      gold: 0,
      updatedAt: "2026-01-01T00:00:00Z",
      totalCombats: null,
      combatsWon: null,
      combatsLost: null,
      totalDamageDealt: null,
      totalDamageTaken: null,
      totalPlaytimeMinutes: null,
      savesCount: 0,
      inventoryCount: 0,
      skillsCount: 0,
      bestiaryCount: 0,
    };

    expect(profile.totalCombats).toBeNull();
    expect(profile.totalDamageDealt).toBeNull();
  });

  it("accepte des stats de combat renseignées", () => {
    const profile: PlayerProfileResponse = {
      id: 1,
      userId: 1,
      characterName: "Veteran",
      level: 20,
      currentHP: 500,
      maxHP: 500,
      currentMP: 200,
      maxMP: 200,
      strength: 50,
      intelligence: 40,
      speed: 35,
      experience: 50000,
      gold: 9999,
      updatedAt: "2026-04-30T10:00:00Z",
      totalCombats: 100,
      combatsWon: 80,
      combatsLost: 20,
      totalDamageDealt: 500000,
      totalDamageTaken: 100000,
      totalPlaytimeMinutes: 3600,
      savesCount: 10,
      inventoryCount: 25,
      skillsCount: 8,
      bestiaryCount: 15,
    };

    expect(profile.totalCombats).toBe(100);
    expect(profile.combatsWon).toBe(80);
    expect(profile.totalPlaytimeMinutes).toBe(3600);
  });
});
