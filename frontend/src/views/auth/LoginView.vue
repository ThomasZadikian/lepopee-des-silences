<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="4">
        <v-card class="pa-4">
          <v-card-title class="text-h5 text-center mb-4"
            >RPG_ESI07</v-card-title
          >
          <v-card-text>
            <!-- v-model lie le champ au ref du script en temps réel -->
            <v-text-field
              v-model="form.username"
              label="Nom d'utilisateur"
              prepend-icon="mdi-account"
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

            <!-- v-if affiche l'alerte uniquement si error est non vide -->
            <v-alert v-if="error" type="error" class="mb-3">{{
              error
            }}</v-alert>
            <v-btn
              block
              color="primary"
              :loading="loading"
              @click="handleLogin"
            >
              Connexion
            </v-btn>
            <v-btn block variant="text" class="mt-2" :to="{ name: 'Register' }">
              Pas encore de compte ?
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
import { useRouter } from "vue-router";
const router = useRouter();
const auth = useAuthStore();
// reactive() pour un objet avec plusieurs champs liés à un formulaire
const form = reactive({ username: "", password: "" });
// ref() pour des valeurs simples
const loading = ref(false);
const error = ref("");
async function handleLogin() {
  error.value = "";
  loading.value = true;
  try {
    const result = await auth.login(form);
    if (result.requiresMfa) {
      await router.push({ name: "Mfa" });
    } else {
      await router.push({ name: "Dashboard" });
    }
  } catch (err: any) {
    // On affiche le message d'erreur retourné par l'API si disponible
    error.value = err.response?.data?.message ?? "Identifiants incorrects";
  } finally {
    // finally s'exécute toujours, même en cas d'erreur
    loading.value = false;
  }
}
</script>
