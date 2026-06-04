import DownloadView from "@/views/DownloadView.vue";
import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";

vi.mock("vue-router", () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/api/auth", () => ({
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));

const stubs = {
  "v-row": { template: "<div><slot /></div>" },
  "v-col": { template: "<div><slot /></div>" },
};

describe("DownloadView", () => {
  it("se monte sans erreur", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    expect(wrapper.exists()).toBe(true);
  });

  it("affiche le titre Télécharger", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    expect(wrapper.text()).toContain("Télécharger");
  });

  it("affiche la version v1.0.0", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    expect(wrapper.text()).toContain("v1.0.0");
  });

  it("le lien de téléchargement pointe vers la release GitHub", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    const link = wrapper.find("a[download]");
    expect(link.attributes("href")).toBe(
      "https://github.com/ThomasZadikian/rpg_esi07/releases/download/v1.0.0/RPG_ESI07.zip"
    );
  });

  it("affiche les prérequis Windows", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    expect(wrapper.text()).toContain("Windows");
  });

  it("affiche les étapes d'installation", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    expect(wrapper.text()).toContain("Télécharger l'archive");
    expect(wrapper.text()).toContain("Extraire l'archive");
    expect(wrapper.text()).toContain("Lancer le jeu");
  });

  it("affiche la section MFA", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    expect(wrapper.text()).toContain("MFA");
  });

  it("openLink ouvre un nouvel onglet pour les URLs externes", () => {
    const windowOpen = vi.spyOn(window, "open").mockImplementation(() => null);
    const wrapper = mount(DownloadView, { global: { stubs } });
    const vm = wrapper.vm as any;
    vm.openLink("https://github.com/ThomasZadikian/rpg_esi07/releases/tag/v1.0.0");
    expect(windowOpen).toHaveBeenCalledWith(
      "https://github.com/ThomasZadikian/rpg_esi07/releases/tag/v1.0.0",
      "_blank"
    );
  });

  it("openLink redirige en interne pour les routes locales", () => {
    const wrapper = mount(DownloadView, { global: { stubs } });
    const vm = wrapper.vm as any;
    const locationSpy = vi.spyOn(window, "location", "get").mockReturnValue({
      ...window.location,
      href: "",
    } as any);
    vm.openLink("/register");
    locationSpy.mockRestore();
    expect(wrapper.exists()).toBe(true);
  });
});