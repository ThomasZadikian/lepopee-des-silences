// src/tests/views/MfaSetupView.test.ts
import MfaSetupView from "@/views/auth/MfaSetupView.vue";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

const mockSetupMfa       = vi.fn();
const mockVerifyMfaSetup = vi.fn();
const mockPush           = vi.fn();

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    setupMfa:       mockSetupMfa,
    verifyMfaSetup: mockVerifyMfaSetup,
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
    { path: "/mfa/setup",  name: "MfaSetup",  component: { template: "<div/>" } },
    { path: "/dashboard",  name: "Dashboard",  component: { template: "<div/>" } },
  ],
});

router.push = mockPush as any;

describe("MfaSetupView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche l'étape 1 au chargement", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" });

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("Étape 1");
  });

it("affiche le QR code après chargement réussi", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" })

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } })
    await wrapper.vm.$nextTick()
    await wrapper.vm.$nextTick()

    const canvas = wrapper.find("canvas")
    expect(canvas.exists()).toBe(true)
})

  it("affiche la clé manuelle après chargement réussi", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" });

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("ABCD1234");
  });

  it("affiche une erreur si setupMfa échoue", async () => {
    mockSetupMfa.mockRejectedValue(new Error("Erreur serveur"));

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("Impossible");
  });

  it("passe à l'étape 2 au clic sur le bouton scanner", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" });

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    const btn = wrapper.findAll("button").find(b => b.text().includes("scanné"));
    if (btn) await btn.trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("Étape 2");
  });

  it("appelle verifyMfaSetup avec le code saisi", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" });
    mockVerifyMfaSetup.mockResolvedValue(undefined);

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    // Passer à l'étape 2
    const btnStep = wrapper.findAll("button").find(b => b.text().includes("scanné"));
    if (btnStep) await btnStep.trigger("click");
    await wrapper.vm.$nextTick();

    // Saisir le code
    const input = wrapper.find("input");
    await input.setValue("123456");

    // Valider
    const btnVerify = wrapper.findAll("button").find(b => b.text().includes("Codex"));
    if (btnVerify) await btnVerify.trigger("click");
    await wrapper.vm.$nextTick();

    expect(mockVerifyMfaSetup).toHaveBeenCalledWith("123456");
  });

  it("affiche une erreur si le code est incorrect", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" });
    mockVerifyMfaSetup.mockRejectedValue(new Error("Code invalide"));

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    const btnStep = wrapper.findAll("button").find(b => b.text().includes("scanné"));
    if (btnStep) await btnStep.trigger("click");
    await wrapper.vm.$nextTick();

    const input = wrapper.find("input");
    await input.setValue("000000");

    const btnVerify = wrapper.findAll("button").find(b => b.text().includes("Codex"));
    if (btnVerify) await btnVerify.trigger("click");
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("incorrect");
  });

  it("retourne à l'étape 1 au clic sur Retour", async () => {
    mockSetupMfa.mockResolvedValue({ qrCodeUri: "otpauth://totp/test", secret: "ABCD1234" });

    const wrapper = mount(MfaSetupView, { global: { plugins: [router] } });
    await wrapper.vm.$nextTick();
    await wrapper.vm.$nextTick();

    const btnStep = wrapper.findAll("button").find(b => b.text().includes("scanné"));
    if (btnStep) await btnStep.trigger("click");
    await wrapper.vm.$nextTick();

    const btnBack = wrapper.findAll("button").find(b => b.text().includes("Retour"));
    if (btnBack) await btnBack.trigger("click");
    await wrapper.vm.$nextTick();

    expect(wrapper.text()).toContain("Étape 1");
  });
});