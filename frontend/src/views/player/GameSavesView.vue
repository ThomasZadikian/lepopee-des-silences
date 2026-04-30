<template>
  <div>
    <h1 class="text-h4 mb-6">Mes sauvegardes</h1>

    <v-progress-circular v-if="loading" indeterminate color="primary" />

    <v-alert v-else-if="saves.length === 0" type="info">
      Aucune sauvegarde trouvée.
    </v-alert>

    <v-row v-else>
      <v-col v-for="save in saves" :key="save.id" cols="12" md="4">
        <v-card>
          <v-card-title>Zone : {{ save.currentZone }}</v-card-title>
          <v-card-subtitle>
            Position : ({{ save.positionX }}, {{ save.positionY }})
          </v-card-subtitle>
          <v-card-text>
            <div class="text-caption text-medium-emphasis">
              Sauvegardé le {{ new Date(save.savedAt).toLocaleString("fr-FR") }}
            </div>
          </v-card-text>
          <v-card-actions>
            <v-btn color="error" variant="text" @click="deleteSave(save.id)">
              Supprimer
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { gameSavesApi } from "@/api/gameSave";
import type { GameSave } from "@/interfaces/gameSave";
import { useAuthStore } from "@/stores/auth";
import { onMounted, ref } from "vue";

const auth = useAuthStore();
const saves = ref<GameSave[]>([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await gameSavesApi.getAll();
    saves.value = res.data.items ?? [];
  } finally {
    loading.value = false;
  }
});

async function deleteSave(id: number) {
  await gameSavesApi.delete(id);
  saves.value = saves.value.filter((s) => s.id !== id);
}
</script>
