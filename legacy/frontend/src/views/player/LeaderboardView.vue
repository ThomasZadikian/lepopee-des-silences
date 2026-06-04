<template>
  <div>
    <!-- ── Header éditorial ───────────────────────────────────────── -->
    <div style="margin: -32px -32px 40px -32px; border-bottom: 1px solid var(--rpg-border);">
      <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; min-height: 140px;">
        <div style="padding: 40px 32px; border-right: 1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Classement mondial</div>
          <div style="font-family: var(--font-serif); font-size: clamp(2rem, 3vw, 3rem); font-weight: 900; line-height: 1; letter-spacing: -0.03em;">
            Codex <span style="font-style: italic; color: var(--rpg-ink-muted);">· Champions</span>
          </div>
        </div>
        <div style="padding: 40px 32px; border-right: 1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Joueurs classés</div>
          <div style="font-family: var(--font-serif); font-size: 2.5rem; font-weight: 700; line-height: 1;">
            {{ totalCount }}
          </div>
        </div>
        <div style="padding: 40px 32px;">
          <div class="editorial-label mb-3">Trier par</div>
          <div style="display: flex; flex-direction: column; gap: 8px;">
            <div
              v-for="sort in sortOptions"
              :key="sort.key"
              style="font-size: 11px; font-weight: 600; letter-spacing: 0.1em; text-transform: uppercase; cursor: pointer; display: flex; align-items: center; gap: 8px;"
              :style="{ color: currentSort === sort.key ? 'var(--rpg-ink)' : 'var(--rpg-ink-muted)' }"
              @click="changeSort(sort.key)"
            >
              <span :style="{ opacity: currentSort === sort.key ? 1 : 0 }">→</span>
              {{ sort.label }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── Recherche ──────────────────────────────────────────────── -->
<div style="margin-bottom: 32px;">
  <div class="editorial-label mb-2">■ Rechercher un joueur</div>
  <div style="display: flex; gap: 12px; align-items: center;">
    <input
      v-model="searchTerm"
      type="text"
      placeholder="Nom du personnage..."
      maxlength="50"
      style="flex: 1; max-width: 400px; padding: 12px 14px; border: 1px solid rgba(0,0,0,0.15); background: transparent; font-family: var(--font-sans); font-size: 14px; outline: none; transition: border-color 0.15s;"
      @input="onSearchInput"
      @focus="e => (e.target as HTMLInputElement).style.borderColor = 'var(--rpg-ink)'"
      @blur="e => (e.target as HTMLInputElement).style.borderColor = 'rgba(0,0,0,0.15)'"
    />
    <button
      v-if="searchTerm.length > 0"
      style="padding: 12px 20px; border: 1px solid var(--rpg-border); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase;"
      @click="clearSearch"
    >
      Effacer
    </button>
  </div>

  <!-- Erreur -->
  <div
    v-if="searchError"
    style="font-size: 12px; color: #C0392B; border-left: 2px solid #C0392B; padding: 8px 12px; margin-top: 12px; background: rgba(192,57,43,0.05); max-width: 400px;"
  >
    {{ searchError }}
  </div>

  <!-- Résultats de recherche -->
  <div v-if="searchTerm.trim().length >= 2" style="margin-top: 16px; max-width: 600px;">
    <div v-if="searching" style="font-size: 12px; color: var(--rpg-ink-muted);">
      Recherche en cours...
    </div>

    <template v-else>
      <div
        v-if="searchResults.length === 0"
        style="font-size: 13px; color: var(--rpg-ink-muted); padding: 16px 0;"
      >
        Aucun personnage trouvé pour "{{ searchTerm }}"
      </div>

      <div v-else style="border-top: 1px solid var(--rpg-border);">
        <div class="editorial-label mb-2" style="padding-top: 12px;">
          {{ searchResults.length }} résultat{{ searchResults.length > 1 ? 's' : '' }}
        </div>
        <div
          v-for="entry in searchResults"
          :key="entry.rank"
          style="display: grid; grid-template-columns: 60px 1fr 80px; padding: 12px 0; border-bottom: 1px solid var(--rpg-border);"
        >
          <div style="font-size: 12px; color: var(--rpg-ink-muted); display: flex; align-items: center;">
            # {{ entry.rank }}
          </div>
          <div style="font-family: var(--font-serif); font-size: 1rem; font-weight: 700; display: flex; align-items: center;">
            {{ entry.characterName }}
          </div>
          <div style="text-align: right; display: flex; align-items: center; justify-content: flex-end;">
            <span style="font-family: var(--font-serif); font-size: 1rem; font-weight: 700;">
              Niv. {{ entry.level }}
            </span>
          </div>
        </div>
      </div>
    </template>
  </div>
</div>

    <!-- ── Chargement ─────────────────────────────────────────────── -->
    <div v-if="loading" class="d-flex justify-center align-center" style="min-height: 40vh;">
      <v-progress-circular indeterminate color="primary" size="40" width="2"/>
    </div>

    <!-- ── Tableau ────────────────────────────────────────────────── -->
    <template v-else>
      <!-- En-tête tableau -->
      <div style="border-top: 1px solid var(--rpg-border); border-bottom: 1px solid var(--rpg-border); margin-bottom: 0;">
        <div style="display: grid; grid-template-columns: 60px 1fr 80px 100px 140px 120px; padding: 12px 0;">
          <div class="editorial-label" style="text-align: center;">#</div>
          <div class="editorial-label">Personnage</div>
          <div class="editorial-label" style="text-align: right;">Niveau</div>
          <div class="editorial-label" style="text-align: right;">Victoires</div>
          <div class="editorial-label" style="text-align: right;">Dégâts infligés</div>
          <div class="editorial-label" style="text-align: right;">Temps de jeu</div>
        </div>
      </div>

      <!-- Lignes -->
      <div
        v-for="entry in entries"
        :key="entry.rank"
        style="display: grid; grid-template-columns: 60px 1fr 80px 100px 140px 120px; padding: 16px 0; border-bottom: 1px solid var(--rpg-border); transition: background 0.1s;"
        @mouseenter="e => (e.currentTarget as HTMLElement).style.background = 'rgba(0,0,0,0.02)'"
        @mouseleave="e => (e.currentTarget as HTMLElement).style.background = 'transparent'"
      >
        <!-- Rang -->
        <div style="text-align: center; display: flex; align-items: center; justify-content: center;">
          <span
            v-if="entry.rank <= 3"
            style="font-family: var(--font-serif); font-size: 1.2rem; font-weight: 900;"
            :style="{ color: entry.rank === 1 ? '#B8860B' : entry.rank === 2 ? '#808080' : '#8B4513' }"
          >
            {{ entry.rank === 1 ? '◆' : entry.rank === 2 ? '◇' : '○' }}
          </span>
          <span v-else style="font-size: 12px; color: var(--rpg-ink-muted); font-weight: 600;">
            {{ entry.rank }}
          </span>
        </div>

        <!-- Nom -->
        <div style="display: flex; align-items: center;">
          <span style="font-family: var(--font-serif); font-size: 1rem; font-weight: 700;">
            {{ entry.characterName }}
          </span>
        </div>

        <!-- Niveau -->
        <div style="text-align: right; display: flex; align-items: center; justify-content: flex-end;">
          <span style="font-family: var(--font-serif); font-size: 1.1rem; font-weight: 700;">
            {{ entry.level }}
          </span>
        </div>

        <!-- Victoires -->
        <div style="text-align: right; display: flex; align-items: center; justify-content: flex-end;">
          <span style="font-size: 13px; font-weight: 600;">{{ entry.combatsWon }}</span>
        </div>

        <!-- Dégâts -->
        <div style="text-align: right; display: flex; align-items: center; justify-content: flex-end;">
          <span style="font-size: 13px; color: var(--rpg-ink-muted);">{{ entry.totalDamageDealt.toLocaleString() }}</span>
        </div>

        <!-- Temps de jeu -->
        <div style="text-align: right; display: flex; align-items: center; justify-content: flex-end;">
          <span style="font-size: 13px; color: var(--rpg-ink-muted);">{{ formatPlaytime(entry.totalPlaytimeMinutes) }}</span>
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" style="display: flex; justify-content: space-between; align-items: center; padding: 24px 0; border-top: 1px solid var(--rpg-border);">
        <button
          style="padding: 10px 24px; border: 1px solid var(--rpg-border); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase;"
          :disabled="currentPage === 1"
          @click="changePage(currentPage - 1)"
        >
          ← Précédent
        </button>

        <div class="editorial-label">
          Page {{ currentPage }} / {{ totalPages }}
        </div>

        <button
          style="padding: 10px 24px; border: 1px solid var(--rpg-border); background: transparent; cursor: pointer; font-family: var(--font-sans); font-size: 11px; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase;"
          :disabled="currentPage === totalPages"
          @click="changePage(currentPage + 1)"
        >
          Suivant →
        </button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import api from '@/api/auth'
import { onMounted, onUnmounted, ref } from 'vue'

interface LeaderboardEntry {
    rank:                 number
    characterName:        string
    level:                number
    combatsWon:           number
    totalDamageDealt:     number
    totalPlaytimeMinutes: number
}

interface LeaderboardResponse {
    items:      LeaderboardEntry[]
    totalCount: number
    page:       number
    pageSize:   number
    totalPages: number
}
interface SearchEntry {
    rank:          number
    characterName: string
    level:         number
}

const searchTerm    = ref('')
const searchResults = ref<SearchEntry[]>([])
const searching     = ref(false)
const searchError   = ref('')
let   debounceTimer: ReturnType<typeof setTimeout> | null = null

function onSearchInput() {
    searchError.value = ''
    if (debounceTimer) clearTimeout(debounceTimer)

    const term = searchTerm.value.trim()

    if (term.length === 0) {
        searchResults.value = []
        return
    }

    if (term.length < 2) return

    // Validation caractères côté client — double protection
    if (!/^[\p{L}\p{N}\s_\-]+$/u.test(term)) {
        searchError.value = 'Caractères non autorisés.'
        return
    }

    debounceTimer = setTimeout(async () => {
        searching.value = true
        try {
            const res = await api.get('/leaderboard/search', { params: { q: term } })
            searchResults.value = res.data.items
        } catch {
            searchResults.value = []
            searchError.value   = 'Erreur lors de la recherche.'
        } finally {
            searching.value = false
        }
    }, 300)
}

function clearSearch() {
    searchTerm.value    = ''
    searchResults.value = []
    searchError.value   = ''
}

const loading     = ref(true)
const entries     = ref<LeaderboardEntry[]>([])
const totalCount  = ref(0)
const totalPages  = ref(1)
const currentPage = ref(1)
const currentSort = ref('level')

const sortOptions = [
    { key: 'level',                label: 'Niveau'         },
    { key: 'combatswon',           label: 'Victoires'      },
    { key: 'totaldamagedealt',     label: 'Dégâts infligés'},
    { key: 'totalplaytimeminutes', label: 'Temps de jeu'   },
    { key: 'charactername',        label: 'Nom'            },
]

function formatPlaytime(minutes: number): string {
    const h = Math.floor(minutes / 60)
    const m = minutes % 60
    return h > 0 ? `${h}h${m.toString().padStart(2, '0')}` : `${m}min`
}

async function fetchLeaderboard() {
    loading.value = true
    try {
        const res = await api.get<LeaderboardResponse>('/leaderboard', {
            params: { sortBy: currentSort.value, page: currentPage.value }
        })
        entries.value    = res.data.items
        totalCount.value = res.data.totalCount
        totalPages.value = res.data.totalPages
    } catch {
        entries.value = []
    } finally {
        loading.value = false
    }
}

async function changeSort(sort: string) {
    currentSort.value = sort
    currentPage.value = 1
    await fetchLeaderboard()
}

async function changePage(page: number) {
    currentPage.value = page
    await fetchLeaderboard()
}

onMounted(fetchLeaderboard)
onUnmounted(() => {
  if (debounceTimer) clearTimeout(debounceTimer)
})
</script>