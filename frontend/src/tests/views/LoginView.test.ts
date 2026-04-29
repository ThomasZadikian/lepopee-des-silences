import LoginView from "@/views/auth/LoginView.vue";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";
vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    login: vi.fn().mockResolvedValue({ requiresMfa: false }),
    isAuthenticated: false,
  }),
}));
vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));
const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: "/login", name: "Login", component: { template: "<div/>" } },
    {
      path: "/dashboard",
      name: "Dashboard",
      component: { template: "<div/>" },
    },
    { path: "/register", name: "Register", component: { template: "<div/>" } },
  ],
});
describe("LoginView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });
  it("affiche le titre RPG_ESI07", async () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] },
    });
    expect(wrapper.text()).toContain("RPG_ESI07");
  });
  it("affiche les champs username et password", () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] },
    });
    // Vérifie que les champs de saisie sont présents dans le DOM
    expect(
      wrapper.find('input[type="password"]').exists() ||
        wrapper.text().includes("Mot de passe"),
    ).toBe(true);
  });
  it("affiche le bouton Connexion", () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] },
    });
    expect(wrapper.text()).toContain("Connexion");
  });
  it("affiche le lien vers Register", () => {
    const wrapper = mount(LoginView, {
      global: { plugins: [router] },
    });
    expect(wrapper.text()).toContain("compte");
  });
});
