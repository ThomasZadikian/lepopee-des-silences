<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="pa-4">
          <v-card-title class="text-h5 text-center mb-4"
            >Créer un compte</v-card-title
          >
          <v-card-text>
            <v-text-field
              v-model="form.username"
              label="Nom d'utilisateur"
              prepend-icon="mdi-account"
              variant="outlined"
              class="mb-3"
            />
            <v-text-field
              v-model="form.email"
              label="Email"
              prepend-icon="mdi-email"
              type="email"
              variant="outlined"
              class="mb-3"
            />
            <v-text-field
              v-model="form.password"
              label="Mot de passe"
              prepend-icon="mdi-lock"
              type="password"
              variant="outlined"
              class="mb-3"
            />
            <v-alert v-if="error" type="error" class="mb-3">{{
              error
            }}</v-alert>
            <v-alert v-if="success" type="success" class="mb-3">{{
              success
            }}</v-alert>
            <v-btn
              block
              color="primary"
              :loading="loading"
              @click="handleRegister"
            >
              S'inscrire
            </v-btn>
            <v-btn block variant="text" class="mt-2" :to="{ name: 'Login' }">
              Déjà un compte ?
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/stores/auth";
import { reactive, ref } from "vue";
const auth = useAuthStore();
const form = reactive({ username: "", email: "", password: "" });
const loading = ref(false);
const error = ref("");
const success = ref("");
async function handleRegister() {
  error.value = "";
  success.value = "";
  loading.value = true;
  try {
    await auth.register(form);
    success.value = "Compte créé ! Redirection vers la connexion...";
  } catch (err: any) {
    error.value = err.response?.data?.message ?? "Erreur lors de l'inscription";
  } finally {
    loading.value = false;
  }
}
</script>
