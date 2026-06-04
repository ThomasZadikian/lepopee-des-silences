import InventoryView from "@/views/player/InventoryView.vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/api/inventory", () => ({
  inventoryApi: {
    getMe: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));

import { inventoryApi } from "@/api/inventory";

const stubs = {
  "v-container": { template: "<div><slot /></div>" },
  "v-progress-circular": { template: "<div />" },
  "v-alert": { template: "<div><slot /></div>" },
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
  "v-card": { template: "<div><slot /></div>" },
  "v-card-item": { template: "<div><slot /></div>" },
  "v-card-title": { template: "<div><slot /></div>" },
  "v-card-subtitle": { template: "<div><slot /></div>" },
  "v-card-text": { template: "<div><slot /></div>" },
  "v-avatar": { template: "<div><slot /></div>" },
  "v-icon": { template: "<span />" },
  "v-chip": { template: "<span><slot /></span>" },
  "v-divider": { template: "<hr />" },
};

const fakeInventory = [
  {
    id: 1,
    playerId: 1,
    itemId: 1,
    quantity: 2,
    isEquipped: true,
    item: {
      id: 1,
      name: "Épée en Bois",
      type: "weapon",
      description: "Arme de débutant",
      price: 50,
      category: null,
      effectValue: null,
      statModifiers: JSON.stringify({ strength: 5 }),
    },
  },
  {
    id: 2,
    playerId: 1,
    itemId: 2,
    quantity: 5,
    isEquipped: false,
    item: {
      id: 2,
      name: "Petite Potion HP",
      type: "consumable",
      description: "Restaure 50 HP",
      price: 20,
      category: "potion_hp",
      effectValue: 50,
      statModifiers: null,
    },
  },
];

describe("InventoryView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre Inventaire", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({
      data: { items: [] },
    } as any);
    const wrapper = mount(InventoryView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Inventaire");
  });

  it("affiche un message si inventaire vide", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({
      data: { items: [] },
    } as any);
    const wrapper = mount(InventoryView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("vide");
  });

  it("affiche les items de l'inventaire", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({
      data: { items: fakeInventory },
    } as any);
    const wrapper = mount(InventoryView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Épée en Bois");
    expect(wrapper.text()).toContain("Petite Potion HP");
  });

  it("affiche la quantité de chaque item", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({
      data: { items: fakeInventory },
    } as any);
    const wrapper = mount(InventoryView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("2");
    expect(wrapper.text()).toContain("5");
  });

  it("affiche le prix de chaque item", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({
      data: { items: fakeInventory },
    } as any);
    const wrapper = mount(InventoryView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("50");
    expect(wrapper.text()).toContain("20");
  });

  it("appelle getMe au montage", async () => {
    vi.mocked(inventoryApi.getMe).mockResolvedValue({
      data: { items: [] },
    } as any);
    mount(InventoryView, { global: { stubs } });
    await flushPromises();
    expect(vi.mocked(inventoryApi.getMe)).toHaveBeenCalledOnce();
  });
});
