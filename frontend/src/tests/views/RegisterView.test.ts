import RegisterView from "@/views/auth/RegisterView.vue"
import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

const mockRegister = vi.fn()

vi.mock("@/stores/auth", () => ({
    useAuthStore: () => ({
        register: mockRegister,
    }),
}))

vi.mock("@/api/auth", () => ({
    default: {
        interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
    },
}))

const router = createRouter({
    history: createMemoryHistory(),
    routes: [
        { path: "/register",  name: "Register",  component: { template: "<div/>" } },
        { path: "/login",     name: "Login",     component: { template: "<div/>" } },
        { path: "/mfa/setup", name: "MfaSetup",  component: { template: "<div/>" } },
    ],
})

describe("RegisterView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("affiche le titre", () => {
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("compte")
    })

    it("affiche le champ username", () => {
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("Nom de marcheur")
    })

    it("affiche le champ email", () => {
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("Adresse email")
    })

    it("affiche le champ mot de passe", () => {
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("Mot de passe")
    })

    it("affiche le bouton d'inscription", () => {
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("Créer mon compte")
    })

    it("appelle register au clic", async () => {
        mockRegister.mockResolvedValue(undefined)
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("Créer mon compte"))
        if (btn) await btn.trigger("click")
        await flushPromises()

        expect(mockRegister).toHaveBeenCalled()
    })

    it("affiche un message de succès après inscription", async () => {
        mockRegister.mockResolvedValue(undefined)
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("Créer mon compte"))
        if (btn) await btn.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("Compte créé")
    })

    it("affiche une erreur si l'inscription échoue", async () => {
        mockRegister.mockRejectedValue({
            response: { data: { message: "Nom d'utilisateur déjà pris" } },
        })
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("Créer mon compte"))
        if (btn) await btn.trigger("click")
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("déjà pris")
    })

    it("register est appelé avec les bonnes données", async () => {
        mockRegister.mockResolvedValue(undefined)
        const wrapper = mount(RegisterView, { global: { plugins: [router] } })

        const inputs = wrapper.findAll("input")
        if (inputs[0]) await inputs[0].setValue("testuser")
        if (inputs[1]) await inputs[1].setValue("test@test.com")
        if (inputs[2]) await inputs[2].setValue("Password123!")

        const btn = wrapper.findAll("button").find(b => b.text().includes("Créer mon compte"))
        if (btn) await btn.trigger("click")
        await flushPromises()

        expect(mockRegister).toHaveBeenCalledWith({
            username: "testuser",
            email:    "test@test.com",
            password: "Password123!"
        })
    })
})