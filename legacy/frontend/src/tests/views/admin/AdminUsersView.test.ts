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
    const vm = wrapper.vm as any
    vm.openView(mockUsers.data.items[0])
    await flushPromises()
    expect(vm.panel).not.toBeNull()
    expect(vm.mode).toBe("view")
    expect(wrapper.text()).toContain("Profil")
  })

  it("appelle api.get au montage", async () => {
    mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    expect(api.get).toHaveBeenCalledWith("/users")
  })

  it("loading est true pendant le chargement", async () => {
    vi.mocked(api.get).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    const vm = wrapper.vm as any
    expect(vm.loading).toBe(true)
  })

  it("ouvre le panneau de confirmation de suppression", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockUsers.data.items[0])
    await flushPromises()
    expect(vm.mode).toBe("delete")
    expect(wrapper.text()).toContain("Supprimer")
    expect(wrapper.text()).toContain("@player1")
  })

  it("ferme le panneau", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockUsers.data.items[0])
    expect(vm.panel).not.toBeNull()
    vm.closePanel()
    expect(vm.panel).toBeNull()
    expect(vm.formError).toBe("")
  })

  it("supprime un utilisateur via DELETE et marque deletedAt", async () => {
    vi.mocked(api.delete).mockResolvedValue({} as any)
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.users.length).toBe(2)
    vm.confirmDelete(mockUsers.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith("/users/1")
    expect(vm.users[0].deletedAt).not.toBeNull()
  })

  it("gère l'erreur API lors de la suppression", async () => {
    vi.mocked(api.delete).mockRejectedValue(new Error("Network"))
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockUsers.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(vm.formError).toBe("Erreur lors de la suppression.")
  })

  it("affiche 'Aucun résultat' quand le filtre ne matche rien", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("ZZZZZ")
    await flushPromises()
    expect(wrapper.text()).toContain("Aucun résultat")
  })

  it("calcule correctement les stats utilisateurs", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.stats[0].label).toBe("Total")
    expect(vm.stats[1].label).toBe("Actifs")
    expect(vm.stats[2].label).toBe("Admins")
    expect(vm.stats[3].label).toBe("MFA activé")
    expect(vm.stats[4].label).toBe("Supprimés")
    expect(vm.stats[2].value).toBeGreaterThanOrEqual(1)
    expect(vm.stats[4].value).toBeGreaterThanOrEqual(0)
  })

  it("retourne les stats détail pour un utilisateur", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockUsers.data.items[0])
    const stats = vm.detailStats
    expect(stats[0].label).toBe("Rôle")
    expect(stats[0].value).toBe("Player")
    expect(stats[1].label).toBe("MFA")
    expect(stats[4].label).toBe("Statut")
  })

  it("retourne detailStats vide quand panel est null", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.detailStats).toEqual([])
  })

  it("retourne les stats détail utilisateur supprimé", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    const deletedUser = { ...mockUsers.data.items[0], deletedAt: "2026-06-01T10:00:00Z" }
    vm.openView(deletedUser)
    expect(vm.detailStats[4].value).toBe("Supprimé")
  })

  it("retourne le formatDate correct", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.formatDate(null)).toBe("—")
    expect(vm.formatDate("2026-01-15T10:00:00Z")).toMatch(/\d{2}\/\d{2}\/\d{4}/)
  })

  it("le bouton Supprimer n'est pas visible pour les utilisateurs supprimés", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    const deletedUser = { ...mockUsers.data.items[0], deletedAt: "2026-06-01T10:00:00Z" }
    vm.openView(deletedUser)
    await flushPromises()
    expect(vm.mode).toBe("view")
    expect(vm.panel.deletedAt).toBeTruthy()
  })

  it("reste en mode view apres annulation de suppression", async () => {
    const wrapper = mount(AdminUsersView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockUsers.data.items[0])
    expect(vm.mode).toBe("delete")
    vm.openView(mockUsers.data.items[0])
    expect(vm.mode).toBe("view")
  })
})
