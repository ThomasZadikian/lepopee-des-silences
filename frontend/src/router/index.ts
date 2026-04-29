import { useAuthStore } from "@/stores/auth";
import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/login",
      name: "Login",
      component: () => import("@/views/auth/LoginView.vue"),
    },
    {
      path: "/register",
      name: "Register",
      component: () => import("@/views/auth/RegisterView.vue"),
    },
    {
      path: "/mfa",
      name: "Mfa",
      component: () => import("@/views/auth/MfaView.vue"),
    },
    {
      path: "/",
      component: () => import("@/layouts/AppLayout.vue"),
      meta: { requiresAuth: true },
      children: [
        { path: "", redirect: "/dashboard" },
        {
          path: "dashboard",
          name: "Dashboard",
          component: () => import("@/views/player/DashboardView.vue"),
        },
        {
          path: "saves",
          name: "Saves",
          component: () => import("@/views/player/GameSavesView.vue"),
        },
        {
          path: "inventory",
          name: "Inventory",
          component: () => import("@/views/player/InventoryView.vue"),
        },
        {
          path: "skills",
          name: "Skills",
          component: () => import("@/views/player/SkillsView.vue"),
        },
        {
          path: "rgpd",
          name: "Rgpd",
          component: () => import("@/views/player/RgpdView.vue"),
        },
        {
          path: "admin/users",
          name: "AdminUsers",
          component: () => import("@/views/admin/UsersView.vue"),
          meta: { RequiresAdmin: true },
        },
        {
          path: "admin/items",
          name: "AdminItems",
          component: () => import("@/views/admin/ItemsView.vue"),
          meta: { RequiresAdmin: true },
        },
      ],
    },
    { path: "/:pathMatch(.*)*", redirect: "/dashboard" },
  ],
});

router.beforeEach((to) => {
  const auth = useAuthStore();
  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: "Dashboard" };
  }
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: "Login" };
  }
  if ((to.name === "Login" || to.name === "Register") && auth.isAuthenticated) {
    return { name: "Dashboard" };
  }
});

export default router;
