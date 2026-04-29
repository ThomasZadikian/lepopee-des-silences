<template>
  <div>
    <h1 class="text-h4 mb-6">Tableau de bord</h1>
    <v-row>
      <v-col cols="12" md="4">
        <v-card class="pa-4" color="primary" variant="tonal">
          <v-card-title>
            <v-icon class="mr-2">mdi-account</v-icon>
            {{ auth.username }}
          </v-card-title>
          <v-card-text>
            <v-chip :color="auth.isAdmin ? 'error' : 'success'">
              {{ auth.isAdmin ? "Administrateur" : "Joueur" }}
            </v-chip>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card class="pa-4" variant="tonal">
          <v-card-title
            ><v-icon class="mr-2">mdi-content-save</v-icon
            >Sauvegardes</v-card-title
          >

          <v-card-actions>
            <v-btn :to="{ name: 'Saves' }" variant="text"
              >Voir mes sauvegardes</v-btn
            >
          </v-card-actions>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card class="pa-4" variant="tonal">
          <v-card-title
            ><v-icon class="mr-2">mdi-shield-account</v-icon>Mes
            données</v-card-title
          >
          <v-card-actions>
            <v-btn :to="{ name: 'Gdpr' }" variant="text"
              >Gérer mes données RGPD</v-btn
            >
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>
<script setup lang="ts">
import { useAuthStore } from "@/stores/auth";
const auth = useAuthStore();
</script>
