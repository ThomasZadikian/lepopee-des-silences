import AdminBestiaireView from "@/views/admin/AdminBestiaireView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/auth", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}))

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    username: "admin",
    isAdmin: true,
  }),
}))

import api from "@/api/auth"

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: "/", name: "AdminBestiary", component: { template: "<div/>" } },
    { path: "/admin/users", name: "AdminUsers", component: { template: "<div/>" } },
  ],
})

const mockEnemies = {
  data: { items: [
    { id: 1, name: "Goblin", type: "basic", maxHP: 50, strength: 8, intelligence: 3, speed: 12, physicalResistance: 1, magicalResistance: 1, experienceReward: 10, goldReward: 5, description: "Petit gobelin" },
    { id: 2, name: "Dragon", type: "boss", maxHP: 900, strength: 50, intelligence: 40, speed: 30, physicalResistance: 1.5, magicalResistance: 1.5, experienceReward: 500, goldReward: 300, description: "Dragon ancien" },
  ]},
}

describe("AdminBestiaireView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(api.get).mockResolvedValue(mockEnemies as any)
  })

  it("affiche le titre admin", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Admin")
  })

  it("affiche la liste des ennemis", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Goblin")
    expect(wrapper.text()).toContain("Dragon")
  })

  it("affiche les types d'ennemis", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("basic")
    expect(wrapper.text()).toContain("boss")
  })

  it("appelle api.get au montage", async () => {
    mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(api.get).toHaveBeenCalled()
  })

  it("gère l'état vide", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).not.toContain("Goblin")
  })
})
