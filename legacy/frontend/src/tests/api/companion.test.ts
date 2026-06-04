import { describe, expect, it, vi } from "vitest"
import { companionApi } from "@/api/companion"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("companionApi", () => {
  it("getState appelle GET /companion/state", () => {
    companionApi.getState()
    expect(api.get).toHaveBeenCalledWith("/companion/state")
  })
})
