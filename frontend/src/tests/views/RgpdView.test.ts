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

  it("affiche le titre Mes données personnelles", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Confidentialité");
  });

  it("affiche Art. 15", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Droit 01");
  });

  it("affiche Art. 20", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Droit 02");
  });

  it("affiche Art. 17", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Droit 03");
  });

  it("affiche le bouton Voir mes données", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Voir mes données");
  });

  it("affiche le bouton Télécharger JSON", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Demander l'export");
  });

  it("affiche le bouton Supprimer mon compte", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    expect(wrapper.text()).toContain("Supprimer mon compte");
  });

it("appelle exportData au clic sur Voir mes données", async () => {
  vi.mocked(rgpdApi.exportData).mockResolvedValue({ data: {} } as any)
  const wrapper = mount(RgpdView, { global: { stubs } })
  const btn = wrapper.findAll("button").find(b => b.text().includes("Ouvrir la vue"))
  if (btn) await btn.trigger("click")
  await flushPromises()
  expect(vi.mocked(rgpdApi.exportData)).toHaveBeenCalled()
})

it("appelle exportJson au clic sur Télécharger JSON", async () => {
  vi.mocked(rgpdApi.exportJson).mockResolvedValue({ data: new Blob(["{}"]) } as any)
  const wrapper = mount(RgpdView, { global: { stubs } })
  const btn = wrapper.findAll("button").find(b => b.text().includes("Demander l'export"))
  if (btn) await btn.trigger("click")
  await flushPromises()
  expect(vi.mocked(rgpdApi.exportJson)).toHaveBeenCalled()
})

it("ouvre le dialogue au clic sur Supprimer mon compte", async () => {
  const wrapper = mount(RgpdView, { global: { stubs } })
  const vm = wrapper.vm as any
  expect(vm.confirmDelete).toBe(false)
  const btn = wrapper.findAll("button").find(b => b.text().includes("Demander la suppression"))
  if (btn) await btn.trigger("click")
  expect(vm.confirmDelete).toBe(true)
})

  it("le dialogue est fermé par défaut", () => {
    const wrapper = mount(RgpdView, { global: { stubs } });
    const vm = wrapper.vm as any;
    expect(vm.confirmDelete).toBe(false);
  });
});
