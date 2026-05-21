import FoxSprite from "@/components/FoxSprite.vue"
import { mount } from "@vue/test-utils"
import { describe, expect, it } from "vitest"

describe("FoxSprite", () => {
  it("affiche un canvas", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("définit les attributs width et height", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    const canvas = wrapper.find("canvas")
    expect(Number(canvas.attributes("width"))).toBeGreaterThan(0)
    expect(Number(canvas.attributes("height"))).toBeGreaterThan(0)
  })

  it("affiche un canvas pour l'état JEU", () => {
    const wrapper = mount(FoxSprite, { props: { state: "JEU" } })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("affiche un canvas pour l'état MANGER", () => {
    const wrapper = mount(FoxSprite, { props: { state: "MANGER" } })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("affiche un canvas pour l'état EXCITE", () => {
    const wrapper = mount(FoxSprite, { props: { state: "EXCITE" } })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("affiche un canvas pour l'état TRISTE", () => {
    const wrapper = mount(FoxSprite, { props: { state: "TRISTE" } })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("affiche un canvas pour l'état ENDORMI", () => {
    const wrapper = mount(FoxSprite, { props: { state: "ENDORMI" } })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("fallback sur REPOS pour un état inconnu", () => {
    const wrapper = mount(FoxSprite, { props: { state: "INCONNU" } as any })
    expect(wrapper.find("canvas").exists()).toBe(true)
  })

  it("applique le style pixelated au canvas", () => {
    const wrapper = mount(FoxSprite, { props: { state: "REPOS" } })
    const canvas = wrapper.find("canvas")
    expect(canvas.attributes("style")).toContain("pixelated")
  })
})
