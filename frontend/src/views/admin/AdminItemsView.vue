<template>
  <div>
    <div style="margin: -32px -32px 0 -32px; padding: 10px 32px; background: var(--rpg-ink); color: white; display: flex; justify-content: space-between; align-items: center;">
      <div style="font-size:10px;font-weight:700;letter-spacing:.15em;text-transform:uppercase;">● RPG_ESI07 Admin · Console privée</div>
      <div style="font-size:10px;letter-spacing:.1em;color:rgba(255,255,255,0.5);">Session {{ auth.username }} · {{ new Date().toLocaleDateString('fr-FR') }}</div>
    </div>

    <div style="margin: 0 -32px; padding: 32px 32px 0; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: flex-end;">
      <div>
        <div class="editorial-label mb-2">Console Admin · Catalogue d'objets</div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">Tous les objets</div>
      </div>
      <div class="d-flex ga-1 mb-0">
        <div v-for="tab in adminTabs" :key="tab.label"
          style="padding:8px 16px; border:1px solid var(--rpg-border); font-size:10px;font-weight:600;letter-spacing:.12em;text-transform:uppercase; cursor:pointer;color:var(--rpg-ink-muted);"
          :style="tab.active ? 'background:var(--rpg-ink);color:white;border-color:var(--rpg-ink);' : ''"
          @click="tab.route && $router.push({ name: tab.route })">{{ tab.label }}</div>
      </div>
    </div>

    <div style="margin: 0 -32px; display: grid; grid-template-columns: repeat(4, 1fr); border-bottom: 1px solid var(--rpg-border);">
      <div v-for="(stat, i) in stats" :key="stat.label" style="padding:24px 32px;" :style="i < 3 ? 'border-right:1px solid var(--rpg-border);' : ''">
        <div class="editorial-label mb-2">{{ stat.label }}</div>
        <div style="font-family:var(--font-serif);font-size:2rem;font-weight:700;line-height:1;">{{ stat.value }}</div>
      </div>
    </div>

    <div style="margin: 0 -32px; padding: 14px 32px; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: center;">
      <input v-model="search" type="text" placeholder="Rechercher..." style="padding:7px 12px; border:1px solid var(--rpg-border); background:transparent; font-family:var(--font-sans);font-size:11px; outline:none;width:200px;"/>
      <button style="padding:7px 14px; border:1px solid var(--rpg-ink); background:var(--rpg-ink);color:white;cursor:pointer; font-family:var(--font-sans);font-size:10px;font-weight:600;letter-spacing:.1em;text-transform:uppercase;" @click="openCreate">+ Nouvel objet</button>
    </div>

    <div v-if="loading" class="d-flex justify-center pa-12"><v-progress-circular indeterminate color="primary" size="40" width="2"/></div>

    <template v-else>
      <div :style="panel ? 'display:grid;grid-template-columns:1fr 380px;gap:0;' : ''">
        <div>
          <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;margin-top:8px;">
            <div style="display:grid;grid-template-columns:80px 2fr 1fr 80px 1fr 120px;gap:0;">
              <div class="editorial-label">ID</div>
              <div class="editorial-label">Nom</div>
              <div class="editorial-label">Type</div>
              <div class="editorial-label">Prix</div>
              <div class="editorial-label">Effet</div>
              <div class="editorial-label text-right">Actions</div>
            </div>
          </div>

          <div v-for="(item, i) in filteredItems" :key="item.id" class="editorial-row" style="padding:14px 0; cursor:pointer;"
            :style="panel?.id === item.id && mode === 'view' ? 'background:rgba(0,0,0,0.02);' : ''"
            @click="(e) => openView(item, e)">
            <div style="display:grid;grid-template-columns:80px 2fr 1fr 80px 1fr 120px;gap:0;align-items:center;">
              <div style="font-size:10px;font-weight:600;color:var(--rpg-ink-muted);">ITM-{{ String(i+1).padStart(4,'0') }}</div>
              <div class="d-flex align-center ga-2">
                <div style="width:7px;height:7px;border-radius:50%;flex-shrink:0;" :style="`background:${typeColor(item.type)};`"/>
                <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;">{{ item.name }}</div>
              </div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);text-transform:capitalize;">{{ item.type }}</div>
              <div style="font-size:12px;font-weight:600;">{{ item.price }} 🪙</div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ item.effectValue ? `+${item.effectValue}` : '—' }}</div>
              <div style="text-align:right;" class="d-flex justify-end ga-3" @click.stop>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;" @click="(e) => openEdit(item, e)">Éditer</span>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;color:#C0392B;" @click="(e) => confirmDelete(item, e)">Suppr.</span>
              </div>
            </div>
          </div>

          <div v-if="filteredItems.length === 0" class="text-center py-12"><div class="editorial-label mb-2">Aucun résultat</div></div>
        </div>

        <div v-if="panel" ref="panelRef" style="border-left:1px solid var(--rpg-border);padding:32px 24px;position:sticky;top:80px;max-height:calc(100vh - 80px);overflow-y:auto;">
          <template v-if="mode === 'view'">
            <div class="editorial-label mb-3">{{ panel.type?.toUpperCase() }} · Détail</div>
            <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:900;margin-bottom:8px;">{{ panel.name }}</div>
            <div v-if="panel.description" style="font-size:13px;color:var(--rpg-ink-muted);font-style:italic;margin-bottom:20px;">{{ panel.description }}</div>
            <div style="border-top:1px solid var(--rpg-border);padding-top:16px;">
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
            <div class="editorial-label mb-4">{{ mode === 'create' ? '■ Nouvel objet' : '■ Modifier · ' + panel.name }}</div>
            <div v-for="field in formFields" :key="field.key" style="margin-bottom:16px;">
              <div class="editorial-label mb-1">{{ field.label }}</div>
              <select v-if="field.type === 'select'" v-model="(form as any)[field.key]" style="width:100%;padding:8px 10px;border:1px solid rgba(0,0,0,0.15);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;">
                <option v-for="opt in field.options" :key="opt" :value="opt">{{ opt }}</option>
              </select>
              <textarea v-else-if="field.type === 'textarea'" v-model="(form as any)[field.key]" style="width:100%;padding:8px 10px;border:1px solid rgba(0,0,0,0.15);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;resize:vertical;min-height:80px;"/>
              <input v-else :type="field.type" v-model="(form as any)[field.key]" style="width:100%;padding:8px 10px;border:1px solid rgba(0,0,0,0.15);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;"/>
            </div>
            <div v-if="formError" style="font-size:12px;color:#C0392B;border-left:2px solid #C0392B;padding:8px 12px;margin-bottom:12px;">{{ formError }}</div>
            <div style="display:flex;gap:8px;">
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-ink);background:var(--rpg-ink);color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" :disabled="saving" @click="save">{{ saving ? 'Enregistrement...' : mode === 'create' ? 'Créer' : 'Enregistrer' }}</button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Annuler</button>
            </div>
          </template>

          <template v-else-if="mode === 'delete'">
            <div class="editorial-label mb-4">■ Supprimer</div>
            <div style="font-family:var(--font-serif);font-size:1.2rem;font-weight:700;margin-bottom:12px;">{{ panel.name }}</div>
            <div style="font-size:13px;color:var(--rpg-ink-muted);margin-bottom:24px;line-height:1.6;">Cette action est irréversible.</div>
            <div style="display:flex;gap:8px;">
              <button style="flex:1;padding:10px;border:1px solid #C0392B;background:#C0392B;color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" :disabled="saving" @click="executeDelete">{{ saving ? 'Suppression...' : 'Confirmer' }}</button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Annuler</button>
            </div>
          </template>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import type { Item } from '@/interfaces/playerInventory'
import { useAuthStore } from '@/stores/auth'
import { computed, nextTick, onMounted, ref } from 'vue'
import api from '@/api/auth'
import * as C from '@/constants'

const auth      = useAuthStore()
const loading   = ref(true)
const search    = ref('')
const items     = ref<Item[]>([])
const panel     = ref<Item | null>(null)
const mode      = ref<'view'|'edit'|'create'|'delete'>('view')
const saving    = ref(false)
const formError = ref('')

const form = ref({ name: '', type: C.ITEM_TYPE_WEAPON, category: '', description: '', price: 0, effectValue: null as number|null, statModifiers: '' })

const adminTabs = [
    { label: 'Utilisateurs', route: 'AdminUsers',    active: false },
    { label: 'Objets',       route: null,            active: true  },
    { label: 'Compétences',  route: 'AdminSkills',   active: false },
    { label: 'Bestiaire',    route: 'AdminBestiary', active: false },
]

const formFields = [
    { key: 'name',        label: 'Nom',         type: 'text'     },
    { key: 'type',        label: 'Type',        type: 'select',  options: [C.ITEM_TYPE_WEAPON, C.ITEM_TYPE_ARMOR, C.ITEM_TYPE_ACCESSORY, C.ITEM_TYPE_CONSUMABLE] },
    { key: 'category',    label: 'Catégorie',   type: 'text'     },
    { key: 'description', label: 'Description', type: 'textarea' },
    { key: 'price',       label: 'Prix (or)',   type: 'number'   },
    { key: 'effectValue', label: 'Valeur effet', type: 'number'  },
]

const stats = computed(() => [
    { label: 'Total',        value: items.value.length },
    { label: 'Armes',        value: items.value.filter(i => i.type === C.ITEM_TYPE_WEAPON).length },
    { label: 'Armures',      value: items.value.filter(i => i.type === C.ITEM_TYPE_ARMOR).length },
    { label: 'Consommables', value: items.value.filter(i => i.type === C.ITEM_TYPE_CONSUMABLE).length },
])

const filteredItems = computed(() => {
    if (!search.value) return items.value
    const q = search.value.toLowerCase()
    return items.value.filter(i => i.name.toLowerCase().includes(q) || i.type.toLowerCase().includes(q))
})

const detailStats = computed(() => {
    if (!panel.value) return []
    return [
        { label: 'Type',         value: panel.value.type },
        { label: 'Catégorie',    value: panel.value.category ?? '—' },
        { label: 'Prix',         value: `${panel.value.price} 🪙` },
        { label: 'Valeur effet', value: panel.value.effectValue ? `+${panel.value.effectValue}` : '—' },
    ]
})

function scrollToElement(event?: MouseEvent) {
    if (!event) return
    nextTick(() => {
        const el = event.currentTarget as HTMLElement
        const y  = el.getBoundingClientRect().top + window.scrollY - 100
        window.scrollTo({ top: Math.max(0, y), behavior: 'smooth' })
    })
}

onMounted(async () => {
    try {
        const res = await api.get('/items')
        items.value = res.data.items ?? []
    } finally {
        loading.value = false
    }
})

function openView(item: Item, event?: MouseEvent)   { panel.value = item; mode.value = 'view'; scrollToElement(event) }
function confirmDelete(item: Item, event?: MouseEvent) { panel.value = item; mode.value = 'delete'; scrollToElement(event) }
function closePanel() { panel.value = null; formError.value = '' }

function openEdit(item: Item, event?: MouseEvent) {
    panel.value = item; mode.value = 'edit'
    Object.assign(form.value, { ...item })
    scrollToElement(event)
}

function openCreate() {
    panel.value = {} as Item; mode.value = 'create'
    form.value = { name: '', type: C.ITEM_TYPE_WEAPON, category: '', description: '', price: 0, effectValue: null, statModifiers: '' }
    formError.value = ''
    window.scrollTo({ top: 0, behavior: 'smooth' })
}

async function save() {
    if (!form.value.name.trim()) { formError.value = 'Le nom est requis.'; return }
    saving.value = true; formError.value = ''
    try {
        if (mode.value === 'create') {
            const res = await api.post('/items', form.value)
            items.value.push(res.data)
        } else {
            await api.put(`/items/${panel.value!.id}`, form.value)
            const idx = items.value.findIndex(i => i.id === panel.value!.id)
            if (idx !== -1) items.value[idx] = { ...items.value[idx], ...form.value }
        }
        closePanel()
    } catch { formError.value = 'Une erreur est survenue.' }
    finally { saving.value = false }
}

async function executeDelete() {
    saving.value = true
    try {
        await api.delete(`/items/${panel.value!.id}`)
        items.value = items.value.filter(i => i.id !== panel.value!.id)
        closePanel()
    } catch { formError.value = 'Erreur lors de la suppression.' }
    finally { saving.value = false }
}

function typeColor(type: string): string {
    const map: Record<string,string> = { weapon:'#C0392B', armor:'#2D4A8A', accessory:'#D68910', consumable:'#1E8449' }
    return map[type] ?? 'var(--rpg-ink-muted)'
}
</script>