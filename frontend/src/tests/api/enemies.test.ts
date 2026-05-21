import { describe, expect, it, vi } from "vitest"
import { enemiesApi } from "@/api/enemies"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("enemiesApi", () => {
  it("getAll appelle GET /enemies", () => {
    enemiesApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/enemies")
  })

  it("getById appelle GET /enemies/{id}", () => {
    enemiesApi.getById(5)
    expect(api.get).toHaveBeenCalledWith("/enemies/5")
  })

  it("getByType appelle GET /enemies/type/{type}", () => {
    enemiesApi.getByType("boss")
    expect(api.get).toHaveBeenCalledWith("/enemies/type/boss")
  })
})
