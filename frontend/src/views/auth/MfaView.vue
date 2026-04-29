<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="pa-4">
          <v-card-title class="text-h5 text-center mb-2">
            Vérification MFA
          </v-card-title>
          <v-card-subtitle class="text-center mb-4">
            Saisir le code de votre application d'authentification
          </v-card-subtitle>
          <v-card-text>
            <v-text-field
              v-model="code"
              label="Code à 6 chiffres"
              prepend-icon="mdi-shield-key"
              variant="outlined"
              maxlength="6"
              class="mb-3"
            />
            <v-alert v-if="error" type="error" class="mb-3">{{
              error
            }}</v-alert>
            <v-btn block color="primary" :loading="loading" @click="handleMfa">
              Valider
            </v-btn>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { useAuthStore } from "@/stores/auth";
import { ref } from "vue";
import { useRouter } from "vue-router";
const router = useRouter();
const auth = useAuthStore();
const code = ref("");
const loading = ref(false);
const error = ref("");
async function handleMfa() {
  error.value = "";
  loading.value = true;
  try {
    await auth.verifyMfa(code.value);
    await router.push({ name: "Dashboard" });
  } catch {
    error.value = "Code incorrect ou expiré";
  } finally {
    loading.value = false;
  }
}
</script>
