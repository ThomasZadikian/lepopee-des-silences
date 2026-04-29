import DashboardView from "@/views/player/DashboardView.vue";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

const mockIsAdmin = vi.fn(() => false);
const mockUsername = vi.fn(() => "testuser");

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    get username() {
      return mockUsername();
    },
    get isAdmin() {
      return mockIsAdmin();
    },
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
    { path: "/saves", name: "Saves", component: { template: "<div/>" } },
    { path: "/gdpr", name: "Gdpr", component: { template: "<div/>" } },
  ],
});

const stubs = {
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
  "v-card": { template: "<div><slot /></div>" },
  "v-card-title": { template: "<div><slot /></div>" },
  "v-card-text": { template: "<div><slot /></div>" },
  "v-card-actions": { template: "<div><slot /></div>" },
  "v-chip": { template: "<span><slot /></span>" },
  "v-icon": { template: "<span><slot /></span>" },
  "v-btn": { template: "<button><slot /></button>" },
};

describe("DashboardView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mockIsAdmin.mockReturnValue(false);
    mockUsername.mockReturnValue("testuser");
  });

  it("affiche le titre Tableau de bord", () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    expect(wrapper.text()).toContain("Tableau de bord");
  });

  it("affiche le nom d'utilisateur", () => {
    mockUsername.mockReturnValue("thomas");
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    expect(wrapper.text()).toContain("thomas");
  });

  it("affiche le rôle Joueur pour un player", () => {
    mockIsAdmin.mockReturnValue(false);
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    expect(wrapper.text()).toContain("Joueur");
  });

  it("affiche le rôle Administrateur pour un admin", () => {
    mockIsAdmin.mockReturnValue(true);
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    expect(wrapper.text()).toContain("Administrateur");
  });

  it("affiche le lien Voir mes sauvegardes", () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    expect(wrapper.text()).toContain("sauvegardes");
  });

  it("affiche le lien Gérer mes données RGPD", () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    expect(wrapper.text()).toContain("RGPD");
  });
});
