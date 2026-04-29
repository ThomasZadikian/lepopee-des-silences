import AppLayout from "@/layouts/AppLayout.vue";
import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

const mockLogout = vi.fn();
const mockIsAdmin = vi.fn(() => false);

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    username: "testuser",
    logout: mockLogout,
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
    { path: "/", name: "Dashboard", component: { template: "<div/>" } },
    { path: "/saves", name: "Saves", component: { template: "<div/>" } },
    {
      path: "/inventory",
      name: "Inventory",
      component: { template: "<div/>" },
    },
    { path: "/skills", name: "Skills", component: { template: "<div/>" } },
    { path: "/rgpd", name: "Rgpd", component: { template: "<div/>" } },
    {
      path: "/admin/users",
      name: "AdminUsers",
      component: { template: "<div/>" },
    },
    {
      path: "/admin/items",
      name: "AdminItems",
      component: { template: "<div/>" },
    },
  ],
});

describe("AppLayout", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mockIsAdmin.mockReturnValue(false);
  });

  // ── Navbar ────────────────────────────────────────────────────────────────

  it("affiche le titre RPG_ESI07", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("RPG_ESI07");
  });

  it("affiche le nom d'utilisateur connecté", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("testuser");
  });

  it("appelle logout au clic sur le bouton déconnexion", async () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    const logoutBtn = wrapper
      .findAll("button")
      .find(
        (b) => b.html().includes("mdi-logout") || b.text().includes("logout"),
      );
    if (logoutBtn) await logoutBtn.trigger("click");
    expect(mockLogout).toHaveBeenCalled();
  });

  // ── Sidebar joueur ────────────────────────────────────────────────────────

  it("affiche le lien Dashboard", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Dashboard");
  });

  it("affiche le lien Sauvegardes", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Sauvegardes");
  });

  it("affiche le lien Inventaire", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Inventaire");
  });

  it("affiche le lien Compétences", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Compétences");
  });

  it("affiche le lien RGPD", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("RGPD");
  });

  // ── Section Admin conditionnelle ──────────────────────────────────────────

  it("n'affiche pas la section Admin pour un joueur normal", () => {
    mockIsAdmin.mockReturnValue(false);
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).not.toContain("Administration");
    expect(wrapper.text()).not.toContain("Utilisateurs");
  });

  it("affiche la section Admin pour un Admin", () => {
    mockIsAdmin.mockReturnValue(true);
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    expect(wrapper.text()).toContain("Administration");
    expect(wrapper.text()).toContain("Utilisateurs");
    expect(wrapper.text()).toContain("Items");
  });

  // ── Drawer ────────────────────────────────────────────────────────────────

  it("le drawer est fermé par défaut", () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    const vm = wrapper.vm as any;
    expect(vm.drawer).toBe(false);
  });

  it("le drawer bascule au clic sur le bouton menu", async () => {
    const wrapper = mount(AppLayout, { global: { plugins: [router] } });
    const vm = wrapper.vm as any;
    const navIcon = wrapper.find("button");
    await navIcon.trigger("click");
    expect(vm.drawer).toBe(true);
  });
});
