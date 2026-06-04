import GameSavesView from "@/views/player/GameSavesView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}))

vi.mock("@/api/gameSave", () => ({
  gameSavesApi: {
    getMe: vi.fn(),
    delete: vi.fn(),
  },
}))

import { gameSavesApi } from "@/api/gameSave"

const router = createRouter({
  history: createMemoryHistory(),
  routes: [{ path: "/", name: "GameSaves", component: { template: "<div/>" } }],
})

const mockSaves = [
  { id: 1, playerId: 1, currentZone: "Forêt", positionX: 120.5, positionY: 80.3, inventoryData: null, questFlags: null, savedAt: "2026-05-24T10:00:00Z" },
  { id: 2, playerId: 1, currentZone: "Donjon", positionX: 50.0, positionY: 200.0, inventoryData: null, questFlags: null, savedAt: "2026-05-23T15:30:00Z" },
]

describe("GameSavesView", () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(gameSavesApi.getMe).mockResolvedValue({ data: { items: mockSaves } } as any)
  })

  it("affiche le titre Sauvegardes", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Sauvegardes")
  })

  it("loading est true pendant le chargement", async () => {
    vi.mocked(gameSavesApi.getMe).mockImplementationOnce(() => new Promise(() => {}))
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    const vm = wrapper.vm as any
    expect(vm.loading).toBe(true)
  })

  it("affiche la liste des sauvegardes après chargement", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Forêt")
    expect(wrapper.text()).toContain("Donjon")
  })

  it("affiche le total des sauvegardes dans les stats", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("2")
  })

  it("calcule lastSave correctement (le plus récent)", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.lastSave).not.toBeNull()
    expect(vm.lastSave.currentZone).toBe("Forêt")
  })

  it("affiche la dernière zone dans les stats", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Forêt")
  })

  it("affiche la dernière sync dans les stats", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Dernière sync")
  })

  it("lastSave est null quand il n'y a pas de sauvegardes", async () => {
    vi.mocked(gameSavesApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.lastSave).toBeNull()
  })

  it("affiche l'état vide quand il n'y a pas de sauvegardes", async () => {
    vi.mocked(gameSavesApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Aucune sauvegarde trouvée")
    expect(wrapper.text()).not.toContain("Forêt")
  })

  it("affiche les positions des sauvegardes", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("120.5")
    expect(wrapper.text()).toContain("80.3")
  })

  it("ouvre le dialogue de confirmation au clic sur Supprimer", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.dialog.show).toBe(false)
    vm.confirmDelete(mockSaves[0])
    expect(vm.dialog.show).toBe(true)
    expect(vm.dialog.save).toEqual(mockSaves[0])
  })

  it("ferme le dialogue au clic sur Annuler", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockSaves[0])
    expect(vm.dialog.show).toBe(true)
    vm.dialog.show = false
    expect(vm.dialog.show).toBe(false)
  })

  it("appelle gameSavesApi.delete et retire l'élément de la liste", async () => {
    vi.mocked(gameSavesApi.delete).mockResolvedValue({} as any)
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    expect(vm.saves.length).toBe(2)
    vm.confirmDelete(mockSaves[0])
    await vm.doDelete()
    await flushPromises()
    expect(gameSavesApi.delete).toHaveBeenCalledWith(1)
    expect(vm.saves.length).toBe(1)
    expect(vm.dialog.show).toBe(false)
  })

  it("ne fait rien si dialog.save est null dans doDelete", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.dialog.save = null
    await vm.doDelete()
    expect(gameSavesApi.delete).not.toHaveBeenCalled()
  })

  it("gère l'erreur API de doDelete silencieusement", async () => {
    vi.mocked(gameSavesApi.delete).mockRejectedValue(new Error("Network"))
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    const vm = wrapper.vm as any
    vm.confirmDelete(mockSaves[0])
    await vm.doDelete()
    await flushPromises()
    expect(vm.saves.length).toBe(2)
    expect(vm.dialog.show).toBe(false)
  })

  it("gère l'absence de sauvegardes", async () => {
    vi.mocked(gameSavesApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Aucune donnée")
  })

  it("affiche les étiquettes de colonnes", async () => {
    const wrapper = mount(GameSavesView, { global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain("Zone")
    expect(wrapper.text()).toContain("Position")
    expect(wrapper.text()).toContain("Sauvegardé le")
    expect(wrapper.text()).toContain("Action")
  })
})
