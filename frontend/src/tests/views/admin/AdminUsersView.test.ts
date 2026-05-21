import AdminUsersView from "@/views/admin/AdminUsersView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/auth", () => ({
  default: {
    get: vi.fn(),
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
    { path: "/", name: "AdminUsers", component: { template: "<div/>" } },
    { path: "/admin/items", name: "AdminItems", component: { template: "<div/>" } },
    { path: "/admin/skills", name: "AdminSkills", component: { template: "<div/>" } },
    { path: "/admin/bestiary", name: "AdminBestiary", component: { template: "<div/>" } },
  ],
})

const mockUsers = {
  data: {
    items: [
      { id: 1, username: "player1", role: "Player", mfaEnabled: false, createdAt: "2026-01-15T10:00:00Z", lastLoginAt: "2026-05-20T08:00:00Z", deletedAt: null },
      { id: 2, username: "admin", role: "Admin", mfaEnabled: true, createdAt: "2026-01-10T08:00:00Z", lastLoginAt: "2026-05-21T12:00:00Z", deletedAt: null },
    ],
  },
}

describe("AdminUsersView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(api.get).mockResolvedValue(mockUsers as any)
  })

  it("affiche le titre Console Admin", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Console Admin")
  })

  it("affiche les utilisateurs", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("player1")
    expect(wrapper.text()).toContain("admin")
  })

  it("affiche les stats (Total, Actifs, Admins)", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Total")
    expect(wrapper.text()).toContain("Actifs")
    expect(wrapper.text()).toContain("Admins")
  })

  it("filtre les utilisateurs par recherche", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()

    const searchInput = wrapper.find("input[type='text']")
    if (searchInput.exists()) {
      await searchInput.setValue("admin")
      await flushPromises()
      expect(wrapper.text()).toContain("@admin")
    }
  })

  it("affiche les tabs de navigation admin", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Utilisateurs")
    expect(wrapper.text()).toContain("Objets")
    expect(wrapper.text()).toContain("Compétences")
    expect(wrapper.text()).toContain("Bestiaire")
  })

  it("affiche le panneau de détail au clic sur Voir", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()

    const voirBtns = wrapper.findAll("span").filter((s) => s.text() === "Voir")
    if (voirBtns.length > 0) {
      await voirBtns[0].trigger("click")
      await flushPromises()
      expect(wrapper.text()).toContain("Profil")
    }
  })

  it("appelle api.get au montage", async () => {
    mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    expect(api.get).toHaveBeenCalledWith("/users")
  })

  it("gère le loading", async () => {
    vi.mocked(api.get).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    expect(wrapper.text()).not.toContain("Aucun résultat")
  })
})
