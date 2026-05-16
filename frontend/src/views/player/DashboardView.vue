<template>
  <div>
    <!-- Chargement -->
    <div
      v-if="loading"
      class="d-flex justify-center align-center"
      style="min-height: 60vh;"
    >
      <v-progress-circular
        indeterminate
        color="primary"
        size="40"
        width="2"
      />
    </div>

    <!-- Pas de personnage -->
    <div
      v-else-if="!profile"
      class="d-flex justify-center align-center"
      style="min-height: 60vh;"
    >
      <div class="text-center">
        <div class="editorial-label mb-4">
          Aucun personnage
        </div>
        <div
          class="editorial-title"
          style="font-size: 2rem;"
        >
          Commencez dans le jeu
        </div>
        <div
          class="mt-3"
          style="color: var(--rpg-ink-muted); font-size: 14px;"
        >
          Connectez-vous au jeu pour créer votre personnage.
        </div>
      </div>
    </div>

    <template v-else>
      <!-- ── HERO éditorial ─────────────────────────────────────────── -->
      <div
        style="
  margin: -32px -32px 40px -32px;
  position: relative; overflow: hidden;
  border-bottom: 1px solid var(--rpg-border);
"
      >
        <div style="display: grid; grid-template-columns: 1fr 1fr 1fr 1fr; min-height: 160px;">
          <div style="padding: 40px 32px; border-right: 1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              {{ auth.username }} · Profil
            </div>
            <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,3rem);font-weight:900;line-height:1;letter-spacing:-0.03em;margin-bottom:12px;">
              {{ profile.characterName }}
              <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">
                {{ auth.isAdmin ? '· Admin.' : '· Joueur.' }}
              </span>
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Niveau
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ profile.level }}
            </div>
            <div style="margin-top:12px;">
              <div class="d-flex justify-space-between editorial-label mb-1">
                <span>XP</span>
                <span>{{ profile.experience % 1000 }} / 1000</span>
              </div>
              <div style="height:2px;background:rgba(0,0,0,0.08);">
                <div :style="`width:${xpPercent}%;height:100%;background:var(--rpg-ink);`" />
              </div>
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Or
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ profile.gold.toLocaleString() }}
            </div>
            <div class="editorial-label mt-2">
              🪙 pièces
            </div>
          </div>
          <div style="padding:40px 32px;">
            <div class="editorial-label mb-3">
              Dernière sync
            </div>
            <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;line-height:1.2;">
              {{ lastSync }}
            </div>
          </div>
        </div>
      </div>
      <!-- ── HP / MP ─────────────────────────────────────────────────── -->
      <v-row
        class="mb-6"
        no-gutters
      >
        <v-col
          cols="12"
          md="4"
          class="pr-md-6 mb-4 mb-md-0"
        >
          <div class="editorial-label mb-2">
            Points de vie
          </div>
          <div class="d-flex align-baseline ga-1 mb-2">
            <span style="font-family: var(--font-serif); font-size: 2rem; font-weight: 700;">
              {{ profile.currentHP }}
            </span>
            <span style="color: var(--rpg-ink-muted); font-size: 13px;">/ {{ profile.maxHP }}</span>
          </div>
          <div style="height: 2px; background: rgba(0,0,0,0.08);">
            <div :style="`width: ${(profile.currentHP / profile.maxHP) * 100}%; height: 100%; background: #C0392B;`" />
          </div>
        </v-col>

        <v-col
          cols="12"
          md="4"
          class="px-md-6 mb-4 mb-md-0"
        >
          <div class="editorial-label mb-2">
            Points de magie
          </div>
          <div class="d-flex align-baseline ga-1 mb-2">
            <span style="font-family: var(--font-serif); font-size: 2rem; font-weight: 700;">
              {{ profile.currentMP }}
            </span>
            <span style="color: var(--rpg-ink-muted); font-size: 13px;">/ {{ profile.maxMP }}</span>
          </div>
          <div style="height: 2px; background: rgba(0,0,0,0.08);">
            <div :style="`width: ${(profile.currentMP / profile.maxMP) * 100}%; height: 100%; background: #2D4A8A;`" />
          </div>
        </v-col>

        <v-col
          cols="12"
          md="4"
          class="pl-md-6"
        >
          <div class="editorial-label mb-2">
            Attributs
          </div>
          <div style="font-family: var(--font-serif); font-size: 1.4rem; font-weight: 700;">
            {{ profile.strength }} · {{ profile.intelligence }} · {{ profile.speed }}
          </div>
          <div class="editorial-label mt-1">
            Force · Intelligence · Vitesse
          </div>
        </v-col>
      </v-row>

      <!-- ── COMPTEURS ──────────────────────────────────────────────── -->
      <div style="border-top: 1px solid rgba(0,0,0,0.08); border-bottom: 1px solid rgba(0,0,0,0.08); margin-bottom: 40px;">
        <v-row no-gutters>
          <v-col
            v-for="(stat, i) in statCards"
            :key="stat.label"
            cols="6"
            md="3"
            :style="i < 3 ? 'border-right: 1px solid rgba(0,0,0,0.08);' : ''"
          >
            <div class="pa-5 text-center">
              <div class="editorial-label mb-3">
                {{ stat.label }}
              </div>
              <div style="font-family: var(--font-serif); font-size: 2.5rem; font-weight: 700; line-height: 1;">
                {{ stat.value }}
              </div>
            </div>
          </v-col>
        </v-row>
      </div>

      <!-- ── STATS DE COMBAT ────────────────────────────────────────── -->
      <div
        v-if="profile.totalCombats !== null"
        style="margin-bottom: 40px;"
      >
        <div class="editorial-label mb-4">
          Stats de combat
        </div>
        <v-row
          no-gutters
          style="border-top: 1px solid rgba(0,0,0,0.08);"
        >
          <v-col
            v-for="(stat, i) in combatStats"
            :key="stat.label"
            cols="6"
            md="2"
            class="pa-4"
            :style="i < 5 ? 'border-right: 1px solid rgba(0,0,0,0.08);' : ''"
          >
            <div class="editorial-label mb-2">
              {{ stat.label }}
            </div>
            <div style="font-family: var(--font-serif); font-size: 1.6rem; font-weight: 700; line-height: 1;">
              {{ stat.value }}
            </div>
          </v-col>
        </v-row>
      </div>

      <!-- ── RACCOURCIS ─────────────────────────────────────────────── -->
      <div class="editorial-label mb-4">
        Accès rapide
      </div>
      <v-row>
        <v-col
          v-for="shortcut in shortcuts"
          :key="shortcut.name"
          cols="12"
          md="4"
        >
          <div
            style="
              border: 1px solid rgba(0,0,0,0.08);
              padding: 24px;
              cursor: pointer;
              transition: background 0.15s ease;
            "
            @mouseenter="e => (e.currentTarget as HTMLElement).style.background = 'rgba(0,0,0,0.02)'"
            @mouseleave="e => (e.currentTarget as HTMLElement).style.background = 'transparent'"
            @click="router.push({ name: shortcut.name })"
          >
            <div class="editorial-label mb-3">
              {{ shortcut.category }}
            </div>
            <div style="font-family: var(--font-serif); font-size: 1.3rem; font-weight: 700; margin-bottom: 6px;">
              {{ shortcut.title }}
            </div>
            <div style="color: var(--rpg-ink-muted); font-size: 13px; margin-bottom: 20px;">
              {{ shortcut.subtitle }}
            </div>
            <div style="font-size: 11px; font-weight: 600; letter-spacing: 0.1em; text-transform: uppercase;">
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
} from '@/interfaces/playerProfile'
import { useAuthStore } from '@/stores/auth'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'

const auth    = useAuthStore()
const router  = useRouter()
const profile = ref<PlayerProfileResponse | null>(null)
const loading = ref(true)

const xpPercent = computed(() => {
  if (!profile.value) return 0
  return (profile.value.experience % 1000) / 10
})

const lastSync = computed(() => {
  if (!profile.value) return '—'
  return new Date(profile.value.updatedAt).toLocaleString('fr-FR')
})

const statCards = computed(() => [
  { label: 'Sauvegardes', value: profile.value?.savesCount ?? 0 },
  { label: 'Items',        value: profile.value?.inventoryCount ?? 0 },
  { label: 'Compétences',  value: profile.value?.skillsCount ?? 0 },
  { label: 'Bestiaire',    value: profile.value?.bestiaryCount ?? 0 },
])

const combatStats = computed(() => [
  { label: 'Combats',         value: profile.value?.totalCombats ?? 0 },
  { label: 'Victoires',       value: profile.value?.combatsWon ?? 0 },
  { label: 'Défaites',        value: profile.value?.combatsLost ?? 0 },
  { label: 'Dégâts infligés', value: profile.value?.totalDamageDealt?.toLocaleString() ?? 0 },
  { label: 'Dégâts reçus',    value: profile.value?.totalDamageTaken?.toLocaleString() ?? 0 },
  { label: 'Temps de jeu',    value: `${Math.round((profile.value?.totalPlaytimeMinutes ?? 0) / 60)}h` },
])

const shortcuts = [
  { name: 'Saves',     category: 'Progression', title: 'Sauvegardes',  subtitle: 'Consultez vos parties sauvegardées' },
  { name: 'Inventory', category: 'Équipement',  title: 'Inventaire',   subtitle: 'Gérez votre équipement et vos objets' },
  { name: 'Rgpd',      category: 'Données',     title: 'Mes données',  subtitle: 'Gestion de vos données personnelles' },
]

onMounted(async () => {
  try {
    const res = await playerProfileApi.getMe()
    profile.value = res.data
  } catch {
    profile.value = null
  } finally {
    loading.value = false
  }
})
</script>