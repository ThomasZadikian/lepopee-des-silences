import { describe, expect, it } from "vitest"
import {
  ENEMY_TYPE_BASIC, ENEMY_TYPE_BOSS, ENEMY_TYPE_MINIBOSS,
  ENEMY_STATE_REPOS, ENEMY_STATE_PATROUILLE, ENEMY_STATE_CHASSE, ENEMY_STATE_FUITE,
  ITEM_TYPE_WEAPON, ITEM_TYPE_ARMOR, ITEM_TYPE_ACCESSORY, ITEM_TYPE_CONSUMABLE,
  EFFECT_DAMAGE, EFFECT_HEAL, EFFECT_BUFF, EFFECT_DEBUFF,
  ELEMENT_NEUTRAL, ELEMENT_FIRE, ELEMENT_ICE, ELEMENT_LIGHTNING,
  NPC_TYPE_NEUTRAL, NPC_TYPE_MERCHANT, NPC_TYPE_QUEST, NPC_TYPE_ALLY,
  COMPANION_STATE_REPOS, COMPANION_STATE_JEU, COMPANION_STATE_MANGER,
  COMPANION_STATE_EXCITE, COMPANION_STATE_TRISTE, COMPANION_STATE_ENDORMI,
  ROLE_PLAYER, ROLE_ADMIN,
} from "@/constants"

describe("constants - Enemy", () => {
  it("définit les types d'ennemis", () => {
    expect(ENEMY_TYPE_BASIC).toBe("basic")
    expect(ENEMY_TYPE_MINIBOSS).toBe("miniboss")
    expect(ENEMY_TYPE_BOSS).toBe("boss")
  })

  it("définit les états d'ennemis", () => {
    expect(ENEMY_STATE_REPOS).toBe("REPOS")
    expect(ENEMY_STATE_PATROUILLE).toBe("PATROUILLE")
    expect(ENEMY_STATE_CHASSE).toBe("CHASSE")
    expect(ENEMY_STATE_FUITE).toBe("FUITE")
  })
})

describe("constants - Item", () => {
  it("définit les types d'items", () => {
    expect(ITEM_TYPE_WEAPON).toBe("weapon")
    expect(ITEM_TYPE_ARMOR).toBe("armor")
    expect(ITEM_TYPE_ACCESSORY).toBe("accessory")
    expect(ITEM_TYPE_CONSUMABLE).toBe("consumable")
  })
})

describe("constants - Skill", () => {
  it("définit les effets", () => {
    expect(EFFECT_DAMAGE).toBe("damage")
    expect(EFFECT_HEAL).toBe("heal")
    expect(EFFECT_BUFF).toBe("buff")
    expect(EFFECT_DEBUFF).toBe("debuff")
  })

  it("définit les éléments", () => {
    expect(ELEMENT_NEUTRAL).toBe("neutral")
    expect(ELEMENT_FIRE).toBe("fire")
    expect(ELEMENT_ICE).toBe("ice")
    expect(ELEMENT_LIGHTNING).toBe("lightning")
  })
})

describe("constants - NPC", () => {
  it("définit les types de NPC", () => {
    expect(NPC_TYPE_NEUTRAL).toBe("neutral")
    expect(NPC_TYPE_MERCHANT).toBe("merchant")
    expect(NPC_TYPE_QUEST).toBe("quest")
    expect(NPC_TYPE_ALLY).toBe("ally")
  })
})

describe("constants - Companion", () => {
  it("définit les états du companion", () => {
    expect(COMPANION_STATE_REPOS).toBe("REPOS")
    expect(COMPANION_STATE_JEU).toBe("JEU")
    expect(COMPANION_STATE_MANGER).toBe("MANGER")
    expect(COMPANION_STATE_EXCITE).toBe("EXCITE")
    expect(COMPANION_STATE_TRISTE).toBe("TRISTE")
    expect(COMPANION_STATE_ENDORMI).toBe("ENDORMI")
  })
})

describe("constants - Rôles", () => {
  it("définit les rôles", () => {
    expect(ROLE_PLAYER).toBe("Player")
    expect(ROLE_ADMIN).toBe("Admin")
  })
})
