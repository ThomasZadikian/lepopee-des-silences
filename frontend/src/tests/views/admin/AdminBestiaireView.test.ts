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

vi.mock("@/api/enemies", () => ({
  enemiesApi: { getAll: vi.fn() },
}))

import api from "@/api/auth"
import { enemiesApi } from "@/api/enemies"

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: "/", name: "AdminBestiary", component: { template: "<div/>" } },
    { path: "/admin/users", name: "AdminUsers", component: { template: "<div/>" } },
  ],
})

const mockEnemies = {
  data: { items: [
    { id: 1, name: "Goblin", type: "basic", maxHP: 50, strength: 8, intelligence: 3, speed: 12, physicalResistance: 1, magicalResistance: 1, experienceReward: 10, goldReward: 5, description: "Petit gobelin", initialState: "REPOS", influenceRadius: 5 },
    { id: 2, name: "Dragon", type: "boss", maxHP: 900, strength: 50, intelligence: 40, speed: 30, physicalResistance: 1.5, magicalResistance: 1.5, experienceReward: 500, goldReward: 300, description: "Dragon ancien", initialState: "CHASSE", influenceRadius: 15 },
  ]},
}

const newEnemy = { id: 3, name: "Slime", type: "basic", maxHP: 20, strength: 3, intelligence: 1, speed: 4, physicalResistance: 1, magicalResistance: 1, experienceReward: 5, goldReward: 2, description: "", initialState: "REPOS", influenceRadius: 3 }

describe("AdminBestiaireView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(enemiesApi.getAll).mockResolvedValue(mockEnemies as any)
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

  it("appelle enemiesApi.getAll au montage", async () => {
    mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(enemiesApi.getAll).toHaveBeenCalled()
  })

  it("gère l'état vide", async () => {
    vi.mocked(enemiesApi.getAll).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).not.toContain("Goblin")
  })

  it("loading est true pendant le chargement", async () => {
    vi.mocked(enemiesApi.getAll).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    const vm = wrapper.vm as any
    expect(vm.loading).toBe(true)
  })

  it("filtre les ennemis par recherche textuelle", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("Dragon")
    await flushPromises()
    expect(wrapper.text()).toContain("Dragon")
    expect(wrapper.text()).not.toContain("Goblin")
  })

  it("affiche 'Aucun résultat' quand le filtre ne matche rien", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("ZZZZZZ")
    await flushPromises()
    expect(wrapper.text()).toContain("Aucun résultat")
  })

  it("ouvre le panneau détail au clic sur Voir", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockEnemies.data.items[0])
    await flushPromises()
    expect(vm.panel).not.toBeNull()
    expect(vm.mode).toBe("view")
    expect(wrapper.text()).toContain("Goblin")
  })

  it("ouvre le panneau d'édition avec le formulaire pré-rempli", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openEdit(mockEnemies.data.items[0])
    await flushPromises()
    expect(vm.mode).toBe("edit")
    expect(vm.form.name).toBe("Goblin")
    expect(wrapper.text()).toContain("Modifier")
  })

  it("ouvre le panneau de création", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    await flushPromises()
    expect(vm.mode).toBe("create")
    expect(vm.form.name).toBe("")
    expect(wrapper.text()).toContain("Nouveau monstre")
  })

  it("ouvre le panneau de confirmation de suppression", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockEnemies.data.items[1])
    await flushPromises()
    expect(vm.mode).toBe("delete")
    expect(wrapper.text()).toContain("Supprimer")
    expect(wrapper.text()).toContain("Dragon")
  })

  it("ferme le panneau", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockEnemies.data.items[0])
    expect(vm.panel).not.toBeNull()
    vm.closePanel()
    expect(vm.panel).toBeNull()
    expect(vm.formError).toBe("")
  })

  it("crée un ennemi via POST", async () => {
    vi.mocked(api.post).mockResolvedValue({ data: newEnemy } as any)
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Slime"
    await vm.save()
    await flushPromises()
    expect(api.post).toHaveBeenCalledWith("/enemies", expect.objectContaining({ name: "Slime" }))
    expect(vm.enemies).toContainEqual(expect.objectContaining({ name: "Slime" }))
  })

  it("édite un ennemi via PUT", async () => {
    vi.mocked(api.put).mockResolvedValue({} as any)
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openEdit(mockEnemies.data.items[0])
    vm.form.name = "Goblin Elite"
    await vm.save()
    await flushPromises()
    expect(api.put).toHaveBeenCalledWith("/enemies/1", expect.objectContaining({ name: "Goblin Elite" }))
    expect(vm.enemies[0].name).toBe("Goblin Elite")
  })

  it("valide que le nom est requis avant save", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = ""
    await vm.save()
    expect(vm.formError).toBe("Le nom est requis.")
    expect(api.post).not.toHaveBeenCalled()
  })

  it("gère l'erreur API lors de la sauvegarde", async () => {
    vi.mocked(api.post).mockRejectedValue(new Error("Network"))
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Test"
    await vm.save()
    await flushPromises()
    expect(vm.formError).toBe("Une erreur est survenue.")
  })

  it("supprime un ennemi via DELETE", async () => {
    vi.mocked(api.delete).mockResolvedValue({} as any)
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockEnemies.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith("/enemies/1")
    expect(vm.enemies.find((e: any) => e.id === 1)).toBeUndefined()
  })

  it("gère l'erreur API lors de la suppression", async () => {
    vi.mocked(api.delete).mockRejectedValue(new Error("Network"))
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockEnemies.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(vm.formError).toBe("Erreur lors de la suppression.")
  })

  it("affiche les statistiques du bestiaire", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Total")
    expect(wrapper.text()).toContain("Boss")
    expect(wrapper.text()).toContain("Communs")
    expect(wrapper.text()).toContain("XP total")
  })

  it("calcule correctement les stats", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.stats[1].label).toBe("Boss")
    expect(vm.stats[3].label).toBe("Communs")
    expect(vm.stats[4].label).toBe("XP total")
    expect(typeof vm.stats[0].value).toBe("number")
    expect(vm.stats[1].value).toBe(1)
  })

  it("affiche les tabs de navigation admin", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Utilisateurs")
    expect(wrapper.text()).toContain("Objets")
    expect(wrapper.text()).toContain("Compétences")
    expect(wrapper.text()).toContain("Bestiaire")
  })

  it("saving désactive le bouton pendant l'enregistrement", async () => {
    vi.mocked(api.post).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Test"
    vm.saving = true
    await flushPromises()
    expect(wrapper.text()).toContain("Enregistrement")
  })

  it("retourne le style correct selon le type", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.typeStyle("basic")).toContain("rpg-ink-muted")
    expect(vm.typeStyle("boss")).toContain("background:var(--rpg-ink)")
    expect(vm.typeStyle("miniboss")).toContain("#D68910")
    expect(vm.typeStyle("unknown")).toBe("")
  })

  it("closePanel réinitialise les erreurs", async () => {
    const wrapper = mount(AdminBestiaireView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.formError = "Erreur test"
    vm.closePanel()
    expect(vm.formError).toBe("")
    expect(vm.panel).toBeNull()
  })
})
