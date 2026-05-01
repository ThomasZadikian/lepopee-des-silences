<template>
  <div>
    <!-- Chargement -->
    <div
      v-if="loading"
      class="d-flex justify-center align-center"
      style="min-height: 60vh"
    >
      <v-progress-circular indeterminate color="primary" size="40" width="2" />
    </div>

    <!-- Pas de personnage -->
    <div
      v-else-if="!profile"
      class="d-flex justify-center align-center"
      style="min-height: 60vh"
    >
      <div class="text-center">
        <div class="editorial-label mb-4">Aucun personnage</div>
        <div class="editorial-title" style="font-size: 2rem">
          Commencez dans le jeu
        </div>
        <div class="mt-3" style="color: var(--rpg-ink-muted); font-size: 14px">
          Connectez-vous au jeu pour créer votre personnage.
        </div>
      </div>
    </div>

    <template v-else>
      <!-- ── EN-TÊTE HERO ─────────────────────────────────────────── -->
      <div class="grid-border-bottom pb-6 mb-6">
        <div class="editorial-label mb-4">Profil du joueur</div>
        <div
          class="d-flex align-start justify-space-between flex-wrap"
          style="gap: 32px"
        >
          <!-- Nom + identité -->
          <div>
            <div
              class="editorial-title"
              style="font-size: clamp(2.5rem, 5vw, 4rem)"
            >
              {{ profile.characterName }}
            </div>
            <div class="d-flex align-center ga-3 mt-2">
              <span style="color: var(--rpg-ink-muted); font-size: 13px">{{
                auth.username
              }}</span>
              <span class="editorial-tag">{{
                auth.isAdmin ? "Admin" : "Joueur"
              }}</span>
            </div>
          </div>

          <!-- Stats rapides -->
          <div class="d-flex ga-6 flex-wrap">
            <div>
              <div class="editorial-label mb-1">Niveau</div>
              <div class="editorial-value">{{ profile.level }}</div>
            </div>
            <div>
              <div class="editorial-label mb-1">Expérience</div>
              <div class="editorial-value">
                {{ profile.experience.toLocaleString() }}
              </div>
            </div>
            <div>
              <div class="editorial-label mb-1">Or</div>
              <div class="editorial-value">
                {{ profile.gold.toLocaleString() }}
              </div>
            </div>
            <div>
              <div class="editorial-label mb-1">Dernière sync</div>
              <div
                style="
                  font-family: var(--font-serif);
                  font-size: 1.1rem;
                  font-weight: 600;
                "
              >
                {{ lastSync }}
              </div>
            </div>
          </div>
        </div>

        <!-- Barre XP -->
        <div class="mt-5">
          <div class="d-flex justify-space-between editorial-label mb-2">
            <span>Progression niveau {{ profile.level }}</span>
            <span>{{ profile.experience % 1000 }} / 1000 xp</span>
          </div>
          <v-progress-linear
            :model-value="xpPercent"
            height="3"
            class="editorial-progress"
            bg-color="rgba(0,0,0,0.08)"
          />
        </div>
      </div>

      <!-- ── HP / MP / STATS ────────────────────────────────────────── -->
      <v-row class="mb-6" no-gutters>
        <v-col cols="12" md="3" class="pr-md-4 mb-4 mb-md-0">
          <div class="editorial-label mb-2">Points de vie</div>
          <div class="d-flex align-baseline ga-1 mb-2">
            <span
              style="
                font-family: var(--font-serif);
                font-size: 1.8rem;
                font-weight: 700;
              "
            >
              {{ profile.currentHP }}
            </span>
            <span style="color: var(--rpg-ink-muted); font-size: 13px"
              >/ {{ profile.maxHP }}</span
            >
          </div>
          <v-progress-linear
            :model-value="(profile.currentHP / profile.maxHP) * 100"
            height="3"
            bg-color="rgba(0,0,0,0.08)"
            style="--v-progress-linear-determinate-background: #c0392b"
            class="editorial-progress"
          />
        </v-col>

        <v-col
          cols="12"
          md="3"
          class="pr-md-4 mb-4 mb-md-0"
          :class="{ 'pl-md-4': true }"
        >
          <div class="editorial-label mb-2">Points de magie</div>
          <div class="d-flex align-baseline ga-1 mb-2">
            <span
              style="
                font-family: var(--font-serif);
                font-size: 1.8rem;
                font-weight: 700;
              "
            >
              {{ profile.currentMP }}
            </span>
            <span style="color: var(--rpg-ink-muted); font-size: 13px"
              >/ {{ profile.maxMP }}</span
            >
          </div>
          <v-progress-linear
            :model-value="(profile.currentMP / profile.maxMP) * 100"
            height="3"
            bg-color="rgba(0,0,0,0.08)"
            class="editorial-progress-accent"
          />
        </v-col>

        <v-col cols="6" md="3" class="pl-md-4">
          <div class="editorial-label mb-2">Force / Int. / Vitesse</div>
          <div
            style="
              font-family: var(--font-serif);
              font-size: 1.2rem;
              font-weight: 700;
            "
          >
            {{ profile.strength }} · {{ profile.intelligence }} ·
            {{ profile.speed }}
          </div>
        </v-col>
      </v-row>

      <!-- ── COMPTEURS ──────────────────────────────────────────────── -->
      <div class="grid-border-top grid-border-bottom py-4 mb-6">
        <v-row no-gutters>
          <v-col
            v-for="(stat, i) in statCards"
            :key="stat.label"
            cols="6"
            md="3"
            :class="{ 'border-right': i < 3 }"
            style="border-right: 1px solid rgba(0, 0, 0, 0.08)"
          >
            <div class="pa-4 text-center">
              <div class="editorial-label mb-2">{{ stat.label }}</div>
              <div class="editorial-value">{{ stat.value }}</div>
            </div>
          </v-col>
        </v-row>
      </div>

      <!-- ── STATS DE COMBAT ────────────────────────────────────────── -->
      <div v-if="profile.totalCombats !== null" class="mb-6">
        <div class="editorial-label mb-4">Stats de combat</div>
        <v-row no-gutters class="grid-border-top">
          <v-col
            v-for="(stat, i) in combatStats"
            :key="stat.label"
            cols="6"
            md="2"
            class="pa-4"
            :style="i < 5 ? 'border-right: 1px solid rgba(0,0,0,0.08);' : ''"
          >
            <div class="editorial-label mb-1">{{ stat.label }}</div>
            <div
              style="
                font-family: var(--font-serif);
                font-size: 1.5rem;
                font-weight: 700;
              "
            >
              {{ stat.value }}
            </div>
          </v-col>
        </v-row>
      </div>

      <!-- ── RACCOURCIS ─────────────────────────────────────────────── -->
      <div class="editorial-label mb-4">Accès rapide</div>
      <v-row>
        <v-col
          v-for="shortcut in shortcuts"
          :key="shortcut.name"
          cols="12"
          md="4"
        >
          <div
            class="editorial-card pa-5"
            style="cursor: pointer; transition: background 0.15s ease"
            @mouseenter="
              (e) =>
                ((e.currentTarget as HTMLElement).style.background =
                  'rgba(0,0,0,0.03)')
            "
            @mouseleave="
              (e) =>
                ((e.currentTarget as HTMLElement).style.background =
                  'transparent')
            "
            @click="router.push({ name: shortcut.name })"
          >
            <div class="editorial-label mb-3">{{ shortcut.category }}</div>
            <div
              style="
                font-family: var(--font-serif);
                font-size: 1.3rem;
                font-weight: 700;
                margin-bottom: 4px;
              "
            >
              {{ shortcut.title }}
            </div>
            <div
              style="
                color: var(--rpg-ink-muted);
                font-size: 13px;
                margin-bottom: 16px;
              "
            >
              {{ shortcut.subtitle }}
            </div>
            <div
              style="
                font-size: 11px;
                font-weight: 600;
                letter-spacing: 0.1em;
                text-transform: uppercase;
              "
            >
              Accéder →
            </div>
          </div>
        </v-col>
      </v-row>
    </template>
  </div>
</template>

<script setup lang="ts">
import {
  playerProfileApi,
  type PlayerProfileResponse,
} from "@/interfaces/playerProfile";
import { useAuthStore } from "@/stores/auth";
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";

const auth = useAuthStore();
const router = useRouter();
const profile = ref<PlayerProfileResponse | null>(null);
const loading = ref(true);

const xpPercent = computed(() => {
  if (!profile.value) return 0;
  return (profile.value.experience % 1000) / 10;
});

const lastSync = computed(() => {
  if (!profile.value) return "—";
  return new Date(profile.value.updatedAt).toLocaleString("fr-FR");
});

const statCards = computed(() => [
  { label: "Sauvegardes", value: profile.value?.savesCount ?? 0 },
  { label: "Items", value: profile.value?.inventoryCount ?? 0 },
  { label: "Compétences", value: profile.value?.skillsCount ?? 0 },
  { label: "Bestiaire", value: profile.value?.bestiaryCount ?? 0 },
]);

const combatStats = computed(() => [
  { label: "Combats", value: profile.value?.totalCombats ?? 0 },
  { label: "Victoires", value: profile.value?.combatsWon ?? 0 },
  { label: "Défaites", value: profile.value?.combatsLost ?? 0 },
  {
    label: "Dégâts infligés",
    value: profile.value?.totalDamageDealt?.toLocaleString() ?? 0,
  },
  {
    label: "Dégâts reçus",
    value: profile.value?.totalDamageTaken?.toLocaleString() ?? 0,
  },
  {
    label: "Temps de jeu",
    value: `${Math.round((profile.value?.totalPlaytimeMinutes ?? 0) / 60)}h`,
  },
]);

const shortcuts = [
  {
    name: "Saves",
    category: "Progression",
    title: "Sauvegardes",
    subtitle: "Consultez vos parties sauvegardées",
  },
  {
    name: "Inventory",
    category: "Équipement",
    title: "Inventaire",
    subtitle: "Gérez votre équipement et vos objets",
  },
  {
    name: "Rgpd",
    category: "Données",
    title: "Mes données",
    subtitle: "Gestion de vos données personnelles",
  },
];

onMounted(async () => {
  try {
    const res = await playerProfileApi.getMe();
    profile.value = res.data;
  } catch {
    profile.value = null;
  } finally {
    loading.value = false;
  }
});
</script>
