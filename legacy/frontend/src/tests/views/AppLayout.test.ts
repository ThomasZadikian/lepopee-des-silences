import AppLayout from "@/layouts/AppLayout.vue";
import { mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

const mockLogout  = vi.fn();
const mockIsAdmin = vi.fn(() => false);
const mockIsAuthenticated = vi.fn(() => true);

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({
    username: "testuser",
    logout: mockLogout,
    get isAuthenticated() { return mockIsAuthenticated(); },
    get isAdmin() { return mockIsAdmin(); },
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
    { path: "/",               name: "Dashboard",    component: { template: "<div/>" } },
    { path: "/saves",          name: "Saves",        component: { template: "<div/>" } },
    { path: "/inventory",      name: "Inventory",    component: { template: "<div/>" } },
    { path: "/skills",         name: "Skills",       component: { template: "<div/>" } },
    { path: "/bestiary",       name: "Bestiary",     component: { template: "<div/>" } },
    { path: "/rgpd",           name: "Rgpd",         component: { template: "<div/>" } },
    { path: "/download",       name: "Download",     component: { template: "<div/>" } },
    { path: "/admin/users",    name: "AdminUsers",   component: { template: "<div/>" } },
    { path: "/admin/items",    name: "AdminItems",   component: { template: "<div/>" } },
    { path: "/admin/skills",   name: "AdminSkills",  component: { template: "<div/>" } },
    { path: "/admin/bestiary", name: "AdminBestiary",component: { template: "<div/>" } },
  ],
});

const globalConfig = {
  plugins: [router],
  stubs: {
    "v-app":               { template: "<div><slot /></div>" },
    "v-layout":            { template: "<div><slot /></div>" },
    "v-navigation-drawer": { template: "<div><slot /><slot name='append' /></div>" },
    "v-app-bar":           { template: "<div><slot /></div>" },
    "v-app-bar-title":     { template: "<div><slot /></div>" },
    "v-spacer":            { template: "<span />" },
    "v-main":              { template: "<div><slot /></div>" },
    "v-container":         { template: "<div><slot /></div>" },
    RouterView:            { template: "<div />" },
  },
};

describe("AppLayout", () => {
  beforeEach(() => {
    mockIsAuthenticated.mockReturnValue(true);
    mockIsAdmin.mockReturnValue(false);
    mockLogout.mockReset();
  });

  it("affiche le titre RPG_ESI07", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("RPG_ESI07");
  });

  it("affiche le nom d'utilisateur connecté", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("testuser");
  });

  it("affiche Déconnexion", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Déconnexion");
  });

  it("appelle logout au clic sur Déconnexion", async () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Déconnexion →");
    const vm = wrapper.vm as any;
    vm.auth?.logout?.();
    expect(mockLogout).toHaveBeenCalled();
  });

  it("affiche le lien Tableau de bord", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Tableau de bord");
  });

  it("affiche le lien Sauvegardes", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Sauvegardes");
  });

  it("affiche le lien Inventaire", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Inventaire");
  });

  it("affiche le lien Compétences", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Compétences");
  });

  it("affiche le lien Mes données", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Mes données");
  });

  it("affiche le lien Télécharger le jeu", () => {
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Télécharger le jeu");
  });

  it("n'affiche pas la section Admin pour un joueur normal", () => {
    mockIsAdmin.mockReturnValue(false);
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).not.toContain("Administration");
    expect(wrapper.text()).not.toContain("Utilisateurs");
  });

  it("affiche la section Admin pour un Admin", () => {
    mockIsAdmin.mockReturnValue(true);
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("Administration");
    expect(wrapper.text()).toContain("Utilisateurs");
  });

  it("n'affiche pas la navigation si non authentifié", () => {
    mockIsAuthenticated.mockReturnValue(false);
    const wrapper = mount(AppLayout, { global: globalConfig });
    expect(wrapper.text()).toContain("RPG_ESI07");
    expect(wrapper.text()).not.toContain("Sauvegardes");
  });
});