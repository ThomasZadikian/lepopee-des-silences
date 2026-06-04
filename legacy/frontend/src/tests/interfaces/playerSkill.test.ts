import { describe, expect, it } from "vitest"
import type { PlayerSkill, Skill } from "@/interfaces/playerSkill"

describe("PlayerSkill interface", () => {
  it("accepte une compétence joueur valide", () => {
    const ps: PlayerSkill = {
      id: 1, playerId: 1, skillId: 1, unlockedAt: new Date().toISOString(),
      skill: { id: 1, name: "Boule de feu", mpCost: 15, baseDamage: 80, healAmount: 0, effectType: "damage", elementType: "fire", description: "" },
    }
    expect(ps.skill.name).toBe("Boule de feu")
  })
})

describe("Skill interface", () => {
  it("accepte une compétence valide", () => {
    const skill: Skill = {
      id: 1, name: "Soin", mpCost: 10, baseDamage: 0, healAmount: 50,
      effectType: "heal", elementType: "neutral", description: "Soigne",
    }
    expect(skill.healAmount).toBe(50)
    expect(skill.mpCost).toBe(10)
  })
})
