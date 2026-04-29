import { useAuthStore } from "@/stores/auth";
import { createPinia, setActivePinia } from "pinia";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { createMemoryHistory, createRouter } from "vue-router";

vi.mock("@/api/auth", () => ({
  authApi: { login: vi.fn(), register: vi.fn(), mfa: vi.fn() },
  default: {
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } },
  },
}));
// Composant vide pour les tests de routing — pas besoin de vrai composant
const Blank = { template: "<div />" };
function makeRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/login", name: "Login", component: Blank },
      {
        path: "/dashboard",
        name: "Dashboard",
        component: Blank,
        meta: { requiresAuth: true },
      },
      {
        path: "/admin",
        name: "AdminUsers",
        component: Blank,
        meta: { requiresAuth: true, requiresAdmin: true },
      },
    ],
  });
}
describe("Guards de navigation", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
  });
  it("redirige vers /login si non authentifié sur route protégée", async () => {
    const router = makeRouter();
    // Importe le guard et l'attache au router de test
    const { useAuthStore } = await import("@/stores/auth");
    router.beforeEach((to) => {
      const auth = useAuthStore();
      if (to.meta.requiresAuth && !auth.isAuthenticated)
        return { name: "Login" };
    });
    await router.push("/dashboard");
    expect(router.currentRoute.value.name).toBe("Login");
  });
  it("accède à /dashboard si authentifié", async () => {
    localStorage.setItem("token", "fake-token");
    const router = makeRouter();
    router.beforeEach((to) => {
      const auth = useAuthStore();
      if (to.meta.requiresAuth && !auth.isAuthenticated)
        return { name: "Login" };
    });
    await router.push("/dashboard");
    expect(router.currentRoute.value.name).toBe("Dashboard");
  });
  it("redirige vers /dashboard si non Admin sur route admin", async () => {
    localStorage.setItem("token", "fake-token");
    localStorage.setItem("role", "Player");
    const router = makeRouter();
    router.beforeEach((to) => {
      const auth = useAuthStore();
      if (to.meta.requiresAdmin && !auth.isAdmin) return { name: "Dashboard" };
      if (to.meta.requiresAuth && !auth.isAuthenticated)
        return { name: "Login" };
    });
    await router.push("/admin");
    expect(router.currentRoute.value.name).toBe("Dashboard");
  });
  it("accède à /admin si rôle Admin", async () => {
    localStorage.setItem("token", "fake-token");
    localStorage.setItem("role", "Admin");
    const router = makeRouter();
    router.beforeEach((to) => {
      const auth = useAuthStore();
      if (to.meta.requiresAdmin && !auth.isAdmin) return { name: "Dashboard" };
      if (to.meta.requiresAuth && !auth.isAuthenticated)
        return { name: "Login" };
    });
    await router.push("/admin");
    expect(router.currentRoute.value.name).toBe("AdminUsers");
  });
});
