import RgpdView from "@/views/player/RgpdView.vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/api/rgpd", () => ({
  rgpdApi: {
    exportData: vi.fn(),
    exportJson: vi.fn(),
    deleteAccount: vi.fn(),
  },
}));

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({ userId: 1, logout: vi.fn() }),
}));

vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));

import { rgpdApi } from "@/api/rgpd";

global.alert = vi.fn();
global.URL.createObjectURL = vi.fn(() => "blob:fake");
global.URL.revokeObjectURL = vi.fn();

const stubs = {
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
  "v-card": { template: "<div><slot /></div>" },
  "v-card-title": { template: "<div><slot /></div>" },
  "v-card-text": { template: "<div><slot /></div>" },
  "v-card-actions": { template: "<div><slot /></div>" },
  "v-btn": {
    template: "<button @click=\"$emit('click')\"><slot /></button>",
    emits: ["click"],
  },
  "v-dialog": { template: "<div><slot /></div>" },
  "v-text-field": { template: "<input />" },
  "v-spacer": { template: "<span />" },
};

describe("RgpdView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Confidentialité");
  });

  it("affiche les trois droits RGPD", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Droit 01");
    expect(wrapper.text()).toContain("Droit 02");
    expect(wrapper.text()).toContain("Droit 03");
  });

  it("affiche le bouton Demander l'export", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Demander l'export");
  });

  it("affiche le bouton Voir mes données", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Voir mes données");
  });

  it("affiche le bouton Supprimer mon compte", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Supprimer mon compte");
  });

  it("confirme la suppression vaut false par défaut", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    expect(vm.confirmDelete).toBe(false);
  });

  it("ouvre le dialogue en cliquant sur le ×", async () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    vm.confirmDelete = true;
    await flushPromises();
    expect(vm.confirmDelete).toBe(true);
  });

  it("annule la suppression sans alert", async () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    vm.confirmDelete = true;
    await flushPromises();
    vm.confirmDelete = false;
    expect(vm.confirmDelete).toBe(false);
  });

  it("appelle deleteAccount via le bouton Confirmer", async () => {
    vi.mocked(rgpdApi.deleteAccount).mockResolvedValue({} as any);
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    vm.deleteReason = "Test reason";
    await vm.deleteAccount();
    await flushPromises();
    expect(vi.mocked(rgpdApi.deleteAccount)).toHaveBeenCalledWith("Test reason");
  });

  it("appelle deleteAccount sans raison", async () => {
    vi.mocked(rgpdApi.deleteAccount).mockResolvedValue({} as any);
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    vm.deleteReason = "";
    await vm.deleteAccount();
    await flushPromises();
    expect(vi.mocked(rgpdApi.deleteAccount)).toHaveBeenCalledWith(undefined);
  });

  it("appelle deleteAccount avec succès", async () => {
    vi.mocked(rgpdApi.deleteAccount).mockResolvedValue({} as any);
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    await vm.deleteAccount();
    await flushPromises();
    expect(vi.mocked(rgpdApi.deleteAccount)).toHaveBeenCalled();
  });

  it("appelle deleteAccount en erreur sans plantage", async () => {
    vi.mocked(rgpdApi.deleteAccount).mockRejectedValue(new Error("Network"));
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    await expect(vm.deleteAccount()).resolves.toBeUndefined();
  });

  it("appelle exportData via le bouton Ouvrir la vue détaillée", async () => {
    vi.mocked(rgpdApi.exportData).mockResolvedValue({ data: { test: "ok" } } as any);
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    await vm.exportData();
    await flushPromises();
    expect(vi.mocked(rgpdApi.exportData)).toHaveBeenCalled();
  });

  it("appelle downloadJson via le bouton Demander l'export", async () => {
    vi.mocked(rgpdApi.exportJson).mockResolvedValue({ data: new Blob(["{}"]) } as any);
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    await vm.downloadJson();
    await flushPromises();
    expect(vi.mocked(rgpdApi.exportJson)).toHaveBeenCalled();
  });

  it("gère l'exportData en erreur silencieuse", async () => {
    vi.mocked(rgpdApi.exportData).mockRejectedValue(new Error("Network"));
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    await expect(vm.exportData()).resolves.toBeUndefined();
  });

  it("gère downloadJson en erreur silencieuse", async () => {
    vi.mocked(rgpdApi.exportJson).mockRejectedValue(new Error("Network"));
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    await expect(vm.downloadJson()).resolves.toBeUndefined();
  });

  it("affiche les 6 catégories de données", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Identité");
    expect(wrapper.text()).toContain("Sauvegardes");
    expect(wrapper.text()).toContain("Profil");
    expect(wrapper.text()).toContain("Bestiaire");
    expect(wrapper.text()).toContain("Statistiques");
    expect(wrapper.text()).toContain("Sessions");
  });
});
