<template>
  <div>

    <!-- ── Header Admin ───────────────────────────────────────────── -->
    <div style="
      margin: -32px -32px 0 -32px;
      padding: 10px 32px;
      background: var(--rpg-ink);
      color: white;
      display: flex; justify-content: space-between; align-items: center;
    ">
      <div style="font-size:10px;font-weight:700;letter-spacing:.15em;text-transform:uppercase;">
        ● RPG_ESI07 Admin · Console privée
      </div>
      <div style="font-size:10px;letter-spacing:.1em;color:rgba(255,255,255,0.5);">
        Session {{ auth.username }} · {{ new Date().toLocaleDateString('fr-FR') }}
      </div>
    </div>

    <!-- ── Titre + onglets ────────────────────────────────────────── -->
    <div style="
      margin: 0 -32px;
      padding: 32px 32px 0;
      border-bottom: 1px solid var(--rpg-border);
      display: flex; justify-content: space-between; align-items: flex-end;
    ">
      <div>
        <div class="editorial-label mb-2">Console Admin · Arbres de talents</div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">
          Toutes les compétences
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
    <div style="
      margin: 0 -32px;
      display: grid; grid-template-columns: repeat(4, 1fr);
      border-bottom: 1px solid var(--rpg-border);
    ">
      <div
        v-for="(stat, i) in stats"
        :key="stat.label"
        style="padding:24px 32px;"
        :style="i < 3 ? 'border-right:1px solid var(--rpg-border);' : ''"
      >
        <div class="editorial-label mb-2">{{ stat.label }}</div>
        <div style="font-family:var(--font-serif);font-size:2rem;font-weight:700;line-height:1;">
          {{ stat.value }}
        </div>
      </div>
    </div>

    <!-- ── Barre d'outils ────────────────────────────────────────── -->
    <div style="
      margin: 0 -32px;
      padding: 14px 32px;
      border-bottom: 1px solid var(--rpg-border);
      display: flex; justify-content: space-between; align-items: center;
    ">
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
        />
        <button style="
          padding:7px 14px;
          border:1px solid var(--rpg-border);
          background:transparent;cursor:pointer;
          font-family:var(--font-sans);font-size:10px;
          font-weight:600;letter-spacing:.1em;text-transform:uppercase;
        ">
          Exporter CSV
        </button>
      </div>
    </div>

    <!-- Chargement -->
    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" size="40" width="2" />
    </div>

    <template v-else>

      <!-- ── Header tableau ────────────────────────────────────────── -->
      <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;margin-top:8px;">
        <div style="display:grid;grid-template-columns:80px 2fr 1fr 1fr 80px 80px 1fr 120px;gap:0;">
          <div class="editorial-label">ID</div>
          <div class="editorial-label">Nom</div>
          <div class="editorial-label">Type</div>
          <div class="editorial-label">Élément</div>
          <div class="editorial-label">MP</div>
          <div class="editorial-label">DMG</div>
          <div class="editorial-label">Statut</div>
          <div class="editorial-label text-right">Actions</div>
        </div>
      </div>

      <!-- ── Lignes ────────────────────────────────────────────────── -->
      <div
        v-for="(skill, i) in filteredSkills"
        :key="skill.id"
        class="editorial-row"
        style="padding:14px 0;"
      >
        <div style="display:grid;grid-template-columns:80px 2fr 1fr 1fr 80px 80px 1fr 120px;gap:0;align-items:center;">

          <div style="font-size:10px;font-weight:600;color:var(--rpg-ink-muted);">
            SKL-{{ String(i + 1).padStart(3, '0') }}
          </div>

          <div class="d-flex align-center ga-2">
            <div
              style="width:7px;height:7px;border-radius:50%;flex-shrink:0;"
              :style="`background:${effectColor(skill.effectType)};`"
            />
            <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;">
              {{ skill.name }}
            </div>
          </div>

          <div style="font-size:12px;color:var(--rpg-ink-muted);text-transform:capitalize;">
            {{ skill.effectType }}
          </div>

          <div style="font-size:12px;color:var(--rpg-ink-muted);text-transform:capitalize;">
            {{ skill.elementType ?? '—' }}
          </div>

          <div style="font-size:12px;font-weight:600;">
            {{ skill.mpCost }}
          </div>

          <div style="font-size:12px;color:var(--rpg-ink-muted);">
            {{ skill.baseDamage ?? '—' }}
          </div>

          <div class="d-flex align-center ga-1">
            <div style="width:6px;height:6px;border-radius:50%;background:#1E8449;" />
            <div style="font-size:11px;color:#1E8449;">Actif</div>
          </div>

          <div style="text-align:right;" class="d-flex justify-end ga-3">
            <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;">
              Éditer
            </span>
            <span
              style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;color:#C0392B;"
            >
              Suppr.
            </span>
          </div>
        </div>
      </div>

      <div v-if="filteredSkills.length === 0" class="text-center py-12">
        <div class="editorial-label mb-2">Aucun résultat</div>
        <div style="font-size:13px;color:var(--rpg-ink-muted);">Aucune compétence ne correspond.</div>
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
const skills  = ref<any[]>([])

const adminTabs = [
  { label: 'Utilisateurs',  route: 'AdminUsers',    active: false },
  { label: 'Objets',        route: 'AdminItems',    active: false },
  { label: 'Compétences',   route: null,   active: true },
  { label: 'Bestiaire',     route: 'AdminBestiary', active: false  },
]

const filters = ['Actifs', 'Récents', 'Archivés']

const stats = computed(() => [
  { label: 'Total',       value: skills.value.length },
  { label: 'Attaque',     value: skills.value.filter(s => s.effectType === 'damage').length },
  { label: 'Soin',        value: skills.value.filter(s => s.effectType === 'heal').length },
  { label: 'Buff/Debuff', value: skills.value.filter(s => ['buff','debuff'].includes(s.effectType)).length },
])

const filteredSkills = computed(() => {
  if (!search.value) return skills.value
  const q = search.value.toLowerCase()
  return skills.value.filter(s =>
    s.name.toLowerCase().includes(q) ||
    s.effectType.toLowerCase().includes(q) ||
    (s.elementType ?? '').toLowerCase().includes(q)
  )
})

onMounted(async () => {
  try {
    const res = await api.get('/skills')
    skills.value = res.data.items ?? []
  } finally {
    loading.value = false
  }
})

function effectColor(type: string): string {
  const map: Record<string, string> = {
    damage: '#C0392B',
    heal:   '#1E8449',
    buff:   '#D68910',
    debuff: '#2D4A8A',
  }
  return map[type] ?? 'var(--rpg-ink-muted)'
}
</script>