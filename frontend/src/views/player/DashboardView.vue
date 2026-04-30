<template>
  <v-container fluid>
    <!-- Chargement -->
    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" size="48" />
    </div>

    <!-- Pas de personnage créé -->
    <v-alert v-else-if="!profile" type="warning" class="mb-6">
      Aucun personnage trouvé. Connectez-vous au jeu pour créer votre
      personnage.
    </v-alert>

    <template v-else>
      <!-- HERO PLAYER -->
      <v-card rounded="lg" elevation="4" class="mb-6">
        <v-row align="center">
          <v-col cols="12" md="8">
            <v-card-item>
              <template #prepend>
                <v-avatar size="64" color="primary">
                  <v-icon size="36" icon="mdi-account" />
                </v-avatar>
              </template>
              <v-card-title class="text-h4">
                {{ profile.characterName }}
              </v-card-title>
              <v-card-subtitle>
                {{ auth.username }} ·
                <v-chip
                  :color="auth.isAdmin ? 'error' : 'success'"
                  variant="flat"
                  size="small"
                >
                  {{ auth.isAdmin ? "Administrateur" : "Joueur" }}
                </v-chip>
              </v-card-subtitle>
            </v-card-item>
          </v-col>

          <v-col cols="12" md="4">
            <v-card-text>
              <div class="d-flex justify-space-between text-caption mb-1">
                <span>Niveau {{ profile.level }}</span>
                <span>{{ profile.experience }} XP</span>
              </div>
              <v-progress-linear
                :model-value="xpPercent"
                color="primary"
                height="10"
                rounded
              />
              <div class="d-flex justify-space-between text-caption mt-2">
                <span>{{ profile.gold }} 🪙</span>
                <span>Dernière sync : {{ lastSync }}</span>
              </div>
            </v-card-text>
          </v-col>
        </v-row>
      </v-card>

      <!-- STATS HP/MP -->
      <v-row class="mb-4">
        <v-col cols="12" md="6">
          <v-card elevation="2">
            <v-card-text>
              <div class="d-flex align-center mb-2">
                <v-icon icon="mdi-heart" color="error" class="mr-2" />
                <span class="text-caption"
                  >HP — {{ profile.currentHP }} / {{ profile.maxHP }}</span
                >
              </div>
              <v-progress-linear
                :model-value="(profile.currentHP / profile.maxHP) * 100"
                color="error"
                height="8"
                rounded
              />
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="12" md="6">
          <v-card elevation="2">
            <v-card-text>
              <div class="d-flex align-center mb-2">
                <v-icon icon="mdi-lightning-bolt" color="info" class="mr-2" />
                <span class="text-caption"
                  >MP — {{ profile.currentMP }} / {{ profile.maxMP }}</span
                >
              </div>
              <v-progress-linear
                :model-value="(profile.currentMP / profile.maxMP) * 100"
                color="info"
                height="8"
                rounded
              />
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- COMPTEURS -->
      <v-row class="mb-4">
        <v-col cols="6" md="3">
          <v-card elevation="2">
            <v-card-text class="text-center">
              <v-icon size="32" icon="mdi-content-save" color="primary" />
              <div class="text-h5 mt-2">{{ profile.savesCount }}</div>
              <div class="text-caption">Sauvegardes</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="6" md="3">
          <v-card elevation="2">
            <v-card-text class="text-center">
              <v-icon size="32" icon="mdi-bag-personal" color="secondary" />
              <div class="text-h5 mt-2">{{ profile.inventoryCount }}</div>
              <div class="text-caption">Items</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="6" md="3">
          <v-card elevation="2">
            <v-card-text class="text-center">
              <v-icon size="32" icon="mdi-lightning-bolt" color="warning" />
              <div class="text-h5 mt-2">{{ profile.skillsCount }}</div>
              <div class="text-caption">Compétences</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="6" md="3">
          <v-card elevation="2">
            <v-card-text class="text-center">
              <v-icon size="32" icon="mdi-book-open" color="success" />
              <div class="text-h5 mt-2">{{ profile.bestiaryCount }}</div>
              <div class="text-caption">Bestiaire</div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- STATS DE COMBAT -->
      <v-row class="mb-4" v-if="profile.totalCombats !== null">
        <v-col cols="12">
          <v-card elevation="2">
            <v-card-title>
              <v-icon start icon="mdi-sword-cross" />
              Stats de combat
            </v-card-title>
            <v-card-text>
              <v-row>
                <v-col cols="6" md="2" class="text-center">
                  <div class="text-h6">{{ profile.totalCombats }}</div>
                  <div class="text-caption">Combats</div>
                </v-col>
                <v-col cols="6" md="2" class="text-center">
                  <div class="text-h6 text-success">
                    {{ profile.combatsWon }}
                  </div>
                  <div class="text-caption">Victoires</div>
                </v-col>
                <v-col cols="6" md="2" class="text-center">
                  <div class="text-h6 text-error">
                    {{ profile.combatsLost }}
                  </div>
                  <div class="text-caption">Défaites</div>
                </v-col>
                <v-col cols="6" md="3" class="text-center">
                  <div class="text-h6">
                    {{ profile.totalDamageDealt?.toLocaleString() }}
                  </div>
                  <div class="text-caption">Dégâts infligés</div>
                </v-col>
                <v-col cols="6" md="3" class="text-center">
                  <div class="text-h6">
                    {{ Math.round((profile.totalPlaytimeMinutes ?? 0) / 60) }}h
                  </div>
                  <div class="text-caption">Temps de jeu</div>
                </v-col>
              </v-row>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- RACCOURCIS -->
      <v-row>
        <v-col cols="12" md="4">
          <v-card elevation="2" hover>
            <v-card-item>
              <template #prepend>
                <v-avatar color="primary">
                  <v-icon icon="mdi-content-save" />
                </v-avatar>
              </template>
              <v-card-title>Sauvegardes</v-card-title>
              <v-card-subtitle>Gérez vos parties</v-card-subtitle>
            </v-card-item>
            <v-card-actions>
              <v-btn block color="primary" :to="{ name: 'Saves' }"
                >Accéder</v-btn
              >
            </v-card-actions>
          </v-card>
        </v-col>
        <v-col cols="12" md="4">
          <v-card elevation="2" hover>
            <v-card-item>
              <template #prepend>
                <v-avatar color="secondary">
                  <v-icon icon="mdi-bag-personal" />
                </v-avatar>
              </template>
              <v-card-title>Inventaire</v-card-title>
              <v-card-subtitle>Équipement & objets</v-card-subtitle>
            </v-card-item>
            <v-card-actions>
              <v-btn block color="secondary" :to="{ name: 'Inventory' }"
                >Accéder</v-btn
              >
            </v-card-actions>
          </v-card>
        </v-col>
        <v-col cols="12" md="4">
          <v-card elevation="2" hover>
            <v-card-item>
              <template #prepend>
                <v-avatar color="error">
                  <v-icon icon="mdi-shield-account" />
                </v-avatar>
              </template>
              <v-card-title>RGPD</v-card-title>
              <v-card-subtitle>Données personnelles</v-card-subtitle>
            </v-card-item>
            <v-card-actions>
              <v-btn block color="error" :to="{ name: 'Rgpd' }">Gérer</v-btn>
            </v-card-actions>
          </v-card>
        </v-col>
      </v-row>
    </template>
  </v-container>
</template>

<script setup lang="ts">
import {
  playerProfileApi,
  type PlayerProfileResponse,
} from "@/interfaces/playerProfile";
import { useAuthStore } from "@/stores/auth";
import { computed, onMounted, ref } from "vue";

const auth = useAuthStore();
const profile = ref<PlayerProfileResponse | null>(null);
const loading = ref(true);

// XP vers le prochain niveau (simplifié : 1000 XP par niveau)
const xpPercent = computed(() => {
  if (!profile.value) return 0;
  return (profile.value.experience % 1000) / 10;
});

const lastSync = computed(() => {
  if (!profile.value) return "—";
  return new Date(profile.value.updatedAt).toLocaleString("fr-FR");
});

onMounted(async () => {
  try {
    const res = await playerProfileApi.getMe();
    profile.value = res.data;
  } catch {
    // Pas de personnage créé — on affiche le message d'avertissement
    profile.value = null;
  } finally {
    loading.value = false;
  }
});
</script>
