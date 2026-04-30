<template>
  <v-app>
    <!-- TOP BAR -->
    <v-app-bar elevation="4">
      <v-app-bar-nav-icon @click="drawer = !drawer" />

      <v-toolbar-title class="d-flex align-center">
        <v-icon start icon="mdi-sword-cross" />
        RPG_ESI07
      </v-toolbar-title>

      <v-spacer />

      <!-- Statut joueur -->
      <v-chip color="primary" variant="flat" class="mr-2">
        <v-icon start icon="mdi-account" />
        {{ auth.username }}
      </v-chip>

      <v-btn icon @click="auth.logout">
        <v-icon>mdi-logout</v-icon>
      </v-btn>
    </v-app-bar>

    <!-- SIDEBAR -->
    <v-navigation-drawer v-model="drawer" temporary elevation="8">
      <v-list nav density="compact">
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
          title="RGPD"
          :to="{ name: 'Rgpd' }"
        />

        <v-divider class="my-3" />

        <v-list-subheader v-if="auth.isAdmin">
          Administration
        </v-list-subheader>

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

    <!-- MAIN -->
    <v-main>
      <v-container fluid class="pa-6">
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
const drawer = ref(false);
</script>
