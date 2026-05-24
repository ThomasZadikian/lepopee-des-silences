import { describe, expect, it, vi } from "vitest"
import { usersApi } from "@/api/users"
import api from "@/api/auth"

vi.mock("@/api/auth", () => ({
  default: { get: vi.fn(), delete: vi.fn(), interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } } },
}))

describe("usersApi", () => {
  it("getAll appelle GET /users", () => {
    usersApi.getAll()
    expect(api.get).toHaveBeenCalledWith("/users")
  })

  it("delete appelle DELETE /users/{id}", () => {
    usersApi.delete(3)
    expect(api.delete).toHaveBeenCalledWith("/users/3")
  })
})
