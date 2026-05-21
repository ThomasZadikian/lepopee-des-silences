import { describe, expect, it } from "vitest"
import type { GameSave, CreateGameSaveDto } from "@/interfaces/gameSave"

describe("GameSave interface", () => {
  it("accepte une sauvegarde valide", () => {
    const save: GameSave = {
      id: 1, playerId: 1, currentZone: "Forêt",
      positionX: 100, positionY: 200,
      inventoryData: "{}", questFlags: "{}",
      savedAt: new Date().toISOString(),
    }
    expect(save.currentZone).toBe("Forêt")
    expect(save.positionX).toBe(100)
  })
})

describe("CreateGameSaveDto interface", () => {
  it("accepte un DTO valide", () => {
    const dto: CreateGameSaveDto = { playerId: 1, currentZone: "Forêt", playerLevel: 5 }
    expect(dto.playerLevel).toBe(5)
    expect(dto.currentZone).toBe("Forêt")
  })
})
