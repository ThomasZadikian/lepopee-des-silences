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
    { path: "/admin/bestiary", name: "AdminBestiary", component: { template: "<div/>" } },
  ],
})

const mockItems = {
  data: { items: [
    { id: 1, name: "Épée", type: "weapon", category: null, description: "Une épée", price: 100, effectValue: 15, statModifiers: "{}" },
    { id: 2, name: "Potion", type: "consumable", category: "potion_hp", description: "Soigne", price: 25, effectValue: 50, statModifiers: "{}" },
  ]},
}

const newItem = { id: 3, name: "Bouclier", type: "armor", category: null, description: "Protège", price: 200, effectValue: 8, statModifiers: "{}" }

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
    expect(api.get).toHaveBeenCalledWith("/items")
  })

  it("gère l'état vide", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).not.toContain("Épée")
  })

  it("loading est true pendant le chargement", async () => {
    vi.mocked(api.get).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    const vm = wrapper.vm as any
    expect(vm.loading).toBe(true)
  })

  it("filtre les items par recherche textuelle", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("Potion")
    await flushPromises()
    expect(wrapper.text()).toContain("Potion")
    expect(wrapper.text()).not.toContain("Épée")
  })

  it("affiche 'Aucun résultat' quand le filtre ne matche rien", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("ZZZZZ")
    await flushPromises()
    expect(wrapper.text()).toContain("Aucun résultat")
  })

  it("ouvre le panneau détail", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockItems.data.items[0])
    await flushPromises()
    expect(vm.panel).not.toBeNull()
    expect(vm.mode).toBe("view")
    expect(wrapper.text()).toContain("Épée")
  })

  it("ouvre le panneau d'édition avec formulaire pré-rempli", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openEdit(mockItems.data.items[0])
    await flushPromises()
    expect(vm.mode).toBe("edit")
    expect(vm.form.name).toBe("Épée")
    expect(wrapper.text()).toContain("Modifier")
  })

  it("ouvre le panneau de création", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    await flushPromises()
    expect(vm.mode).toBe("create")
    expect(vm.form.name).toBe("")
    expect(wrapper.text()).toContain("Nouvel objet")
  })

  it("ouvre le panneau de confirmation de suppression", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockItems.data.items[0])
    await flushPromises()
    expect(vm.mode).toBe("delete")
    expect(wrapper.text()).toContain("Supprimer")
  })

  it("ferme le panneau", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockItems.data.items[0])
    expect(vm.panel).not.toBeNull()
    vm.closePanel()
    expect(vm.panel).toBeNull()
    expect(vm.formError).toBe("")
  })

  it("crée un item via POST", async () => {
    vi.mocked(api.post).mockResolvedValue({ data: newItem } as any)
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Bouclier"
    await vm.save()
    await flushPromises()
    expect(api.post).toHaveBeenCalledWith("/items", expect.objectContaining({ name: "Bouclier" }))
    expect(vm.items).toContainEqual(expect.objectContaining({ name: "Bouclier" }))
  })

  it("édite un item via PUT", async () => {
    vi.mocked(api.put).mockResolvedValue({} as any)
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openEdit(mockItems.data.items[0])
    vm.form.name = "Épée longue"
    await vm.save()
    await flushPromises()
    expect(api.put).toHaveBeenCalledWith("/items/1", expect.objectContaining({ name: "Épée longue" }))
    expect(vm.items[0].name).toBe("Épée longue")
  })

  it("valide que le nom est requis avant save", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
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
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Test"
    await vm.save()
    await flushPromises()
    expect(vm.formError).toBe("Une erreur est survenue.")
  })

  it("supprime un item via DELETE", async () => {
    vi.mocked(api.delete).mockResolvedValue({} as any)
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockItems.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith("/items/1")
    expect(vm.items.find((i: any) => i.id === 1)).toBeUndefined()
  })

  it("gère l'erreur API lors de la suppression", async () => {
    vi.mocked(api.delete).mockRejectedValue(new Error("Network"))
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockItems.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(vm.formError).toBe("Erreur lors de la suppression.")
  })

  it("affiche les statistiques du catalogue", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Total")
    expect(wrapper.text()).toContain("Armes")
    expect(wrapper.text()).toContain("Consommables")
  })

  it("calcule correctement les stats", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.stats[1].label).toBe("Armes")
    expect(vm.stats[2].label).toBe("Armures")
    expect(vm.stats[3].label).toBe("Consommables")
    expect(vm.stats[1].value).toBe(1)
  })

  it("affiche les tabs de navigation admin", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Utilisateurs")
    expect(wrapper.text()).toContain("Objets")
    expect(wrapper.text()).toContain("Compétences")
    expect(wrapper.text()).toContain("Bestiaire")
  })

  it("retourne la couleur correcte selon le type", async () => {
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.typeColor("weapon")).toBe("#C0392B")
    expect(vm.typeColor("armor")).toBe("#2D4A8A")
    expect(vm.typeColor("accessory")).toBe("#D68910")
    expect(vm.typeColor("consumable")).toBe("#1E8449")
    expect(vm.typeColor("unknown")).toBe("var(--rpg-ink-muted)")
  })

  it("saving désactive le bouton", async () => {
    vi.mocked(api.post).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminItemsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Test"
    vm.saving = true
    await flushPromises()
    expect(wrapper.text()).toContain("Enregistrement")
  })
})
