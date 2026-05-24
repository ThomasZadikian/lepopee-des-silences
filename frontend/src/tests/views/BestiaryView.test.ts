import BestiaryView from "@/views/player/BestiaryView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/bestiary", () => ({
    bestiaryApi: { getMe: vi.fn() }
}))

vi.mock("@/api/auth", () => ({
    default: {
        interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } }
    }
}))

import { bestiaryApi } from "@/api/bestiary"

const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/", name: "Bestiary", component: { template: "<div/>" } }]
})

const mockEnemy = (overrides = {}) => ({
    id: 1,
    enemy: {
        id: 1, name: "Rat Géant", type: "basic", description: "Un rat géant",
        maxHP: 30, strength: 6, intelligence: 2, speed: 14,
        physicalResistance: 1.0, magicalResistance: 1.0,
        experienceReward: 10, goldReward: 3,
        ...overrides
    }
})

describe("BestiaryView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("affiche le chargement au départ", () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        expect(wrapper.find(".v-progress-circular").exists()).toBe(true)
    })

    it("affiche le titre Bestiaire", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Bestiaire")
    })

    it("affiche le message vide si aucune entrée", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [] } } as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Bestiaire vide")
    })

    it("affiche le nombre d'entrées", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [mockEnemy(), mockEnemy()] } } as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("2")
    })

    it("affiche le nombre de boss vaincus", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [
            mockEnemy({ type: "boss", name: "Dragon" }),
            mockEnemy({ type: "basic" }),
        ]}} as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("1")
    })

    it("affiche la créature la plus forte", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [
            mockEnemy({ maxHP: 30,  name: "Rat Géant" }),
            mockEnemy({ maxHP: 900, name: "Dragon de l'Aube" }),
        ]}} as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Dragon de l'Aube")
    })

    it("affiche le panneau détail au clic sur une entrée", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [mockEnemy()] } } as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()

        const row = wrapper.find(".editorial-row")
        if (row.exists()) await row.trigger("click")
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("Rat Géant")
    })

    it("ferme le panneau détail au second clic", async () => {
        vi.mocked(bestiaryApi.getMe).mockResolvedValue({ data: { items: [mockEnemy()] } } as any)
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()

        const row = wrapper.find(".editorial-row")
        if (row.exists()) {
            await row.trigger("click")
            await wrapper.vm.$nextTick()
            await row.trigger("click")
            await wrapper.vm.$nextTick()
        }

        expect(wrapper.findAll(".editorial-row").length).toBe(1)
    })

    it("gère l'erreur API silencieusement", async () => {
        vi.mocked(bestiaryApi.getMe).mockRejectedValue(new Error("Network error"))
        const wrapper = mount(BestiaryView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Bestiaire vide")
    })
})