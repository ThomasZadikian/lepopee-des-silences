import EnclosView from "@/views/player/EnclosView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/companion", () => ({
    companionApi: { getState: vi.fn() }
}))

vi.mock("@/api/auth", () => ({
    default: {
        interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } }
    }
}))

vi.mock("@/components/FoxSprite.vue", () => ({
    default: { template: "<canvas data-testid='fox-sprite'/>" }
}))

import { companionApi } from "@/api/companion"

const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/", name: "Enclos", component: { template: "<div/>" } }]
})

const mockState = (overrides = {}) => ({
    data: {
        currentState:   "REPOS",
        lastUpdated:    new Date().toISOString(),
        victoriesToday: 5,
        defeatesToday:  2,
        ...overrides
    }
})

describe("EnclosView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("affiche le titre Neige", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState() as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Neige")
    })

    it("affiche le composant FoxSprite", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState() as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.find("[data-testid='fox-sprite']").exists()).toBe(true)
    })

    it("affiche l'état actuel du companion", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState({ currentState: "EXCITE" }) as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Excitée")
    })

    it("affiche les victoires et défaites", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState({ victoriesToday: 42, defeatesToday: 7 }) as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("42")
        expect(wrapper.text()).toContain("7")
    })

    it("affiche le texte d'humeur selon l'état", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState({ currentState: "TRISTE" }) as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("défaites")
    })

    it("affiche la liste des états possibles", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState() as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Repos")
        expect(wrapper.text()).toContain("Jeu")
        expect(wrapper.text()).toContain("Dort")
    })

    it("affiche la dernière sync", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState() as any)
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Dernière sync")
    })

    it("gère l'erreur API silencieusement", async () => {
        vi.mocked(companionApi.getState).mockRejectedValue(new Error("Network error"))
        const wrapper = mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Neige")
    })

    it("appelle getState au montage", async () => {
        vi.mocked(companionApi.getState).mockResolvedValue(mockState() as any)
        mount(EnclosView, { global: { plugins: [router] } })
        await flushPromises()
        expect(vi.mocked(companionApi.getState)).toHaveBeenCalledTimes(1)
    })
})