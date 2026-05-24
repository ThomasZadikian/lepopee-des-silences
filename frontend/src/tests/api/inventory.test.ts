import { describe, expect, it, vi } from "vitest"
import { inventoryApi } from "@/api/inventory"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), delete: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("inventoryApi", () => {
  it("getAll appelle GET /playerinventories", () => {
    inventoryApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/playerinventories")
  })

  it("getMe appelle GET /playerinventories/me", () => {
    inventoryApi.getMe()
    expect(api.get).toHaveBeenCalledWith("/playerinventories/me")
  })

  it("getById appelle GET /playerinventories/{id}", () => {
    inventoryApi.getById(7)
    expect(api.get).toHaveBeenCalledWith("/playerinventories/7")
  })

  it("delete appelle DELETE /playerinventories/{id}", () => {
    inventoryApi.delete(7)
    expect(api.delete).toHaveBeenCalledWith("/playerinventories/7")
  })
})
