<template>
  <div>
    <h1 class="text-h4 mb-2">Mes données personnelles (RGPD)</h1>
    <p class="text-body-1 mb-6 text-medium-emphasis">
      Conformément au RGPD, vous pouvez accéder, exporter ou supprimer vos
      données.
    </p>
    <v-row>
      <v-col cols="12" md="4">
        <v-card class="pa-4" variant="tonal">
          <v-card-title>Art. 15 — Droit d'accès</v-card-title>
          <v-card-text
            >Voir toutes vos données personnelles stockées.</v-card-text
          >
          <v-card-actions>
            <v-btn color="primary" @click="exportData">Voir mes données</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card class="pa-4" variant="tonal">
          <v-card-title>Art. 20 — Portabilité</v-card-title>
          <v-card-text>Télécharger vos données au format JSON.</v-card-text>
          <v-card-actions>
            <v-btn color="info" @click="downloadJson">Télécharger JSON</v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
      <v-col cols="12" md="4">
        <v-card class="pa-4" variant="tonal" color="error">
          <v-card-title>Art. 17 — Droit à l'effacement</v-card-title>

          <v-card-text
            >Supprimer définitivement votre compte et vos données.</v-card-text
          >

          <v-card-actions>
            <v-btn color="error" @click="confirmDelete = true"
              >Supprimer mon compte</v-btn
            >
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
    <!-- Dialogue de confirmation avant suppression -->
    <v-dialog v-model="confirmDelete" max-width="400">
      <v-card>
        <v-card-title>Confirmer la suppression</v-card-title>
        <v-card-text>
          Cette action est <b>irréversible</b>. Votre compte sera anonymisé.
          <v-text-field
            v-model="deleteReason"
            label="Raison (optionnel)"
            variant="outlined"
            class="mt-4"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="confirmDelete = false">Annuler</v-btn>
          <v-btn color="error" @click="deleteAccount">Confirmer</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { rgpdApi } from "@/api/rgpd";
import { useAuthStore } from "@/stores/auth";
import { ref } from "vue";
const auth = useAuthStore();
const confirmDelete = ref(false);
const deleteReason = ref("");
async function exportData() {
  const res = await rgpdApi.exportData();
  alert(JSON.stringify(res.data, null, 2));
}
async function downloadJson() {
  const res = await rgpdApi.exportJson();
  const url = URL.createObjectURL(new Blob([res.data]));
  const link = document.createElement("a");
  link.href = url;
  link.download = `mes-donnees-rgpd-${auth.userId}.json`;
  link.click();
  URL.revokeObjectURL(url);
}
async function deleteAccount() {
  await rgpdApi.deleteAccount(deleteReason.value || undefined);
  auth.logout();
}
</script>
