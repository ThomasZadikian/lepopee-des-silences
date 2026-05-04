import DashboardView from "@/views/player/DashboardView.vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

vi.mock("@/interfaces/playerProfile", () => ({
  playerProfileApi: { getMe: vi.fn() },
}));

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

import { playerProfileApi } from "@/interfaces/playerProfile";

const mockGetMe = vi.mocked(playerProfileApi.getMe);
const mockIsAdmin = vi.fn(() => false);
const mockUsername = vi.fn(() => "testuser");

const router = createRouter({
  history: createMemoryHistory(),
  routes: [
    { path: "/saves", name: "Saves", component: { template: "<div/>" } },
    {
      path: "/inventory",
      name: "Inventory",
      component: { template: "<div/>" },
    },
    { path: "/rgpd", name: "Rgpd", component: { template: "<div/>" } },
  ],
});

const stubs = {
  "v-container": { template: "<div><slot /></div>" },
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
  "v-card": { template: "<div><slot /></div>" },
  "v-card-item": { template: "<div><slot /></div>" },
  "v-card-title": { template: "<div><slot /></div>" },
  "v-card-subtitle": { template: "<div><slot /></div>" },
  "v-card-text": { template: "<div><slot /></div>" },
  "v-card-actions": { template: "<div><slot /></div>" },
  "v-avatar": { template: "<div><slot /></div>" },
  "v-icon": { template: "<span><slot /></span>" },
  "v-chip": { template: "<span><slot /></span>" },
  "v-btn": { template: "<button><slot /></button>" },
  "v-progress-circular": { template: "<div />" },
  "v-progress-linear": { template: "<div />" },
  "v-alert": { template: "<div><slot /></div>" },
};

const mockProfile = {
  id: 10,
  userId: 1,
  characterName: "Aragorn",
  level: 5,
  currentHP: 80,
  maxHP: 100,
  currentMP: 30,
  maxMP: 50,
  strength: 15,
  intelligence: 12,
  speed: 11,
  experience: 1250,
  gold: 300,
  updatedAt: "2026-04-30T10:00:00Z",
  totalCombats: 20,
  combatsWon: 15,
  combatsLost: 5,
  totalDamageDealt: 8000,
  totalDamageTaken: 3000,
  totalPlaytimeMinutes: 120,
  savesCount: 2,
  inventoryCount: 1,
  skillsCount: 3,
  bestiaryCount: 1,
};

describe("DashboardView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mockIsAdmin.mockReturnValue(false);
    mockUsername.mockReturnValue("testuser");
    mockGetMe.mockResolvedValue({ data: mockProfile } as any);
  });

  it("affiche le nom du personnage", async () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("Aragorn"); // ce qui est vraiment affiché
  });

  it("affiche le rôle Joueur pour un player", async () => {
    mockIsAdmin.mockReturnValue(false);
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("Joueur");
  });

  it("affiche le rôle Administrateur pour un admin", async () => {
    mockIsAdmin.mockReturnValue(true);
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("Admin.");
  });

  it("affiche le lien Voir mes sauvegardes", async () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("Sauvegardes");
  });

  it("affiche le lien Gérer mes données RGPD", async () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("Mes données");
  });

  it("affiche un avertissement si aucun personnage", async () => {
    mockGetMe.mockRejectedValue(new Error("404"));
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("jeu");
  });

  it("affiche le nom du personnage", async () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("Aragorn");
  });

  it("affiche le level du personnage", async () => {
    const wrapper = mount(DashboardView, {
      global: { plugins: [router], stubs },
    });
    await flushPromises();
    expect(wrapper.text()).toContain("5");
  });
});
