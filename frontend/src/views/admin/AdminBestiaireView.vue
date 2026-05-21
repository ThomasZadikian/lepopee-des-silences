<template>
  <div>
    <div style="margin: -32px -32px 0 -32px; padding: 10px 32px; background: var(--rpg-ink); color: white; display: flex; justify-content: space-between; align-items: center;">
      <div style="font-size:10px;font-weight:700;letter-spacing:.15em;text-transform:uppercase;">● RPG_ESI07 Admin · Console privée</div>
      <div style="font-size:10px;letter-spacing:.1em;color:rgba(255,255,255,0.5);">Session {{ auth.username }} · {{ new Date().toLocaleDateString('fr-FR') }}</div>
    </div>

    <div style="margin: 0 -32px; padding: 32px 32px 0; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: flex-end;">
      <div>
        <div class="editorial-label mb-2">Console Admin · Bestiaire global</div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">Tous les monstres</div>
      </div>
      <div class="d-flex ga-1 mb-0">
        <div v-for="tab in adminTabs" :key="tab.label"
          style="padding:8px 16px; border:1px solid var(--rpg-border); font-size:10px;font-weight:600;letter-spacing:.12em;text-transform:uppercase; cursor:pointer;color:var(--rpg-ink-muted);transition:all .15s;"
          :style="tab.active ? 'background:var(--rpg-ink);color:white;border-color:var(--rpg-ink);' : ''"
          @click="tab.route && $router.push({ name: tab.route })">
          {{ tab.label }}
        </div>
      </div>
    </div>

    <div style="margin: 0 -32px; display: grid; grid-template-columns: repeat(5, 1fr); border-bottom: 1px solid var(--rpg-border);">
      <div v-for="(stat, i) in stats" :key="stat.label" style="padding:24px 32px;" :style="i < 4 ? 'border-right:1px solid var(--rpg-border);' : ''">
        <div class="editorial-label mb-2">{{ stat.label }}</div>
        <div style="font-family:var(--font-serif);font-size:2rem;font-weight:700;line-height:1;">{{ stat.value }}</div>
      </div>
    </div>

    <div style="margin: 0 -32px; padding: 14px 32px; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: center;">
      <input v-model="search" type="text" placeholder="Rechercher..."
        style="padding:7px 12px; border:1px solid var(--rpg-border); background:transparent; font-family:var(--font-sans);font-size:11px; outline:none;width:200px;"/>
      <button style="padding:7px 14px; border:1px solid var(--rpg-ink); background:var(--rpg-ink);color:white;cursor:pointer; font-family:var(--font-sans);font-size:10px;font-weight:600;letter-spacing:.1em;text-transform:uppercase;" @click="openCreate">
        + Nouveau monstre
      </button>
    </div>

    <div v-if="loading" class="d-flex justify-center pa-12">
      <v-progress-circular indeterminate color="primary" size="40" width="2"/>
    </div>

    <template v-else>
        <div style="position: relative;" :style="panel ? 'display:grid;grid-template-columns:1fr 380px;gap:0;align-items:start;' : ''">        <div>
          <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;margin-top:8px;">
            <div style="display:grid;grid-template-columns:80px 2fr 100px 80px 80px 80px 80px 120px;gap:0;">
              <div class="editorial-label">ID</div>
              <div class="editorial-label">Créature</div>
              <div class="editorial-label">Catégorie</div>
              <div class="editorial-label">HP max</div>
              <div class="editorial-label">Force</div>
              <div class="editorial-label">XP</div>
              <div class="editorial-label">Or</div>
              <div class="editorial-label text-right">Actions</div>
            </div>
          </div>

          <div v-for="(enemy, i) in filteredEnemies" :key="enemy.id" class="editorial-row" style="padding:14px 0; cursor:pointer;"
            :style="panel?.id === enemy.id && mode === 'view' ? 'background:rgba(0,0,0,0.02);' : ''"
            @click.self="(e) => openView(enemy, e)">
            <div style="display:grid;grid-template-columns:80px 2fr 100px 80px 80px 80px 80px 120px;gap:0;align-items:center; pointer-events:none;">
              <div style="font-size:10px;font-weight:600;color:var(--rpg-ink-muted);">MON-{{ String(i+1).padStart(2,'0') }}</div>
              <div>
                <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;margin-bottom:2px;">{{ enemy.name }}</div>
                <div style="font-size:11px;color:var(--rpg-ink-muted);font-style:italic;">{{ enemy.description?.substring(0,40) }}{{ (enemy.description?.length ?? 0) > 40 ? '…' : '' }}</div>
              </div>
              <div><span style="font-size:10px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;padding:2px 6px;" :style="typeStyle(enemy.type)">{{ enemy.type }}</span></div>
              <div style="font-size:12px;font-weight:600;">{{ enemy.maxHP }}</div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ enemy.strength }}</div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ enemy.experienceReward }}</div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ enemy.goldReward }}</div>
              <div style="text-align:right; pointer-events:auto;" class="d-flex justify-end ga-3" @click.stop>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;" @click="(e) => openEdit(enemy, e)">Éditer</span>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;color:#C0392B;" @click="(e) => confirmDelete(enemy, e)">Suppr.</span>
              </div>
            </div>
          </div>

          <div v-if="filteredEnemies.length === 0" class="text-center py-12">
            <div class="editorial-label mb-2">Aucun résultat</div>
          </div>
        </div>

        <div v-if="panel" ref="panelRef" :style="{ marginTop: panelMarginTop + 'px' }" style="border-left:1px solid var(--rpg-border);padding:32px 24px;max-height:calc(100vh - 80px);overflow-y:auto;">
          <template v-if="mode === 'view'">
            <div class="editorial-label mb-3">{{ panel.type.toUpperCase() }} · Détail</div>
            <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:900;margin-bottom:8px;">{{ panel.name }}</div>
            <div v-if="panel.description" style="font-size:13px;color:var(--rpg-ink-muted);font-style:italic;margin-bottom:20px;line-height:1.6;">« {{ panel.description }} »</div>
            <div style="border-top:1px solid var(--rpg-border);padding-top:16px;margin-bottom:16px;">
              <div v-for="stat in detailStats" :key="stat.label" style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid var(--rpg-border);">
                <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ stat.label }}</div>
                <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">{{ stat.value }}</div>
              </div>
            </div>
            <div style="display:flex;gap:8px;margin-top:16px;">
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-ink);background:var(--rpg-ink);color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="openEdit(panel)">Éditer</button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Fermer</button>
            </div>
          </template>

          <template v-else-if="mode === 'edit' || mode === 'create'">
            <div class="editorial-label mb-4">{{ mode === 'create' ? '■ Nouveau monstre' : '■ Modifier · ' + panel.name }}</div>
            <div v-for="field in formFields" :key="field.key" style="margin-bottom:16px;">
              <div class="editorial-label mb-1">{{ field.label }}</div>
              <select v-if="field.type === 'select'" v-model="(form as any)[field.key]"
                style="width:100%;padding:8px 10px;border:1px solid rgba(0,0,0,0.15);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;">
                <option v-for="opt in field.options" :key="opt" :value="opt">{{ opt }}</option>
              </select>
              <textarea v-else-if="field.type === 'textarea'" v-model="(form as any)[field.key]"
                style="width:100%;padding:8px 10px;border:1px solid rgba(0,0,0,0.15);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;resize:vertical;min-height:80px;"/>
              <input v-else :type="field.type" v-model="(form as any)[field.key]"
                style="width:100%;padding:8px 10px;border:1px solid rgba(0,0,0,0.15);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;"/>
            </div>
            <div v-if="formError" style="font-size:12px;color:#C0392B;border-left:2px solid #C0392B;padding:8px 12px;margin-bottom:12px;background:rgba(192,57,43,0.05);">{{ formError }}</div>
            <div style="display:flex;gap:8px;margin-top:8px;">
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-ink);background:var(--rpg-ink);color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" :disabled="saving" @click="save">
                {{ saving ? 'Enregistrement...' : mode === 'create' ? 'Créer' : 'Enregistrer' }}
              </button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Annuler</button>
            </div>
          </template>

          <template v-else-if="mode === 'delete'">
            <div class="editorial-label mb-4">■ Supprimer</div>
            <div style="font-family:var(--font-serif);font-size:1.2rem;font-weight:700;margin-bottom:12px;">{{ panel.name }}</div>
            <div style="font-size:13px;color:var(--rpg-ink-muted);margin-bottom:24px;line-height:1.6;">Cette action est irréversible. Le monstre sera supprimé du bestiaire global.</div>
            <div style="display:flex;gap:8px;">
              <button style="flex:1;padding:10px;border:1px solid #C0392B;background:#C0392B;color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" :disabled="saving" @click="executeDelete">
                {{ saving ? 'Suppression...' : 'Confirmer' }}
              </button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Annuler</button>
            </div>
          </template>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { enemiesApi } from '@/api/enemies'
import type { Enemy } from '@/interfaces/bestiary'
import { useAuthStore } from '@/stores/auth'
import { computed, onMounted, ref } from 'vue'
import api from '@/api/auth'
import * as C from '@/constants'

const auth      = useAuthStore()
const loading   = ref(true)
const search    = ref('')
const enemies   = ref<Enemy[]>([])
const panel     = ref<Enemy | null>(null)
const panelMarginTop = ref(0)
const mode      = ref<'view'|'edit'|'create'|'delete'>('view')
const saving    = ref(false)
const formError = ref('')

const form = ref({
    name: '', type: C.ENEMY_TYPE_BASIC, maxHP: 100, strength: 10,
    intelligence: 5, speed: 10, physicalResistance: 1.0,
    magicalResistance: 1.0, experienceReward: 10,
    goldReward: 5, description: '',
    initialState: C.ENEMY_STATE_REPOS, influenceRadius: 5
})

const adminTabs = [
    { label: 'Utilisateurs', route: 'AdminUsers',    active: false },
    { label: 'Objets',       route: 'AdminItems',    active: false },
    { label: 'Compétences',  route: 'AdminSkills',   active: false },
    { label: 'Bestiaire',    route: 'AdminBestiary', active: true  },
]

const formFields = [
    { key: 'name',               label: 'Nom',                 type: 'text'     },
    { key: 'type',               label: 'Type',                type: 'select',  options: [C.ENEMY_TYPE_BASIC, C.ENEMY_TYPE_MINIBOSS, C.ENEMY_TYPE_BOSS] },
    { key: 'initialState',       label: 'État initial',        type: 'select',  options: [C.ENEMY_STATE_REPOS, C.ENEMY_STATE_PATROUILLE, C.ENEMY_STATE_CHASSE, C.ENEMY_STATE_FUITE] },
    { key: 'description',        label: 'Description',         type: 'textarea' },
    { key: 'maxHP',              label: 'HP max',              type: 'number'   },
    { key: 'strength',           label: 'Force',               type: 'number'   },
    { key: 'intelligence',       label: 'Intelligence',        type: 'number'   },
    { key: 'speed',              label: 'Vitesse',             type: 'number'   },
    { key: 'physicalResistance', label: 'Résistance physique', type: 'number'   },
    { key: 'magicalResistance',  label: 'Résistance magique',  type: 'number'   },
    { key: 'experienceReward',   label: 'Récompense XP',       type: 'number'   },
    { key: 'goldReward',         label: 'Récompense Or',       type: 'number'   },
    { key: 'influenceRadius',    label: "Rayon d'influence",   type: 'number'   },
]

const stats = computed(() => [
    { label: 'Total',    value: enemies.value.length },
    { label: 'Boss',     value: enemies.value.filter(e => e.type === C.ENEMY_TYPE_BOSS).length },
    { label: 'Miniboss', value: enemies.value.filter(e => e.type === C.ENEMY_TYPE_MINIBOSS).length },
    { label: 'Communs',  value: enemies.value.filter(e => e.type === C.ENEMY_TYPE_BASIC).length },
    { label: 'XP total', value: enemies.value.reduce((s, e) => s + e.experienceReward, 0).toLocaleString() },
])

const filteredEnemies = computed(() => {
    if (!search.value) return enemies.value
    const q = search.value.toLowerCase()
    return enemies.value.filter(e => e.name.toLowerCase().includes(q) || e.type.toLowerCase().includes(q))
})

const detailStats = computed(() => {
    if (!panel.value) return []
    return [
        { label: 'HP max',              value: panel.value.maxHP },
        { label: 'Force',               value: panel.value.strength },
        { label: 'Intelligence',        value: panel.value.intelligence },
        { label: 'Vitesse',             value: panel.value.speed },
        { label: 'Résistance physique', value: `${(panel.value.physicalResistance * 100).toFixed(0)}%` },
        { label: 'Résistance magique',  value: `${(panel.value.magicalResistance * 100).toFixed(0)}%` },
        { label: 'Récompense XP',       value: panel.value.experienceReward },
        { label: 'Récompense Or',       value: `${panel.value.goldReward} 🪙` },
    ]
})

onMounted(async () => {
    try {
        const res = await enemiesApi.getAll()
        enemies.value = res.data.items ?? []
    } finally {
        loading.value = false
    }
})

function alignPanel(event?: MouseEvent) {
    if (!event) return
    const el = event.currentTarget as HTMLElement
    panelMarginTop.value = el.offsetTop
}

function openView(enemy: Enemy, event?: MouseEvent) {
    panel.value = enemy
    mode.value  = 'view'
    alignPanel(event)
}

function openEdit(enemy: Enemy, event?: MouseEvent) {
    panel.value = enemy
    mode.value  = 'edit'
    Object.assign(form.value, { ...enemy })
    alignPanel(event)
}

function confirmDelete(enemy: Enemy, event?: MouseEvent) {
    panel.value = enemy
    mode.value  = 'delete'
    alignPanel(event)
}

function openCreate() {
    panel.value = {} as Enemy
    mode.value  = 'create'
    form.value  = {
        name: '', type: C.ENEMY_TYPE_BASIC, maxHP: 100, strength: 10,
        intelligence: 5, speed: 10, physicalResistance: 1.0,
        magicalResistance: 1.0, experienceReward: 10,
        goldReward: 5, description: '',
        initialState: C.ENEMY_STATE_REPOS, influenceRadius: 5
    }
    formError.value = ''
    panelMarginTop.value = 0
    window.scrollTo({ top: 0, behavior: 'smooth' })
}

function closePanel() {
    panel.value = null
    formError.value = ''
    panelMarginTop.value = 0
}

async function save() {
    if (!form.value.name.trim()) { formError.value = 'Le nom est requis.'; return }
    saving.value    = true
    formError.value = ''
    try {
        if (mode.value === 'create') {
            const res = await api.post('/enemies', form.value)
            enemies.value.push(res.data)
        } else {
            await api.put(`/enemies/${panel.value!.id}`, form.value)
            const idx = enemies.value.findIndex(e => e.id === panel.value!.id)
            if (idx !== -1) enemies.value[idx] = { ...enemies.value[idx], ...form.value }
        }
        closePanel()
    } catch {
        formError.value = 'Une erreur est survenue.'
    } finally {
        saving.value = false
    }
}

async function executeDelete() {
    saving.value = true
    try {
        await api.delete(`/enemies/${panel.value!.id}`)
        enemies.value = enemies.value.filter(e => e.id !== panel.value!.id)
        closePanel()
    } catch {
        formError.value = 'Erreur lors de la suppression.'
    } finally {
        saving.value = false
    }
}

function typeStyle(type: string): string {
    const map: Record<string, string> = {
        [C.ENEMY_TYPE_BASIC]:    'border:1px solid var(--rpg-border);color:var(--rpg-ink-muted);',
        [C.ENEMY_TYPE_MINIBOSS]: 'border:1px solid #D68910;color:#D68910;',
        [C.ENEMY_TYPE_BOSS]:     'background:var(--rpg-ink);color:white;',
    }
    return map[type] ?? ''
}
</script>