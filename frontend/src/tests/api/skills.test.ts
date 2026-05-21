import { describe, expect, it, vi } from "vitest"
import { playerSkillsApi, skillsApi } from "@/api/skills"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("playerSkillsApi", () => {
  it("getAll appelle GET /playerskills", () => {
    playerSkillsApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/playerskills")
  })

  it("getMe appelle GET /playerskills/me", () => {
    playerSkillsApi.getMe()
    expect(api.get).toHaveBeenCalledWith("/playerskills/me")
  })
})

describe("skillsApi", () => {
  it("getAll appelle GET /skills", () => {
    skillsApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/skills")
  })
})
