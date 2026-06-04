import FoxSprite from "@/components/FoxSprite.vue"
import { mount } from "@vue/test-utils"
import { beforeEach, describe, expect, it, vi } from "vitest"

// Mock image load
beforeEach(() => {
  vi.clearAllMocks()
  vi.spyOn(window, "requestAnimationFrame").mockImplementation((cb: FrameRequestCallback) => {
    return window.setTimeout(() => cb(Date.now()), 16)
  })
  vi.spyOn(window, "cancelAnimationFrame").mockImplementation((id: number) => {
    window.clearTimeout(id)
  })
})

// Mock Image
const mockImage = {
  src: "",
  complete: true,
  naturalWidth: 100,
  naturalHeight: 100,
  onload: null as (() => void) | null,
}
vi.stubGlobal("Image", function () { return mockImage })

// Mock Canvas
HTMLCanvasElement.prototype.getContext = vi.fn(() => ({
  clearRect: vi.fn(),
  drawImage: vi.fn(),
})) as any

describe("FoxSprite", () => {
  it("affiche un élément canvas", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    const canvas = wrapper.find("canvas")
    expect(canvas.exists()).toBe(true)
  })

  it("affiche le canvas avec les dimensions REPOS par défaut", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    const canvas = wrapper.find("canvas") as any
    expect(canvas.attributes("width")).toBeDefined()
    expect(canvas.attributes("height")).toBeDefined()
  })

  it("calcule les bonnes dimensions pour l'état JEU", () => {
    const wrapper = mount(FoxSprite, { props: { state: "JEU" } })
    const vm = wrapper.vm as any
    expect(vm.displayW).toBeGreaterThan(0)
    expect(vm.displayH).toBeGreaterThan(0)
    expect(vm.currentConfig.speed).toBe(100)
  })

  it("calcule les bonnes dimensions pour l'état MANGER", () => {
    const wrapper = mount(FoxSprite, { props: { state: "MANGER" } })
    const vm = wrapper.vm as any
    expect(vm.currentConfig.speed).toBe(200)
    expect(vm.currentConfig.frames.length).toBe(6)
  })

  it("calcule les bonnes dimensions pour l'état EXCITE", () => {
    const wrapper = mount(FoxSprite, { props: { state: "EXCITE" } })
    const vm = wrapper.vm as any
    expect(vm.currentConfig.speed).toBe(80)
    expect(vm.currentConfig.frames.length).toBe(5)
  })

  it("calcule les bonnes dimensions pour l'état TRISTE", () => {
    const wrapper = mount(FoxSprite, { props: { state: "TRISTE" } })
    const vm = wrapper.vm as any
    expect(vm.currentConfig.speed).toBe(300)
  })

  it("calcule les bonnes dimensions pour l'état ENDORMI", () => {
    const wrapper = mount(FoxSprite, { props: { state: "ENDORMI" } })
    const vm = wrapper.vm as any
    expect(vm.currentConfig.speed).toBe(500)
  })

  it("retourne REPOS comme fallback pour un état inconnu", () => {
    const wrapper = mount(FoxSprite, { props: { state: "INCONNU" } })
    const vm = wrapper.vm as any
    expect(vm.currentConfig.speed).toBe(150)
  })

  it("retourne la largeur max correcte pour REPOS", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    const vm = wrapper.vm as any
    // La frame la plus large dans REPOS a w=153, donc displayW = 153 * 2 = 306
    expect(vm.displayW).toBe(306)
    // sh=122, donc displayH = 122 * 2 = 244
    expect(vm.displayH).toBe(244)
  })

  it("appelle requestAnimationFrame au montage", () => {
    mount(FoxSprite, { props: { state: "REPOS" } })
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it("nettoie l'animation au démontage", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })

  it("redémarre l'animation quand la propriété state change", async () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    const cancelSpy = vi.spyOn(window, "cancelAnimationFrame")
    await wrapper.setProps({ state: "JEU" })
    expect(cancelSpy).toHaveBeenCalled()
  })
})
