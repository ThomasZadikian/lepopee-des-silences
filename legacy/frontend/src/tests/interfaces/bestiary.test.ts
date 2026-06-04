import { describe, expect, it } from "vitest"
import type { Enemy, BestiaryUnlock } from "@/interfaces/bestiary"

describe("Enemy interface", () => {
  it("accepte un ennemi valide", () => {
    const enemy: Enemy = {
      id: 1, name: "Goblin", type: "basic",
      maxHP: 50, strength: 8, intelligence: 3, speed: 12,
      physicalResistance: 1, magicalResistance: 1,
      experienceReward: 10, goldReward: 5, description: "Un gobelin",
    }
    expect(enemy.name).toBe("Goblin")
    expect(enemy.maxHP).toBe(50)
  })
})

describe("BestiaryUnlock interface", () => {
  it("accepte un déblocage valide", () => {
    const unlock: BestiaryUnlock = {
      id: 1, playerId: 1, enemyId: 1, unlockedAt: new Date().toISOString(),
      enemy: { id: 1, name: "Goblin", type: "basic", maxHP: 50, strength: 8, intelligence: 3, speed: 12, physicalResistance: 1, magicalResistance: 1, experienceReward: 10, goldReward: 5, description: "" },
    }
    expect(unlock.enemy.name).toBe("Goblin")
  })
})
