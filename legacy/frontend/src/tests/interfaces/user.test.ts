import { describe, expect, it } from "vitest"
import type { User } from "@/interfaces/user"

describe("User interface", () => {
  it("accepte un utilisateur valide (actif)", () => {
    const user: User = {
      id: 1, username: "test", role: "Player", mfaEnabled: false,
      createdAt: "2026-01-01T00:00:00Z", lastLoginAt: "2026-05-20T00:00:00Z", deletedAt: null,
    }
    expect(user.role).toBe("Player")
    expect(user.deletedAt).toBeNull()
  })

  it("accepte un utilisateur supprimé", () => {
    const user: User = {
      id: 2, username: "deleted", role: "Player", mfaEnabled: true,
      createdAt: "2026-01-01T00:00:00Z", lastLoginAt: null, deletedAt: "2026-05-21T00:00:00Z",
    }
    expect(user.deletedAt).not.toBeNull()
    expect(user.lastLoginAt).toBeNull()
  })

  it("accepte un administrateur", () => {
    const user: User = {
      id: 3, username: "admin", role: "Admin", mfaEnabled: true,
      createdAt: "2026-01-01T00:00:00Z", lastLoginAt: "2026-05-21T00:00:00Z", deletedAt: null,
    }
    expect(user.role).toBe("Admin")
  })
})
