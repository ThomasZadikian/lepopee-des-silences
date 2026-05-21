import AdminSkillsView from "@/views/admin/AdminSkillsView.vue"
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
    { path: "/", name: "AdminSkills", component: { template: "<div/>" } },
    { path: "/admin/users", name: "AdminUsers", component: { template: "<div/>" } },
  ],
})

const mockSkills = {
  data: { items: [
    { id: 1, name: "Boule de feu", mpCost: 15, baseDamage: 80, healAmount: 0, effectType: "damage", elementType: "fire", description: "Inflige des dégâts de feu" },
    { id: 2, name: "Soin", mpCost: 10, baseDamage: 0, healAmount: 50, effectType: "heal", elementType: "neutral", description: "Soigne les blessures" },
  ]},
}

describe("AdminSkillsView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(api.get).mockResolvedValue(mockSkills as any)
  })

  it("affiche le titre admin", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Admin")
  })

  it("affiche la liste des compétences", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Boule de feu")
    expect(wrapper.text()).toContain("Soin")
  })

  it("affiche les coûts MP", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("15")
    expect(wrapper.text()).toContain("10")
  })

  it("appelle api.get au montage", async () => {
    mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(api.get).toHaveBeenCalled()
  })

  it("gère l'état vide", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).not.toContain("Boule de feu")
  })
})
