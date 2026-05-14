<template>
  <div>
    <!-- ── Header Admin ───────────────────────────────────────────── -->
    <div
      style="
      margin: -32px -32px 0 -32px;
      padding: 10px 32px;
      background: var(--rpg-ink);
      color: white;
      display: flex; justify-content: space-between; align-items: center;
    "
    >
      <div style="font-size:10px;font-weight:700;letter-spacing:.15em;text-transform:uppercase;">
        ● RPG_ESI07 Admin · Console privée
      </div>
      <div style="font-size:10px;letter-spacing:.1em;color:rgba(255,255,255,0.5);">
        Session {{ auth.username }} · {{ new Date().toLocaleDateString('fr-FR') }}
      </div>
    </div>

    <!-- ── Titre + onglets ────────────────────────────────────────── -->
    <div
      style="
      margin: 0 -32px;
      padding: 32px 32px 0;
      border-bottom: 1px solid var(--rpg-border);
      display: flex; justify-content: space-between; align-items: flex-end;
    "
    >
      <div>
        <div class="editorial-label mb-2">
          Console Admin · Bestiaire global
        </div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">
          Tous les monstres
        </div>
      </div>

      <div class="d-flex ga-1 mb-0">
        <div
          v-for="tab in adminTabs"
          :key="tab.label"
          style="
            padding:8px 16px;
            border:1px solid var(--rpg-border);
            font-size:10px;font-weight:600;letter-spacing:.12em;text-transform:uppercase;
            cursor:pointer;color:var(--rpg-ink-muted);transition:all .15s;
          "
          :style="tab.active ? 'background:var(--rpg-ink);color:white;border-color:var(--rpg-ink);' : ''"
          @click="tab.route && $router.push({ name: tab.route })"
        >
          {{ tab.label }}
        </div>
      </div>
    </div>

    <!-- ── Compteurs ──────────────────────────────────────────────── -->
    <div
      style="
      margin: 0 -32px;
      display: grid; grid-template-columns: repeat(5, 1fr);
      border-bottom: 1px solid var(--rpg-border);
    "
    >
      <div
        v-for="(stat, i) in stats"
        :key="stat.label"
        style="padding:24px 32px;"
        :style="i < 4 ? 'border-right:1px solid var(--rpg-border);' : ''"
      >
        <div class="editorial-label mb-2">
          {{ stat.label }}
        </div>
        <div style="font-family:var(--font-serif);font-size:2rem;font-weight:700;line-height:1;">
          {{ stat.value }}
        </div>
      </div>
    </div>

    <!-- ── Barre d'outils ────────────────────────────────────────── -->
    <div
      style="
      margin: 0 -32px;
      padding: 14px 32px;
      border-bottom: 1px solid var(--rpg-border);
      display: flex; justify-content: space-between; align-items: center;
    "
    >
      <div class="d-flex ga-4">
        <span style="font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;cursor:pointer;">
          ● Tous
        </span>
        <span
          v-for="filter in filters"
          :key="filter"
          style="font-size:10px;font-weight:500;letter-spacing:.1em;text-transform:uppercase;color:var(--rpg-ink-muted);cursor:pointer;"
        >
          {{ filter }}
        </span>
      </div>
      <div class="d-flex ga-2">
        <input
          v-model="search"
          type="text"
          placeholder="Rechercher..."
          style="
            padding:7px 12px;
            border:1px solid var(--rpg-border);
            background:transparent;
            font-family:var(--font-sans);font-size:11px;
            outline:none;width:180px;
          "
        >
        <button
          style="
          padding:7px 14px;
          border:1px solid var(--rpg-border);
          background:transparent;cursor:pointer;
          font-family:var(--font-sans);font-size:10px;
          font-weight:600;letter-spacing:.1em;text-transform:uppercase;
        "
        >
          Exporter CSV
        </button>
      </div>
    </div>

    <!-- Chargement -->
    <div
      v-if="loading"
      class="d-flex justify-center pa-12"
    >
      <v-progress-circular
        indeterminate
        color="primary"
        size="40"
        width="2"
      />
    </div>

    <template v-else>
      <!-- ── Header tableau ────────────────────────────────────────── -->
      <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;margin-top:8px;">
        <div style="display:grid;grid-template-columns:80px 2fr 100px 80px 80px 80px 80px 80px 120px;gap:0;">
          <div class="editorial-label">
            ID
          </div>
          <div class="editorial-label">
            Créature
          </div>
          <div class="editorial-label">
            Catégorie
          </div>
          <div class="editorial-label">
            HP max
          </div>
          <div class="editorial-label">
            Force
          </div>
          <div class="editorial-label">
            XP
          </div>
          <div class="editorial-label">
            Or
          </div>
          <div class="editorial-label">
            Menace
          </div>
          <div class="editorial-label text-right">
            Actions
          </div>
        </div>
      </div>

      <!-- ── Lignes ────────────────────────────────────────────────── -->
      <div
        v-for="(enemy, i) in filteredEnemies"
        :key="enemy.id"
        class="editorial-row"
        style="padding:14px 0;"
      >
        <div style="display:grid;grid-template-columns:80px 2fr 100px 80px 80px 80px 80px 80px 120px;gap:0;align-items:center;">
          <div style="font-size:10px;font-weight:600;color:var(--rpg-ink-muted);">
            MON-{{ String(i + 1).padStart(2, '0') }}
          </div>

          <div>
            <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;margin-bottom:2px;">
              {{ enemy.name }}
            </div>
            <div style="font-size:11px;color:var(--rpg-ink-muted);font-style:italic;">
              {{ enemy.description?.substring(0, 40) }}{{ (enemy.description?.length ?? 0) > 40 ? '…' : '' }}
            </div>
          </div>

          <div>
            <span
              style="
                font-size:10px;font-weight:700;letter-spacing:.06em;
                text-transform:uppercase;padding:2px 6px;
              "
              :style="typeStyle(enemy.type)"
            >
              {{ enemy.type }}
            </span>
          </div>

          <div style="font-size:12px;font-weight:600;">
            {{ enemy.maxHP }}
          </div>
          <div style="font-size:12px;color:var(--rpg-ink-muted);">
            {{ enemy.strength }}
          </div>
          <div style="font-size:12px;color:var(--rpg-ink-muted);">
            {{ enemy.experienceReward }}
          </div>
          <div style="font-size:12px;color:var(--rpg-ink-muted);">
            {{ enemy.goldReward }}
          </div>

          <div>
            <!-- Barre de menace -->
            <div style="height:2px;background:rgba(0,0,0,0.08);width:60px;">
              <div :style="`width:${Math.min((enemy.maxHP / 600) * 100, 100)}%;height:100%;background:${typeColor(enemy.type)};`" />
            </div>
          </div>

          <div
            style="text-align:right;"
            class="d-flex justify-end ga-3"
          >
            <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;">
              Éditer
            </span>
            <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;">
              Stats
            </span>
          </div>
        </div>
      </div>

      <div
        v-if="filteredEnemies.length === 0"
        class="text-center py-12"
      >
        <div class="editorial-label mb-2">
          Aucun résultat
        </div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);">
          Aucun monstre ne correspond.
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { computed, onMounted, ref } from 'vue'
import { enemiesApi } from '@/api/enemies'

const auth    = useAuthStore()
const loading = ref(true)
const search  = ref('')
const enemies = ref<any[]>([])

const adminTabs = [
  { label: 'Utilisateurs',  route: 'AdminUsers',    active: false },
  { label: 'Objets',        route: 'AdminItems',    active: false },
  { label: 'Compétences',   route: 'AdminSkills',   active: false },
  { label: 'Bestiaire',     route: 'AdminBestiary', active: true  },
]

const filters = ['Tous', 'Boss', 'Élites', 'Communs']

const stats = computed(() => [
  { label: 'Total',    value: enemies.value.length },
  { label: 'Boss',     value: enemies.value.filter(e => e.type === 'boss').length },
  { label: 'Miniboss', value: enemies.value.filter(e => e.type === 'miniboss').length },
  { label: 'Communs',  value: enemies.value.filter(e => e.type === 'basic').length },
  { label: 'XP total', value: enemies.value.reduce((s, e) => s + e.experienceReward, 0).toLocaleString() },
])

const filteredEnemies = computed(() => {
  if (!search.value) return enemies.value
  const q = search.value.toLowerCase()
  return enemies.value.filter(e =>
    e.name.toLowerCase().includes(q) ||
    e.type.toLowerCase().includes(q)
  )
})

onMounted(async () => {
  try {
const res = await enemiesApi.getAll()
enemies.value = res.data.items ?? []
  } finally {
    loading.value = false
  }
})

function typeColor(type: string): string {
  const map: Record<string, string> = {
    basic:    'var(--rpg-ink-muted)',
    miniboss: '#D68910',
    boss:     '#C0392B',
  }
  return map[type] ?? 'var(--rpg-ink-muted)'
}

function typeStyle(type: string): string {
  const map: Record<string, string> = {
    basic:    'border:1px solid var(--rpg-border);color:var(--rpg-ink-muted);',
    miniboss: 'border:1px solid #D68910;color:#D68910;',
    boss:     'background:var(--rpg-ink);color:white;',
  }
  return map[type] ?? ''
}

console.log(enemies)
</script>
