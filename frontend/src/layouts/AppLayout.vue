<template>
  <v-app>
    <!-- Barre de navigation supérieure -->
    <v-app-bar color="primary" elevation="2">
      <v-app-bar-nav-icon @click="drawer = !drawer" />
      <v-toolbar-title>RPG_ESI07</v-toolbar-title>
      <v-spacer />
      <v-chip color="secondary" class="mr-3">{{ auth.username }}</v-chip>
      <v-btn icon @click="auth.logout">
        <v-icon>mdi-logout</v-icon>
      </v-btn>
    </v-app-bar>
    <!-- Sidebar de navigation -->
    <v-navigation-drawer v-model="drawer" temporary>
      <v-list density="compact" nav>
        <v-list-item
          prepend-icon="mdi-view-dashboard"
          title="Dashboard"
          :to="{ name: 'Dashboard' }"
        />
        <v-list-item
          prepend-icon="mdi-content-save"
          title="Sauvegardes"
          :to="{ name: 'Saves' }"
        />
        <v-list-item
          prepend-icon="mdi-bag-personal"
          title="Inventaire"
          :to="{ name: 'Inventory' }"
        />
        <v-list-item
          prepend-icon="mdi-lightning-bolt"
          title="Compétences"
          :to="{ name: 'Skills' }"
        />
        <v-list-item
          prepend-icon="mdi-shield-account"
          title="Mes données (RGPD)"
          :to="{ name: 'Rgpd' }"
        />
        <!-- Section Admin — affichée uniquement si l'utilisateur est Admin -->
        <v-divider v-if="auth.isAdmin" class="my-2" />
        <v-list-subheader v-if="auth.isAdmin">Administration</v-list-subheader>
        <v-list-item
          v-if="auth.isAdmin"
          prepend-icon="mdi-account-group"
          title="Utilisateurs"
          :to="{ name: 'AdminUsers' }"
        />
        <v-list-item
          v-if="auth.isAdmin"
          prepend-icon="mdi-sword"
          title="Items"
          :to="{ name: 'AdminItems' }"
        />
      </v-list>
    </v-navigation-drawer>
    <!-- Zone de contenu : RouterView affiche la page active -->
    <v-main>
      <v-container fluid class="pa-4">
        <RouterView />
      </v-container>
    </v-main>
  </v-app>
</template>
<script setup lang="ts">
import { useAuthStore } from "@/stores/auth";
import { ref } from "vue";
import { RouterView } from "vue-router";
const auth = useAuthStore();
// drawer contrôle l'ouverture/fermeture de la sidebar
const drawer = ref(false);
</script>
