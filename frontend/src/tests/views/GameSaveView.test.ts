import GameSavesView from "@/views/player/GameSavesView.vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/api/gameSave", () => ({
  gameSavesApi: {
    getById: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock("@/stores/auth", () => ({
  useAuthStore: () => ({ userId: 1 }),
}));

vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));

import { gameSavesApi } from "@/api/gameSave";

const stubs = {
  "v-progress-circular": { template: "<div />" },
  "v-alert": { template: "<div><slot /></div>" },
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
  "v-card": { template: "<div><slot /></div>" },
  "v-card-title": { template: "<div><slot /></div>" },
  "v-card-subtitle": { template: "<div><slot /></div>" },
  "v-card-actions": { template: "<div><slot /></div>" },
  "v-btn": {
    template: "<button @click=\"$emit('click')\"><slot /></button>",
    emits: ["click"],
  },
};

const fakeSaves = [
  { id: 1, saveName: "Save 1", currentZone: "Forest", playerLevel: 5 },
  { id: 2, saveName: "Save 2", currentZone: "Castle", playerLevel: 10 },
];

describe("GameSavesView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre Mes sauvegardes", async () => {
    vi.mocked(gameSavesApi.getById).mockResolvedValue({ data: [] } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("sauvegardes");
  });

  it("affiche un message si aucune sauvegarde", async () => {
    vi.mocked(gameSavesApi.getById).mockResolvedValue({ data: [] } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Aucune sauvegarde");
  });

  it("affiche la liste des sauvegardes", async () => {
    vi.mocked(gameSavesApi.getById).mockResolvedValue({
      data: fakeSaves,
    } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Save 1");
    expect(wrapper.text()).toContain("Save 2");
  });

  it("affiche la zone et le niveau", async () => {
    vi.mocked(gameSavesApi.getById).mockResolvedValue({
      data: fakeSaves,
    } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Forest");
    expect(wrapper.text()).toContain("5");
  });

  it("supprime une sauvegarde au clic sur Supprimer", async () => {
    vi.mocked(gameSavesApi.getById).mockResolvedValue({
      data: fakeSaves,
    } as any);
    vi.mocked(gameSavesApi.delete).mockResolvedValue({} as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();

    const deleteBtn = wrapper
      .findAll("button")
      .find((b) => b.text().includes("Supprimer"));
    if (deleteBtn) await deleteBtn.trigger("click");
    await flushPromises();

    expect(vi.mocked(gameSavesApi.delete)).toHaveBeenCalledWith(1);
    expect(wrapper.text()).not.toContain("Save 1");
  });

  it("appelle getById avec le userId du store", async () => {
    vi.mocked(gameSavesApi.getById).mockResolvedValue({ data: [] } as any);
    mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(vi.mocked(gameSavesApi.getById)).toHaveBeenCalledWith(1);
  });
});
