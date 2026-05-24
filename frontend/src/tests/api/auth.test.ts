import { describe, expect, it, vi } from "vitest"
import { authApi } from "@/api/auth"

// On mock uniquement axios, pas le module auth
vi.mock("axios", async (importOriginal) => {
  const actual = await importOriginal()
  return {
    ...(actual as any),
    default: {
      ...(actual as any).default,
      create: () => ({
        post: vi.fn(),
        get: vi.fn(),
        delete: vi.fn(),
        interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
      }),
    },
  }
})

import api from "@/api/auth"

describe("authApi", () => {
  it("login appelle POST /auth/login", () => {
    authApi.login({ username: "test", password: "pass" })
    expect(api.post).toHaveBeenCalledWith("/auth/login", { username: "test", password: "pass" })
  })

  it("register appelle POST /auth/register", () => {
    authApi.register({ username: "test", email: "test@test.com", password: "pass" })
    expect(api.post).toHaveBeenCalledWith("/auth/register", { username: "test", email: "test@test.com", password: "pass" })
  })

  it("mfa appelle POST /auth/mfa", () => {
    authApi.mfa({ userId: 1, code: "123456" })
    expect(api.post).toHaveBeenCalledWith("/auth/mfa", { userId: 1, code: "123456" })
  })

  it("setupMfa appelle POST /auth/mfa/setup", () => {
    authApi.setupMfa({ userId: 1, mfaToken: "token" })
    expect(api.post).toHaveBeenCalledWith("/auth/mfa/setup", { userId: 1, mfaToken: "token" })
  })

  it("verifyMfaWithToken appelle POST /auth/mfa/verify", () => {
    authApi.verifyMfaWithToken({ userId: 1, mfaToken: "token", code: "123456" })
    expect(api.post).toHaveBeenCalledWith("/auth/mfa/verify", { userId: 1, mfaToken: "token", code: "123456" })
  })
})
