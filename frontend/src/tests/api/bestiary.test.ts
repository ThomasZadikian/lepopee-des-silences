import { describe, expect, it, vi } from "vitest"
import { bestiaryApi } from "@/api/bestiary"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("bestiaryApi", () => {
  it("getMe appelle GET /bestiaryunlocks/me", () => {
    bestiaryApi.getMe()
    expect(api.get).toHaveBeenCalledWith("/bestiaryunlocks/me")
  })

  it("getAll appelle GET /bestiaryunlocks", () => {
    bestiaryApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/bestiaryunlocks")
  })
})
