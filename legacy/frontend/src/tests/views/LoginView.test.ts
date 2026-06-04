import LoginView from "@/views/auth/LoginView.vue"
import { mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { beforeEach, describe, expect, it, vi } from "vitest"
import { createMemoryHistory, createRouter } from "vue-router"

const mockLogin = vi.fn()
const mockPush  = vi.fn()

vi.mock("@/stores/auth", () => ({
    useAuthStore: () => ({
        login:           mockLogin,
        isAuthenticated: false,
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
        { path: "/login",     name: "Login",     component: { template: "<div/>" } },
        { path: "/dashboard", name: "Dashboard", component: { template: "<div/>" } },
        { path: "/register",  name: "Register",  component: { template: "<div/>" } },
        { path: "/mfa",       name: "Mfa",       component: { template: "<div/>" } },
        { path: "/mfa/setup", name: "MfaSetup",  component: { template: "<div/>" } },
    ],
})

router.push = mockPush as any

describe("LoginView", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("affiche le titre RPG_ESI07", () => {
        const wrapper = mount(LoginView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("RPG_ESI07")
    })

    it("affiche les champs username et password", () => {
        const wrapper = mount(LoginView, { global: { plugins: [router] } })
        expect(wrapper.find('input[type="password"]').exists() || wrapper.text().includes("Mot de passe")).toBe(true)
    })

    it("affiche le bouton Connexion", () => {
        const wrapper = mount(LoginView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("Connexion")
    })

    it("affiche le lien vers Register", () => {
        const wrapper = mount(LoginView, { global: { plugins: [router] } })
        expect(wrapper.text()).toContain("compte")
    })

    it("redirige vers Dashboard si login réussi sans MFA", async () => {
        mockLogin.mockResolvedValue({ requiresMfa: false, requiresMfaSetup: false })
        const wrapper = mount(LoginView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("veille"))
        if (btn) await btn.trigger("click")
        await wrapper.vm.$nextTick()

        expect(mockPush).toHaveBeenCalledWith({ name: "Dashboard" })
    })

    it("redirige vers Mfa si MFA requis", async () => {
        mockLogin.mockResolvedValue({ requiresMfa: true, requiresMfaSetup: false })
        const wrapper = mount(LoginView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("veille"))
        if (btn) await btn.trigger("click")
        await wrapper.vm.$nextTick()

        expect(mockPush).toHaveBeenCalledWith({ name: "Mfa" })
    })

    it("redirige vers MfaSetup si setup MFA requis", async () => {
        mockLogin.mockResolvedValue({ requiresMfa: false, requiresMfaSetup: true })
        const wrapper = mount(LoginView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("veille"))
        if (btn) await btn.trigger("click")
        await wrapper.vm.$nextTick()

        expect(mockPush).toHaveBeenCalledWith({ name: "MfaSetup" })
    })

    it("affiche une erreur si le login échoue", async () => {
        mockLogin.mockRejectedValue({ response: { data: { message: "Identifiants incorrects." } } })
        const wrapper = mount(LoginView, { global: { plugins: [router] } })

        const btn = wrapper.findAll("button").find(b => b.text().includes("veille"))
        if (btn) await btn.trigger("click")
        await wrapper.vm.$nextTick()
        await wrapper.vm.$nextTick()

        expect(wrapper.text()).toContain("Identifiants")
    })
})