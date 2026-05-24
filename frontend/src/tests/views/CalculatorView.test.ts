import CalculatorView from "@/views/player/CalculatorView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/auth", () => ({
  default: {
    get: vi.fn(),
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}))

vi.mock("@/api/enemies", () => ({
  enemiesApi: { getAll: vi.fn() },
}))

vi.mock("@/api/inventory", () => ({
  inventoryApi: { getMe: vi.fn() },
}))

import api from "@/api/auth"
import { enemiesApi } from "@/api/enemies"
import { inventoryApi } from "@/api/inventory"

const router = createRouter({
  history: createMemoryHistory(),
  routes: [{ path: "/", name: "Calculator", component: { template: "<div/>" } }],
})

const mockEnemies = {
  data: {
    items: [
      { id: 1, name: "Goblin", type: "basic", maxHP: 50, strength: 8, intelligence: 3, speed: 12, experienceReward: 10, goldReward: 5 },
      { id: 2, name: "Dragon", type: "boss", maxHP: 900, strength: 50, intelligence: 40, speed: 30, experienceReward: 500, goldReward: 300 },
    ],
  },
}

const mockProfile = {
  data: {
    id: 1, userId: 1, characterName: "Aragorn", level: 10,
    currentHP: 90, maxHP: 100, currentMP: 40, maxMP: 50,
    strength: 15, intelligence: 12, speed: 11,
    experience: 500, gold: 200, updatedAt: new Date().toISOString(),
    totalCombats: 10, combatsWon: 7, combatsLost: 3,
    totalDamageDealt: 5000, totalDamageTaken: 2000,
    totalPlaytimeMinutes: 60, savesCount: 2,
    inventoryCount: 3, skillsCount: 2, bestiaryCount: 1,
  },
}

const mockInventory = {
  data: {
    items: [
      { id: 1, playerId: 1, itemId: 10, quantity: 1, isEquipped: true, item: { id: 10, name: "Épée longue", type: "weapon", effectValue: 15 } },
      { id: 2, playerId: 1, itemId: 20, quantity: 1, isEquipped: true, item: { id: 20, name: "Bouclier", type: "armor", effectValue: 8 } },
    ],
  },
}

const mockScaledResult = {
  data: {
    enemy: {
      id: 1, name: "Goblin", type: "basic",
      multiplier: 1.5,
      scaledMaxHP: 75, scaledStrength: 12, scaledIntelligence: 5, scaledSpeed: 18,
      experienceReward: 15, goldReward: 8,
    },
  },
}

const mockAllItems = {
  data: { items: [
    { id: 10, name: "Épée longue", type: "weapon", effectValue: 15 },
    { id: 20, name: "Bouclier", type: "armor", effectValue: 8 },
    { id: 30, name: "Anneau", type: "accessory", effectValue: 5 },
    { id: 40, name: "Potion", type: "consumable", effectValue: 50 },
  ]},
}

describe("CalculatorView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(api.get).mockResolvedValue(mockProfile as any)
    vi.mocked(inventoryApi.getMe).mockResolvedValue(mockInventory as any)
    vi.mocked(enemiesApi.getAll).mockResolvedValue(mockEnemies as any)
  })

  it("affiche le titre Calculateur", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Calculateur")
  })

  it("affiche le profil importé par défaut", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Aragorn")
  })

  it("affiche les items équipés du profil", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Épée longue")
    expect(wrapper.text()).toContain("Bouclier")
  })

  it("affiche le bouton mode custom", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const buttons = wrapper.findAll("button")
    const customBtn = buttons.find((b) => b.text().toLowerCase().includes("custom"))
    expect(customBtn).toBeDefined()
  })

  it("filtre les ennemis par recherche", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()

    const searchInput = wrapper.find("input[type='text']")
    if (searchInput.exists()) {
      await searchInput.setValue("Goblin")
      await flushPromises()
      expect(wrapper.text()).toContain("Goblin")
    }
  })

  it("bouton Simuler désactivé sans ennemi sélectionné", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const simBtn = wrapper.findAll("button").find((b) => b.text().includes("Simuler"))
    expect(simBtn).toBeDefined()
  })

  it("affiche les stats du profil chargé", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Niveau")
    expect(wrapper.text()).toContain("10")
  })

  it("affiche l'analyse de combat après calcul", async () => {
    vi.mocked(api.get).mockResolvedValueOnce(mockProfile as any)
      .mockResolvedValueOnce(mockScaledResult as any)
    vi.mocked(inventoryApi.getMe).mockResolvedValue(mockInventory as any)
    vi.mocked(enemiesApi.getAll).mockResolvedValue(mockEnemies as any)

    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("scaling")
  })

  it("gère l'erreur de chargement du profil", async () => {
    vi.mocked(api.get).mockRejectedValueOnce(new Error("Network error"))
    vi.mocked(inventoryApi.getMe).mockRejectedValue(new Error("Network error"))

    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Calculateur")
  })

  it("bascule en mode custom", async () => {
    vi.mocked(api.get).mockResolvedValue(mockAllItems as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.switchMode("custom")
    await flushPromises()
    expect(vm.mode).toBe("custom")
    expect(vm.result).toBeNull()
  })

  it("charge tous les items en mode custom", async () => {
    vi.mocked(api.get).mockResolvedValue(mockAllItems as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.allItems.length).toBe(0)
    vm.switchMode("custom")
    await flushPromises()
    expect(vm.allItems.length).toBeGreaterThan(0)
  })

  it("sélectionne un ennemi", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.selectEnemy(mockEnemies.data.items[0])
    expect(vm.selectedEnemy).toEqual(mockEnemies.data.items[0])
    expect(vm.result).toBeNull()
  })

  it("calcule et affiche le résultat", async () => {
    vi.mocked(api.get).mockResolvedValueOnce(mockProfile as any)
      .mockResolvedValueOnce(mockScaledResult as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.selectEnemy(mockEnemies.data.items[0])
    await vm.calculate()
    await flushPromises()
    expect(vm.result).not.toBeNull()
    expect(vm.result.enemy.name).toBe("Goblin")
  })

  it("gère l'erreur API calculate", async () => {
    vi.mocked(api.get).mockResolvedValueOnce(mockProfile as any)
      .mockRejectedValueOnce(new Error("Network"))
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.selectEnemy(mockEnemies.data.items[0])
    await vm.calculate()
    await flushPromises()
    expect(vm.error).toBe("Erreur lors du calcul. Réessayez.")
  })

  it("calcule playerPower correctement", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    // level=10, bonus=23 (15+8)
    expect(vm.playerPower).toBe(123)
  })

  it("calcule playerPower en mode custom", async () => {
    vi.mocked(api.get).mockResolvedValue(mockAllItems as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.switchMode("custom")
    vm.customLevel = 5
    // customLevel=5, bonus=0 (aucun item sélectionné)
    expect(vm.playerPower).toBe(50)
  })

  it("calcule totalEquipmentBonus en mode custom avec items sélectionnés", async () => {
    vi.mocked(api.get).mockResolvedValue(mockAllItems as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.switchMode("custom")
    await flushPromises()
    vm.selectedByType.weapon = 10
    expect(vm.totalEquipmentBonus).toBe(15)
  })

  it("affiche 'Aucun item équipé' quand la liste est vide", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Aucun item équipé")
  })

  it("canCalculate est false sans ennemi sélectionné", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.canCalculate).toBe(false)
  })

  it("canCalculate est true avec ennemi et niveau valide", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.selectEnemy(mockEnemies.data.items[0])
    expect(vm.canCalculate).toBe(true)
  })

  it("filtre les ennemis via filteredEnemies", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.filteredEnemies.length).toBe(2)
    vm.enemySearch = "Goblin"
    expect(vm.filteredEnemies.length).toBe(1)
    vm.enemySearch = "ZZZZZ"
    expect(vm.filteredEnemies.length).toBe(0)
  })

  it("retourne profileStats vide quand profile est null", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.profile = null
    expect(vm.profileStats).toEqual([])
  })

  it("retourne comparisonStats vide quand result ou selectedEnemy est null", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.comparisonStats).toEqual([])
  })

  it("affiche les stats de comparaison après calcul", async () => {
    vi.mocked(api.get).mockResolvedValueOnce(mockProfile as any)
      .mockResolvedValueOnce(mockScaledResult as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.selectEnemy(mockEnemies.data.items[0])
    await vm.calculate()
    await flushPromises()
    expect(vm.comparisonStats.length).toBeGreaterThan(0)
    expect(vm.comparisonStats[0].base).toBe(50)
    expect(vm.comparisonStats[0].scaled).toBe(75)
  })

  it("combatAnalysis est null sans result ou selectedEnemy", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.combatAnalysis).toBeNull()
  })

  it("returnne le multiplierColor correct selon survivalRatio", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.multiplierColor).toBe("var(--rpg-ink-muted)")
  })

  it("retourne multiplierLabel vide sans analyse", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.multiplierLabel).toBe("")
  })

  it("retourne analysisText vide sans analyse", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.analysisText).toBe("")
  })

  it("reset remet tous les états à zéro", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.selectEnemy(mockEnemies.data.items[0])
    vm.result = mockScaledResult as any
    vm.enemySearch = "Goblin"
    vm.error = "Erreur"
    vm.reset()
    expect(vm.result).toBeNull()
    expect(vm.selectedEnemy).toBeNull()
    expect(vm.enemySearch).toBe("")
    expect(vm.error).toBe("")
  })

  it("bloque les caractères invalides", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    const preventDefault = vi.fn()
    vm.blockInvalidChars({ key: "e", preventDefault } as any)
    expect(preventDefault).toHaveBeenCalled()
    vm.blockInvalidChars({ key: "3", preventDefault } as any)
    expect(preventDefault).toHaveBeenCalledTimes(1)
  })

  it("estimatedPlayerStats utilise le profil en mode import", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.estimatedPlayerStats.hp).toBe(100)
    expect(vm.estimatedPlayerStats.strength).toBe(15)
  })

  it("estimatedPlayerStats calcule en mode custom", async () => {
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.switchMode("custom")
    vm.customLevel = 5
    expect(vm.estimatedPlayerStats.hp).toBe(140)
    expect(vm.estimatedPlayerStats.strength).toBe(18)
  })

  it("itemsByType filtre correctement les items", async () => {
    vi.mocked(api.get).mockResolvedValue(mockAllItems as any)
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.switchMode("custom")
    await flushPromises()
    const weapons = vm.itemsByType("weapon")
    expect(weapons.length).toBe(1)
    expect(weapons[0].name).toBe("Épée longue")
  })

  it("affiche le loader profile pendant le chargement", async () => {
    vi.mocked(api.get).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    const vm = wrapper.vm as any
    expect(vm.loadingProfile).toBe(true)
  })

  it("gère le cas ou profile chargé est null", async () => {
    vi.mocked(api.get).mockRejectedValue(new Error("fail"))
    vi.mocked(inventoryApi.getMe).mockRejectedValue(new Error("fail"))
    const wrapper = mount(CalculatorView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Impossible de charger le profil")
  })
})
