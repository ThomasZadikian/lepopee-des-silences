// src/tests/views/MfaView.test.ts
import MfaView from "@/views/auth/MfaView.vue";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

const mockVerifyMfa = vi.fn();
const mockPush = vi.fn();

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    verifyMfa: mockVerifyMfa,
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
    { path: "/mfa", name: "Mfa", component: { template: "<div/>" } },
    {
      path: "/dashboard",
      name: "Dashboard",
      component: { template: "<div/>" },
    },
  ],
});

router.push = mockPush as any;

describe("MfaView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre Vérification MFA", () => {
    const wrapper = mount(MfaView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("MFA");
  });

  it("affiche le champ de saisie du code", () => {
    const wrapper = mount(MfaView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("6 chiffres");
  });

  it("affiche le bouton Valider", () => {
    const wrapper = mount(MfaView, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Valider");
  });

  it("appelle verifyMfa au clic sur Valider", async () => {
    mockVerifyMfa.mockResolvedValue(undefined);
    const wrapper = mount(MfaView, { global: { plugins: [router] } });

    const btn = wrapper
      .findAll("button")
      .find((b) => b.text().includes("Valider"));
    if (btn) await btn.trigger("click");

    expect(mockVerifyMfa).toHaveBeenCalled();
  });

  it("affiche une erreur si le code est incorrect", async () => {
    mockVerifyMfa.mockRejectedValue(new Error("Code invalide"));
    const wrapper = mount(MfaView, { global: { plugins: [router] } });

    const btn = wrapper
      .findAll("button")
      .find((b) => b.text().includes("Valider"));
    if (btn) await btn.trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("incorrect");
  });
});
