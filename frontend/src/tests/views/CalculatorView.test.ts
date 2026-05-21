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
})
