<template>
  <v-app theme="rpgLight">
    <v-layout>
      <!-- ── Sidebar ────────────────────────────────────────────────── -->
      <v-navigation-drawer 
      permanent 
      width="200" 
      class="rpg-sidebar" 
      style="position: fixed; height: 100vh; overflow-y: auto;">
        <!-- Logo -->
        <div class="pa-5 grid-border-bottom">
          <div
            style="
              font-family: var(--font-serif);
              font-size: 1.1rem;
              font-weight: 900;
              letter-spacing: -0.02em;
            "
          >
            RPG_ESI07
          </div>
          <div class="editorial-label mt-1">Portail joueur</div>
        </div>

        <!-- Navigation principale -->
        <div class="py-3">
          <div
            v-for="item in navItems"
            :key="item.name"
            class="rpg-sidebar-item"
            :class="{ active: currentRoute === item.name }"
            @click="router.push({ name: item.name })"
          >
            {{ item.label }}
          </div>
        </div>

        <!-- Section Admin -->
        <template v-if="auth.isAdmin">
          <div class="grid-border-top py-3">
            <div class="editorial-label px-5 mb-2">Administration</div>
            <div
              v-for="item in adminItems"
              :key="item.name"
              class="rpg-sidebar-item"
              :class="{ active: currentRoute === item.name }"
              @click="router.push({ name: item.name })"
            >
              {{ item.label }}
            </div>
          </div>
        </template>

        <!-- Logout -->
        <template #append>
          <div class="grid-border-top">
            <div class="pa-4">
              <div
                class="editorial-label mb-1"
                style="color: var(--rpg-ink-muted)"
              >
                Connecté en tant que
              </div>
              <div
                style="font-size: 13px; font-weight: 600; margin-bottom: 12px"
              >
                {{ auth.username }}
              </div>
              <div
                class="rpg-sidebar-item"
                style="padding: 8px 0; color: var(--rpg-ink-muted)"
                @click="auth.logout()"
              >
                Déconnexion →
              </div>
            </div>
          </div>
        </template>
      </v-navigation-drawer>

      <!-- ── App Bar ─────────────────────────────────────────────────── -->
      <v-app-bar
        flat
        height="56"
        style="
          background: var(--rpg-cream) !important;
          border-bottom: 1px solid var(--rpg-border);
        "
      >
        <v-app-bar-title>
          <span
            style="
              font-family: var(--font-sans);
              font-size: 11px;
              font-weight: 600;
              letter-spacing: 0.15em;
              text-transform: uppercase;
              color: var(--rpg-ink-muted);
            "
          >
            {{ currentPageTitle }}
          </span>
        </v-app-bar-title>

        <v-spacer />

        <div class="d-flex align-center ga-4 mr-5">
          <span class="editorial-label" style="color: var(--rpg-ink-muted)">
            {{
              new Date().toLocaleDateString("fr-FR", {
                day: "2-digit",
                month: "long",
                year: "numeric",
              })
            }}
          </span>
          <span v-if="auth.isAdmin" class="editorial-tag"> Admin </span>
        </div>
      </v-app-bar>

      <!-- ── Contenu principal ───────────────────────────────────────── -->
      <v-main>
        <v-container fluid class="pa-8">
          <RouterView />
        </v-container>
      </v-main>
    </v-layout>
  </v-app>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/stores/auth";
import { computed } from "vue";
import { RouterView, useRoute, useRouter } from "vue-router";

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();

const navItems = [
  { name: "Dashboard", label: "Tableau de bord" },
  { name: "Saves", label: "Sauvegardes" },
  { name: "Inventory", label: "Inventaire" },
  { name: "Skills", label: "Compétences" },
  { name: "Bestiary", label: "Bestiaire" },
  { name: "Rgpd", label: "Mes données" },
];

const adminItems = [
  { name: "AdminUsers", label: "Utilisateurs" },
  { name: "AdminItems", label: "Items" },
  {name: "AdminSkills", label:"Compétences"}, 
  {name:"AdminBestiary", label:"Monstres"}
];

const currentRoute = computed(() => route.name as string);

const pageTitles: Record<string, string> = {
  Dashboard: "Tableau de bord",
  Saves: "Sauvegardes",
  Inventory: "Inventaire",
  Skills: "Compétences",
  Bestiary: "Bestiaire",
  Rgpd: "Mes données",
  AdminUsers: "Gestion utilisateurs",
  AdminItems: "Gestion items",
};

const currentPageTitle = computed(
  () => pageTitles[currentRoute.value] ?? "RPG_ESI07",
);
</script>
