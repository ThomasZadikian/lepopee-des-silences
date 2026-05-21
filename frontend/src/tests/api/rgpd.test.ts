import { describe, expect, it, vi } from "vitest"
import { rgpdApi } from "@/api/rgpd"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), delete: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("rgpdApi", () => {
  it("exportData appelle GET /rgpd/export", () => {
    rgpdApi.exportData()
    expect(api.get).toHaveBeenCalledWith("/rgpd/export")
  })

  it("exportJson appelle GET /rgpd/export/json avec responseType blob", () => {
    rgpdApi.exportJson()
    expect(api.get).toHaveBeenCalledWith("/rgpd/export/json", { responseType: "blob" })
  })

  it("deleteAccount appelle DELETE /rgpd/me avec la raison", () => {
    rgpdApi.deleteAccount("Je quitte le jeu")
    expect(api.delete).toHaveBeenCalledWith("/rgpd/me", { data: "Je quitte le jeu" })
  })

  it("deleteAccount sans raison appelle DELETE /rgpd/me", () => {
    rgpdApi.deleteAccount()
    expect(api.delete).toHaveBeenCalledWith("/rgpd/me", { data: undefined })
  })
})
