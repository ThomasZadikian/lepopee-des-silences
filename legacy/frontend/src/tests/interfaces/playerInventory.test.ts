import { describe, expect, it } from "vitest"
import type { PlayerInventory, Item } from "@/interfaces/playerInventory"

describe("PlayerInventory interface", () => {
  it("accepte un inventaire valide", () => {
    const inv: PlayerInventory = {
      id: 1, playerId: 1, itemId: 10, quantity: 1, isEquipped: true,
      item: { id: 10, name: "Épée", type: "weapon", category: null, description: "", price: 100, effectValue: 15, statModifiers: "{}" },
    }
    expect(inv.isEquipped).toBe(true)
    expect(inv.quantity).toBe(1)
  })
})

describe("Item interface", () => {
  it("accepte un item valide", () => {
    const item: Item = {
      id: 1, name: "Potion", type: "consumable", category: "potion_hp",
      description: "Soigne", price: 25, effectValue: 50, statModifiers: "{}",
    }
    expect(item.name).toBe("Potion")
    expect(item.price).toBe(25)
  })
})
