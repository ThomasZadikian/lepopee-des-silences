<!-- InventoryView.vue -->
<template>
  <v-container fluid>
    <div class="text-h4 font-weight-bold mb-6">Inventaire</div>

    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" size="48" />
    </div>

    <v-alert v-else-if="inventory.length === 0" type="info">
      Votre inventaire est vide.
    </v-alert>

    <v-row v-else>
      <v-col
        v-for="entry in inventory"
        :key="entry.id"
        cols="12"
        sm="6"
        md="4"
        lg="3"
      >
        <v-card height="100%">
          <v-card-item>
            <template #prepend>
              <v-avatar
                :color="typeColor(entry.item.type ?? '')"
                variant="tonal"
                size="40"
              >
                <v-icon :icon="typeIcon(entry.item.type ?? '')" />
              </v-avatar>
            </template>
            <v-card-title>{{ entry.item.name }}</v-card-title>
            <v-card-subtitle>
              <v-chip
                size="x-small"
                :color="typeColor(entry.item.type ?? '')"
                variant="flat"
              >
                {{ entry.item.type ?? "" }}
              </v-chip>
              <v-chip
                v-if="entry.isEquipped"
                size="x-small"
                color="success"
                variant="flat"
                class="ml-1"
              >
                Équipé
              </v-chip>
            </v-card-subtitle>
          </v-card-item>

          <v-card-text>
            <div class="text-caption text-medium-emphasis mb-2">
              {{ entry.item.description }}
            </div>

            <v-divider class="mb-2" />

            <div class="d-flex justify-space-between text-caption">
              <span
                >Quantité : <strong>{{ entry.quantity }}</strong></span
              >
              <span
                >Prix : <strong>{{ entry.item.price }} 🪙</strong></span
              >
            </div>

            <!-- Modificateurs de stats -->
            <div v-if="entry.item.statModifiers" class="mt-2">
              <div
                v-for="(value, stat) in parsedMods(entry.item.statModifiers)"
                :key="stat"
                class="d-flex justify-space-between text-caption"
              >
                <span class="text-medium-emphasis">{{
                  statLabel(String(stat))
                }}</span>
                <span
                  :class="Number(value) >= 0 ? 'text-success' : 'text-error'"
                >
                  {{ Number(value) >= 0 ? "+" : "" }}{{ value }}
                </span>
              </div>
            </div>

            <!-- Valeur d'effet pour les consommables -->
            <div v-if="entry.item.effectValue" class="mt-1 text-caption">
              <span class="text-medium-emphasis">Effet : </span>
              <span class="text-success">+{{ entry.item.effectValue }}</span>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { inventoryApi } from "@/api/inventory";
import type { PlayerInventory } from "@/interfaces/playerInventory";
import { onMounted, ref } from "vue";

const inventory = ref<PlayerInventory[]>([]);
const loading = ref(true);

onMounted(async () => {
  try {
    const res = await inventoryApi.getMe();
    inventory.value = res.data.items ?? [];
  } finally {
    loading.value = false;
  }
});

function typeColor(type: string): string {
  const map: Record<string, string> = {
    weapon: "error",
    armor: "primary",
    accessory: "secondary",
    consumable: "success",
  };
  return map[type] ?? "grey";
}

function typeIcon(type: string): string {
  const map: Record<string, string> = {
    weapon: "mdi-sword",
    armor: "mdi-shield",
    accessory: "mdi-ring",
    consumable: "mdi-bottle-tonic",
  };
  return map[type] ?? "mdi-help";
}

function statLabel(stat: string): string {
  const map: Record<string, string> = {
    strength: "Force",
    intelligence: "Intelligence",
    speed: "Vitesse",
    maxHP: "HP max",
    maxMP: "MP max",
  };
  return map[stat] ?? stat;
}

function parsedMods(raw: string): Record<string, number> {
  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}
</script>
