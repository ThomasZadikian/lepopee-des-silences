import RegisterView from "@/views/auth/RegisterView.vue";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

const mockRegister = vi.fn();

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    register: mockRegister,
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
    { path: "/register", name: "Register", component: { template: "<div/>" } },
    { path: "/login", name: "Login", component: { template: "<div/>" } },
  ],
});

describe("RegisterView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre Créer un compte", () => {
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("compte");
  });

  it("affiche le champ username", () => {
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Nom d'utilisateur");
  });

  it("affiche le champ email", () => {
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Email");
  });

  it("affiche le champ mot de passe", () => {
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Mot de passe");
  });

  it("affiche le bouton S'inscrire", () => {
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("inscrire");
  });

  it("affiche le lien vers Login", () => {
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("compte");
  });

  it("appelle register au clic sur S'inscrire", async () => {
    mockRegister.mockResolvedValue(undefined);
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });

    const btn = wrapper
      .findAll("button")
      .find((b) => b.text().includes("inscrire"));
    if (btn) await btn.trigger("click");

    expect(mockRegister).toHaveBeenCalled();
  });

  it("affiche un message de succès après inscription", async () => {
    mockRegister.mockResolvedValue(undefined);
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });

    const btn = wrapper
      .findAll("button")
      .find((b) => b.text().includes("inscrire"));
    if (btn) await btn.trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("créé");
  });

  it("affiche une erreur si l'inscription échoue", async () => {
    mockRegister.mockRejectedValue({
      response: { data: { message: "Nom d'utilisateur déjà pris" } },
    });
    const wrapper = mount(RegisterView, { global: { plugins: [router] } });

    const btn = wrapper
      .findAll("button")
      .find((b) => b.text().includes("inscrire"));
    if (btn) await btn.trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("Nom d'utilisateur déjà pris");
  });
});
