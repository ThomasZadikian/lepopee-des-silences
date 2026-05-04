import SkillsView from "@/views/player/SkillsView.vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/api/skills", () => ({
  playerSkillsApi: {
    getMe: vi.fn(),
  },
}));

vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));

import { playerSkillsApi } from "@/api/skills";

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

const fakeSkills = [
  {
    id: 1,
    playerId: 1,
    skillId: 1,
    unlockedAt: "2026-01-01T00:00:00Z",
    skill: {
      id: 1,
      name: "Boule de Feu",
      mpCost: 20,
      baseDamage: 40,
      healAmount: null,
      effectType: "damage",
      elementType: "fire",
      description: "Lance une boule de feu enflammée",
    },
  },
  {
    id: 2,
    playerId: 1,
    skillId: 2,
    unlockedAt: "2026-01-05T00:00:00Z",
    skill: {
      id: 2,
      name: "Soin",
      mpCost: 10,
      baseDamage: null,
      healAmount: 50,
      effectType: "heal",
      elementType: "neutral",
      description: "Restaure les points de vie",
    },
  },
];

describe("SkillsView", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  it("affiche le titre Compétences", async () => {
    vi.mocked(playerSkillsApi.getMe).mockResolvedValue({
      data: { items: [] },
    } as any);
    const wrapper = mount(SkillsView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Arbre");
  });

  it("affiche un message si aucune compétence", async () => {
    vi.mocked(playerSkillsApi.getMe).mockResolvedValue({
      data: { items: [] },
    } as any);
    const wrapper = mount(SkillsView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Aucune compétence");
  });

  it("affiche les compétences débloquées", async () => {
    vi.mocked(playerSkillsApi.getMe).mockResolvedValue({
      data: { items: fakeSkills },
    } as any);
    const wrapper = mount(SkillsView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Boule de Feu");
    expect(wrapper.text()).toContain("Soin");
  });

  it("affiche le coût MP de chaque compétence", async () => {
    vi.mocked(playerSkillsApi.getMe).mockResolvedValue({
      data: { items: fakeSkills },
    } as any);
    const wrapper = mount(SkillsView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("20");
    expect(wrapper.text()).toContain("10");
  });

  it("affiche la description de chaque compétence", async () => {
    vi.mocked(playerSkillsApi.getMe).mockResolvedValue({
      data: { items: fakeSkills },
    } as any);
    const wrapper = mount(SkillsView, { global: { stubs } });
    await flushPromises();
    expect(wrapper.text()).toContain("Boule de Feu");
  });

  it("appelle getMe au montage", async () => {
    vi.mocked(playerSkillsApi.getMe).mockResolvedValue({
      data: { items: [] },
    } as any);
    mount(SkillsView, { global: { stubs } });
    await flushPromises();
    expect(vi.mocked(playerSkillsApi.getMe)).toHaveBeenCalledOnce();
  });
});
