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
          Console Admin · Catalogue d'objets
        </div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">
          Tous les objets
        </div>
      </div>

      <!-- Onglets Admin -->
      <div
        class="d-flex ga-1 mb-0"
        style="padding-bottom:0;"
      >
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
      display: grid; grid-template-columns: repeat(4, 1fr);
      border-bottom: 1px solid var(--rpg-border);
    "
    >
      <div
        v-for="(stat, i) in stats"
        :key="stat.label"
        style="padding:24px 32px;"
        :style="i < 3 ? 'border-right:1px solid var(--rpg-border);' : ''"
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
          @mouseenter="e => (e.currentTarget as HTMLElement).style.color='var(--rpg-ink)'"
          @mouseleave="e => (e.currentTarget as HTMLElement).style.color='var(--rpg-ink-muted)'"
        >
          {{ filter }}
        </span>
      </div>
      <div class="d-flex ga-2">
        <!-- Recherche -->
        <div style="position:relative;">
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
        </div>
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
        <div style="display:grid;grid-template-columns:80px 2fr 1fr 1fr 80px 1fr 1fr 120px;gap:0;">
          <div class="editorial-label">
            ID
          </div>
          <div class="editorial-label">
            Nom
          </div>
          <div class="editorial-label">
            Type
          </div>
          <div class="editorial-label">
            Catégorie
          </div>
          <div class="editorial-label">
            Prix
          </div>
          <div class="editorial-label">
            Effet
          </div>
          <div class="editorial-label">
            Statut
          </div>
          <div class="editorial-label text-right">
            Actions
          </div>
        </div>
      </div>

      <!-- ── Lignes ────────────────────────────────────────────────── -->
      <div
        v-for="(item, i) in filteredItems"
        :key="item.id"
        class="editorial-row"
        style="padding:14px 0;"
      >
        <div style="display:grid;grid-template-columns:80px 2fr 1fr 1fr 80px 1fr 1fr 120px;gap:0;align-items:center;">
          <div style="font-size:10px;font-weight:600;color:var(--rpg-ink-muted);">
            ITM-{{ String(i + 1).padStart(4, '0') }}
          </div>

          <div class="d-flex align-center ga-2">
            <div
              style="width:7px;height:7px;border-radius:50%;flex-shrink:0;"
              :style="`background:${typeColor(item.type)};`"
            />
            <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;">
              {{ item.name }}
            </div>
          </div>

          <div style="font-size:12px;color:var(--rpg-ink-muted);text-transform:capitalize;">
            {{ item.type }}
          </div>

          <div style="font-size:12px;color:var(--rpg-ink-muted);">
            {{ item.category ?? '—' }}
          </div>

          <div style="font-size:12px;font-weight:600;">
            {{ item.price }} 🪙
          </div>

          <div style="font-size:12px;color:var(--rpg-ink-muted);">
            {{ item.effectValue ? `+${item.effectValue}` : '—' }}
          </div>

          <div class="d-flex align-center ga-1">
            <div style="width:6px;height:6px;border-radius:50%;background:#1E8449;" />
            <div style="font-size:11px;color:#1E8449;">
              Actif
            </div>
          </div>

          <div
            style="text-align:right;"
            class="d-flex justify-end ga-3"
          >
            <span
              style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;"
            >
              Éditer
            </span>
            <span
              style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;color:#C0392B;"
              @click="deleteItem(item.id)"
            >
              Suppr.
            </span>
          </div>
        </div>
      </div>

      <!-- Aucun résultat -->
      <div
        v-if="filteredItems.length === 0"
        class="text-center py-12"
      >
        <div class="editorial-label mb-2">
          Aucun résultat
        </div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);">
          Aucun objet ne correspond à votre recherche.
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { computed, onMounted, ref } from 'vue'
import api from '@/api/auth'

const auth    = useAuthStore()
const loading = ref(true)
const search  = ref('')
const items   = ref<any[]>([])

const adminTabs = [
  { label: 'Utilisateurs',  route: 'AdminUsers',    active: false },
  { label: 'Objets',        route: null,    active: true },
  { label: 'Compétences',   route: 'AdminSkills',   active: false },
  { label: 'Bestiaire',     route: 'AdminBestiary', active: false  },
]

const filters = ['Actifs', 'Récents', 'Archivés']

const stats = computed(() => [
  { label: 'Total catalogue', value: items.value.length },
  { label: 'Armes',           value: items.value.filter(i => i.type === 'weapon').length },
  { label: 'Armures',         value: items.value.filter(i => i.type === 'armor').length },
  { label: 'Consommables',    value: items.value.filter(i => i.type === 'consumable').length },
])

const filteredItems = computed(() => {
  if (!search.value) return items.value
  const q = search.value.toLowerCase()
  return items.value.filter(i =>
    i.name.toLowerCase().includes(q) ||
    i.type.toLowerCase().includes(q) ||
    (i.category ?? '').toLowerCase().includes(q)
  )
})

onMounted(async () => {
  try {
    const res = await api.get('/items')
    items.value = res.data.items ?? []
  } finally {
    loading.value = false
  }
})

async function deleteItem(id: number) {
  await api.delete(`/items/${id}`)
  items.value = items.value.filter(i => i.id !== id)
}

function typeColor(type: string): string {
  const map: Record<string, string> = {
    weapon:     '#C0392B',
    armor:      '#2D4A8A',
    accessory:  '#D68910',
    consumable: '#1E8449',
  }
  return map[type] ?? 'var(--rpg-ink-muted)'
}
</script>