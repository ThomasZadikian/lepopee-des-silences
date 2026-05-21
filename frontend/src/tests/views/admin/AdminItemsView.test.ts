import AdminItemsView from "@/views/admin/AdminItemsView.vue"
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
    { path: "/", name: "AdminItems", component: { template: "<div/>" } },
    { path: "/admin/users", name: "AdminUsers", component: { template: "<div/>" } },
  ],
})

const mockItems = {
  data: { items: [
    { id: 1, name: "Épée", type: "weapon", category: null, description: "Une épée", price: 100, effectValue: 15, statModifiers: "{}" },
    { id: 2, name: "Potion", type: "consumable", category: "potion_hp", description: "Soigne", price: 25, effectValue: 50, statModifiers: "{}" },
  ]},
}

describe("AdminItemsView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(api.get).mockResolvedValue(mockItems as any)
  })

  it("affiche le titre admin", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Admin")
  })

  it("affiche la liste des items", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Épée")
    expect(wrapper.text()).toContain("Potion")
  })

  it("affiche les stats items", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("weapon")
    expect(wrapper.text()).toContain("consumable")
  })

  it("appelle api.get au montage", async () => {
    mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(api.get).toHaveBeenCalled()
  })

  it("gère l'état vide", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).not.toContain("Épée")
  })
})
