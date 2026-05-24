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
    { path: "/admin/items", name: "AdminItems", component: { template: "<div/>" } },
  ],
})

const mockSkills = {
  data: { items: [
    { id: 1, name: "Boule de feu", mpCost: 15, baseDamage: 80, healAmount: 0, effectType: "damage", elementType: "fire", description: "Inflige des dégâts de feu" },
    { id: 2, name: "Soin", mpCost: 10, baseDamage: 0, healAmount: 50, effectType: "heal", elementType: "neutral", description: "Soigne les blessures" },
  ]},
}

const newSkill = { id: 3, name: "Rafale", mpCost: 12, baseDamage: 60, healAmount: 0, effectType: "damage", elementType: "ice", description: "" }

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
    expect(api.get).toHaveBeenCalledWith("/skills")
  })

  it("gère l'état vide", async () => {
    vi.mocked(api.get).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).not.toContain("Boule de feu")
  })

  it("loading est true pendant le chargement", async () => {
    vi.mocked(api.get).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    const vm = wrapper.vm as any
    expect(vm.loading).toBe(true)
  })

  it("filtre les compétences par recherche textuelle", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("Soin")
    await flushPromises()
    expect(wrapper.text()).toContain("Soin")
    expect(wrapper.text()).not.toContain("Boule")
  })

  it("filtre par type d'effet", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("heal")
    await flushPromises()
    expect(wrapper.text()).toContain("Soin")
    expect(wrapper.text()).not.toContain("Boule")
  })

  it("affiche 'Aucun résultat' quand le filtre ne matche rien", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const input = wrapper.find("input[type='text']")
    await input.setValue("ZZZZZ")
    await flushPromises()
    expect(wrapper.text()).toContain("Aucun résultat")
  })

  it("ouvre le panneau détail", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockSkills.data.items[0])
    await flushPromises()
    expect(vm.panel).not.toBeNull()
    expect(vm.mode).toBe("view")
    expect(wrapper.text()).toContain("Boule de feu")
  })

  it("ouvre le panneau d'édition avec formulaire pré-rempli", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openEdit(mockSkills.data.items[0])
    await flushPromises()
    expect(vm.mode).toBe("edit")
    expect(vm.form.name).toBe("Boule de feu")
    expect(vm.form.mpCost).toBe(15)
    expect(wrapper.text()).toContain("Modifier")
  })

  it("ouvre le panneau de création", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    await flushPromises()
    expect(vm.mode).toBe("create")
    expect(vm.form.name).toBe("")
    expect(wrapper.text()).toContain("Nouvelle compétence")
  })

  it("ouvre le panneau de confirmation de suppression", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockSkills.data.items[1])
    await flushPromises()
    expect(vm.mode).toBe("delete")
    expect(wrapper.text()).toContain("Supprimer")
    expect(wrapper.text()).toContain("Soin")
  })

  it("ferme le panneau", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openView(mockSkills.data.items[0])
    expect(vm.panel).not.toBeNull()
    vm.closePanel()
    expect(vm.panel).toBeNull()
    expect(vm.formError).toBe("")
  })

  it("crée une compétence via POST", async () => {
    vi.mocked(api.post).mockResolvedValue({ data: newSkill } as any)
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Rafale"
    await vm.save()
    await flushPromises()
    expect(api.post).toHaveBeenCalledWith("/skills", expect.objectContaining({ name: "Rafale" }))
    expect(vm.skills).toContainEqual(expect.objectContaining({ name: "Rafale" }))
  })

  it("édite une compétence via PUT", async () => {
    vi.mocked(api.put).mockResolvedValue({} as any)
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openEdit(mockSkills.data.items[0])
    vm.form.mpCost = 20
    await vm.save()
    await flushPromises()
    expect(api.put).toHaveBeenCalledWith("/skills/1", expect.objectContaining({ mpCost: 20 }))
    expect(vm.skills[0].mpCost).toBe(20)
  })

  it("valide que le nom est requis avant save", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
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
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Test"
    await vm.save()
    await flushPromises()
    expect(vm.formError).toBe("Une erreur est survenue.")
  })

  it("supprime une compétence via DELETE", async () => {
    vi.mocked(api.delete).mockResolvedValue({} as any)
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockSkills.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith("/skills/1")
    expect(vm.skills.find((s: any) => s.id === 1)).toBeUndefined()
  })

  it("gère l'erreur API lors de la suppression", async () => {
    vi.mocked(api.delete).mockRejectedValue(new Error("Network"))
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockSkills.data.items[0])
    await vm.executeDelete()
    await flushPromises()
    expect(vm.formError).toBe("Erreur lors de la suppression.")
  })

  it("affiche les statistiques des compétences", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Total")
    expect(wrapper.text()).toContain("Attaque")
    expect(wrapper.text()).toContain("Soin")
    expect(wrapper.text()).toContain("Buff")
  })

  it("calcule correctement les stats", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.stats[0].label).toBe("Total")
    expect(vm.stats[1].label).toBe("Attaque")
    expect(vm.stats[2].label).toBe("Soin")
    expect(vm.stats[3].label).toBe("Buff/Debuff")
    expect(vm.stats[1].value).toBeGreaterThanOrEqual(1)
    expect(vm.stats[2].value).toBeGreaterThanOrEqual(1)
  })

  it("affiche les tabs de navigation admin", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Utilisateurs")
    expect(wrapper.text()).toContain("Objets")
    expect(wrapper.text()).toContain("Compétences")
    expect(wrapper.text()).toContain("Bestiaire")
  })

  it("retourne la couleur correcte selon le type d'effet", async () => {
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.effectColor("damage")).toBe("#C0392B")
    expect(vm.effectColor("heal")).toBe("#1E8449")
    expect(vm.effectColor("buff")).toBe("#D68910")
    expect(vm.effectColor("debuff")).toBe("#2D4A8A")
    expect(vm.effectColor("unknown")).toBe("var(--rpg-ink-muted)")
  })

  it("saving désactive le bouton", async () => {
    vi.mocked(api.post).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(AdminSkillsView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.openCreate()
    vm.form.name = "Test"
    vm.saving = true
    await flushPromises()
    expect(wrapper.text()).toContain("Enregistrement")
  })
})
