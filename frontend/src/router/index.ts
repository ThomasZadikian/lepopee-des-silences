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
    path: '/mfa/setup',
    name: 'MfaSetup',
    component: () => import('@/views/auth/MfaSetupView.vue'),
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
          path: 'enclos',
          name: 'Enclos',
          component: () => import('@/views/player/EnclosView.vue'),
        },
        {
          path: 'bestiary',
          name: 'Bestiary',
          component: () => import('@/views/player/BestiaryView.vue'),
          meta: { requiresAuth: true }
        },
        {
          path: 'leaderboard',
          name: 'Leaderboard',
          component: () => import('@/views/player/LeaderboardView.vue'),
        },
        {
          path: 'calculator',
          name: 'Calculator',
          component: () => import('@/views/player/CalculatorView.vue'),
        },
        {
          path: "admin/users",
          name: "AdminUsers",
          component: () => import("@/views/admin/AdminUsersView.vue"),
          meta: { requiresAdmin: true },
        },
        {
          path: "admin/items",
          name: "AdminItems",
          component: () => import("@/views/admin/AdminItemsView.vue"),
          meta: { requiresAdmin: true },
        },
        {
          path: "admin/skills",
          name: "AdminSkills",
          component: () => import("@/views/admin/AdminSkillsView.vue"),
          meta: { requiresAdmin: true },
        },
        {
          path: "admin/bestiary",
          name: "AdminBestiary",
          component: () => import("@/views/admin/AdminBestiaireView.vue"),
          meta: { requiresAdmin: true },
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
