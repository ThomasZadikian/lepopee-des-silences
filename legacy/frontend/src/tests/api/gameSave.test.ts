import { describe, expect, it, vi } from "vitest"
import { gameSavesApi } from "@/api/gameSave"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("gameSavesApi", () => {
  it("getAll appelle GET /gamesaves", () => {
    gameSavesApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/gamesaves")
  })

  it("getMe appelle GET /gamesaves/me", () => {
    gameSavesApi.getMe()
    expect(api.get).toHaveBeenCalledWith("/gamesaves/me")
  })

  it("getById appelle GET /gamesaves/{id}", () => {
    gameSavesApi.getById(3)
    expect(api.get).toHaveBeenCalledWith("/gamesaves/3")
  })

  it("create appelle POST /gamesaves avec les données", () => {
    const dto = { playerId: 1, currentZone: "Forêt", playerLevel: 5 }
    gameSavesApi.create(dto)
    expect(api.post).toHaveBeenCalledWith("/gamesaves", dto)
  })

  it("delete appelle DELETE /gamesaves/{id}", () => {
    gameSavesApi.delete(3)
    expect(api.delete).toHaveBeenCalledWith("/gamesaves/3")
  })
})
