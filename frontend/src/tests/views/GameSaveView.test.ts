import GameSavesView from "@/views/player/GameSavesView.vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/api/gameSave", () => ({
  gameSavesApi: {
    getAll: vi.fn(),
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
  {
    id: 1,
    playerId: 1,
    currentZone: "Forest",
    positionX: 0,
    positionY: 0,
    savedAt: "2026-01-01",
  },
  {
    id: 2,
    playerId: 1,
    currentZone: "Castle",
    positionX: 5,
    positionY: 5,
    savedAt: "2026-01-02",
  },
];

describe("GameSavesView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre Mes sauvegardes", async () => {
    vi.mocked(gameSavesApi.getAll).mockResolvedValue({
      data: { items: [] },
    } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("sauvegardes");
  });

  it("affiche un message si aucune sauvegarde", async () => {
    vi.mocked(gameSavesApi.getAll).mockResolvedValue({
      data: { items: [] },
    } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Aucune sauvegarde");
  });

  it("affiche la liste des sauvegardes", async () => {
    vi.mocked(gameSavesApi.getAll).mockResolvedValue({
      data: { items: fakeSaves },
    } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Forest");
    expect(wrapper.text()).toContain("Castle");
  });

  it("affiche la zone de chaque sauvegarde", async () => {
    vi.mocked(gameSavesApi.getAll).mockResolvedValue({
      data: { items: fakeSaves },
    } as any);
    const wrapper = mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Forest");
  });

  it("supprime une sauvegarde au clic sur Supprimer", async () => {
    vi.mocked(gameSavesApi.getAll).mockResolvedValue({
      data: { items: fakeSaves },
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
  });

  it("appelle getAll au montage", async () => {
    vi.mocked(gameSavesApi.getAll).mockResolvedValue({
      data: { items: [] },
    } as any);
    mount(GameSavesView, { global: { stubs } });
    await flushPromises();
    expect(vi.mocked(gameSavesApi.getAll)).toHaveBeenCalledOnce();
  });
});
