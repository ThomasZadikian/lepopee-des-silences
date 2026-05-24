import LeaderboardView from "@/views/player/LeaderboardView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

vi.mock("@/api/auth", () => ({
    default: {
        get: vi.fn(),
        interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } }
    }
}))

import api from "@/api/auth"

const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/", name: "Leaderboard", component: { template: "<div/>" } }]
})

const mockLeaderboardResponse = (overrides = {}) => ({
    data: {
        items: [
            { rank: 1, characterName: "Kael",  level: 25, combatsWon: 298, totalDamageDealt: 145000, totalPlaytimeMinutes: 2880 },
            { rank: 2, characterName: "Elara", level: 18, combatsWon: 120, totalDamageDealt: 80000,  totalPlaytimeMinutes: 1440 },
        ],
        totalCount: 2,
        page:       1,
        pageSize:   50,
        totalPages: 1,
        ...overrides
    }
})

describe("LeaderboardView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("affiche le chargement au départ", () => { 
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        expect(wrapper.find(".v-progress-circular").exists()).toBe(true)
    })

    it("affiche le titre Classement", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Champions")
    })

    it("affiche les entrées du leaderboard", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Kael")
        expect(wrapper.text()).toContain("Elara")
    })

    it("affiche le rang correctement", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("◆")
    })

    it("affiche le total de joueurs", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("2")
    })

    it("affiche les options de tri", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Niveau")
        expect(wrapper.text()).toContain("Victoires")
    })

    it("change le tri au clic", async () => {
    vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
    const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
    await flushPromises()

    vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())

    // Cherche le div cliquable qui contient "Victoires" parmi les options de tri
    const sortDivs = wrapper.findAll("div[style*='cursor: pointer']")
    const sortBtn  = sortDivs.find(d => d.text().includes("Victoires"))
    if (sortBtn) await sortBtn.trigger("click")
    await flushPromises()

    expect(vi.mocked(api.get)).toHaveBeenLastCalledWith("/leaderboard", {
        params: { sortBy: "combatswon", page: 1 }
    })
    })

    it("n'affiche pas la pagination si une seule page", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).not.toContain("Précédent")
    })

    it("affiche la pagination si plusieurs pages", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse({ totalPages: 2 }))
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.text()).toContain("Précédent")
        expect(wrapper.text()).toContain("Suivant")
    })

    it("affiche la barre de recherche", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.find("input").exists()).toBe(true)
    })

    it("affiche un message si aucun résultat de recherche", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()

        const input = wrapper.find("input")
        await input.setValue("xxxxxx")
        await input.trigger("input")
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("xxxxxx")
    })

    it("gère l'erreur API silencieusement", async () => {
        vi.mocked(api.get).mockRejectedValue(new Error("Network error"))
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()
        expect(wrapper.find(".v-progress-circular").exists()).toBe(false)
    })

    it("appelle l'API de recherche avec le bon terme", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()

        vi.mocked(api.get).mockResolvedValue({
            data: { items: [{ rank: 1, characterName: "Kael", level: 25 }] }
        })

        const input = wrapper.find("input")
        await input.setValue("Ka")
        await input.trigger("input")

        await new Promise(resolve => setTimeout(resolve, 350))
        await flushPromises()

        expect(vi.mocked(api.get)).toHaveBeenCalledWith("/leaderboard/search", {
            params: { q: "Ka" }
        })
    })

    it("n'appelle pas l'API si le terme fait moins de 2 caractères", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()

        const callCount = vi.mocked(api.get).mock.calls.length

        const input = wrapper.find("input")
        await input.setValue("K")
        await input.trigger("input")

        await new Promise(resolve => setTimeout(resolve, 350))
        await flushPromises()

        expect(vi.mocked(api.get).mock.calls.length).toBe(callCount)
    })

    it("affiche les résultats de recherche", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()

        vi.mocked(api.get).mockResolvedValue({
            data: { items: [{ rank: 5, characterName: "Kael", level: 25 }] }
        })

        const input = wrapper.find("input")
        await input.setValue("Ka")
        await input.trigger("input")

        await new Promise(resolve => setTimeout(resolve, 350))
        await flushPromises()

        expect(wrapper.text()).toContain("Kael")
        expect(wrapper.text()).toContain("1 résultat")
    })

    it("affiche une erreur si caractères non autorisés", async () => {
        vi.mocked(api.get).mockResolvedValue(mockLeaderboardResponse())
        const wrapper = mount(LeaderboardView, { global: { plugins: [router] } })
        await flushPromises()

        const input = wrapper.find("input")
        await input.setValue("<script>")
        await input.trigger("input")
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("non autorisés")
    })
})