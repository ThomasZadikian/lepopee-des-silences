<template>
  <div>
    <div style="margin: -32px -32px 0 -32px; padding: 10px 32px; background: var(--rpg-ink); color: white; display: flex; justify-content: space-between; align-items: center;">
      <div style="font-size:10px;font-weight:700;letter-spacing:.15em;text-transform:uppercase;">● RPG_ESI07 Admin · Console privée</div>
      <div style="font-size:10px;letter-spacing:.1em;color:rgba(255,255,255,0.5);">Session {{ auth.username }} · {{ new Date().toLocaleDateString('fr-FR') }}</div>
    </div>

    <div style="margin: 0 -32px; padding: 32px 32px 0; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: flex-end;">
      <div>
        <div class="editorial-label mb-2">Console Admin · Arbres de talents</div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">Toutes les compétences</div>
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
      <button style="padding:7px 14px; border:1px solid var(--rpg-ink); background:var(--rpg-ink);color:white;cursor:pointer; font-family:var(--font-sans);font-size:10px;font-weight:600;letter-spacing:.1em;text-transform:uppercase;" @click="openCreate">+ Nouvelle compétence</button>
    </div>

    <div v-if="loading" class="d-flex justify-center pa-12"><v-progress-circular indeterminate color="primary" size="40" width="2"/></div>

    <template v-else>
      <div style="position: relative;" :style="panel ? 'display:grid;grid-template-columns:1fr 380px;gap:0;align-items:start;' : ''">
        <div>
          <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;margin-top:8px;">
            <div style="display:grid;grid-template-columns:80px 2fr 1fr 1fr 80px 80px 120px;gap:0;">
              <div class="editorial-label">ID</div>
              <div class="editorial-label">Nom</div>
              <div class="editorial-label">Effet</div>
              <div class="editorial-label">Élément</div>
              <div class="editorial-label">MP</div>
              <div class="editorial-label">DMG</div>
              <div class="editorial-label text-right">Actions</div>
            </div>
          </div>

          <div v-for="(skill, i) in filteredSkills" :key="skill.id" class="editorial-row" style="padding:14px 0; cursor:pointer;"
            :style="panel?.id === skill.id && mode === 'view' ? 'background:rgba(0,0,0,0.02);' : ''"
            @click.self="(e) => openView(skill, e)">
            <div style="display:grid;grid-template-columns:80px 2fr 1fr 1fr 80px 80px 120px;gap:0;align-items:center; pointer-events:none;">
              <div style="font-size:10px;font-weight:600;color:var(--rpg-ink-muted);">SKL-{{ String(i+1).padStart(3,'0') }}</div>
              <div class="d-flex align-center ga-2">
                <div style="width:7px;height:7px;border-radius:50%;flex-shrink:0;" :style="`background:${effectColor(skill.effectType)};`"/>
                <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;">{{ skill.name }}</div>
              </div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);text-transform:capitalize;">{{ skill.effectType }}</div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ skill.elementType ?? '—' }}</div>
              <div style="font-size:12px;font-weight:600;">{{ skill.mpCost }}</div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ skill.baseDamage ?? '—' }}</div>
              <div style="text-align:right; pointer-events:auto;" class="d-flex justify-end ga-3" @click.stop>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;" @click="(e) => openEdit(skill, e)">Éditer</span>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;color:#C0392B;" @click="(e) => confirmDelete(skill, e)">Suppr.</span>
              </div>
            </div>
          </div>

          <div v-if="filteredSkills.length === 0" class="text-center py-12"><div class="editorial-label mb-2">Aucun résultat</div></div>
        </div>

        <div v-if="panel" ref="panelRef" :style="{ marginTop: panelMarginTop + 'px' }" style="border-left:1px solid var(--rpg-border);padding:32px 24px;max-height:calc(100vh - 80px);overflow-y:auto;">
          <template v-if="mode === 'view'">
            <div class="editorial-label mb-3">{{ panel.effectType?.toUpperCase() }} · Détail</div>
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
            <div class="editorial-label mb-4">{{ mode === 'create' ? '■ Nouvelle compétence' : '■ Modifier · ' + panel.name }}</div>
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
import type { Skill } from '@/interfaces/playerSkill'
import { useAuthStore } from '@/stores/auth'
import { computed, onMounted, ref } from 'vue'
import api from '@/api/auth'
import * as C from '@/constants'


const auth      = useAuthStore()
const loading   = ref(true)
const search    = ref('')
const skills    = ref<Skill[]>([])
const panel     = ref<Skill | null>(null)
const mode      = ref<'view'|'edit'|'create'|'delete'>('view')
const saving    = ref(false)
const formError = ref('')
const panelMarginTop = ref(0)

const form = ref({ name: '', effectType: C.EFFECT_DAMAGE, elementType: C.ELEMENT_NEUTRAL, description: '', mpCost: 10, baseDamage: null as number|null, healAmount: null as number|null })

const adminTabs = [
    { label: 'Utilisateurs', route: 'AdminUsers',    active: false },
    { label: 'Objets',       route: 'AdminItems',    active: false },
    { label: 'Compétences',  route: null,            active: true  },
    { label: 'Bestiaire',    route: 'AdminBestiary', active: false },
]

const formFields = [
    { key: 'name',        label: 'Nom',           type: 'text'     },
    { key: 'effectType',  label: "Type d'effet",  type: 'select',  options: [C.EFFECT_DAMAGE, C.EFFECT_HEAL, C.EFFECT_BUFF, C.EFFECT_DEBUFF] },
    { key: 'elementType', label: 'Élément',       type: 'select',  options: [C.ELEMENT_NEUTRAL, C.ELEMENT_FIRE, C.ELEMENT_ICE, C.ELEMENT_LIGHTNING] },
    { key: 'description', label: 'Description',   type: 'textarea' },
    { key: 'mpCost',      label: 'Coût MP',       type: 'number'   },
    { key: 'baseDamage',  label: 'Dégâts de base', type: 'number'  },
    { key: 'healAmount',  label: 'Soin de base',  type: 'number'   },
]

const stats = computed(() => [
    { label: 'Total',       value: skills.value.length },
    { label: 'Attaque',     value: skills.value.filter(s => s.effectType === C.EFFECT_DAMAGE).length },
    { label: 'Soin',        value: skills.value.filter(s => s.effectType === C.EFFECT_HEAL).length },
    { label: 'Buff/Debuff', value: skills.value.filter(s => [C.EFFECT_BUFF, C.EFFECT_DEBUFF].includes(s.effectType)).length },
])

const filteredSkills = computed(() => {
    if (!search.value) return skills.value
    const q = search.value.toLowerCase()
    return skills.value.filter(s => s.name.toLowerCase().includes(q) || s.effectType.toLowerCase().includes(q))
})

const detailStats = computed(() => {
    if (!panel.value) return []
    return [
        { label: "Type d'effet", value: panel.value.effectType },
        { label: 'Élément',      value: panel.value.elementType ?? '—' },
        { label: 'Coût MP',      value: panel.value.mpCost },
        { label: 'Dégâts',       value: panel.value.baseDamage ?? '—' },
        { label: 'Soin',         value: panel.value.healAmount ?? '—' },
    ]
})

onMounted(async () => {
    try {
        const res = await api.get('/skills')
        skills.value = res.data.items ?? []
    } finally {
        loading.value = false
    }
})

function alignPanel(event?: MouseEvent) {
    if (!event) return
    const el = event.currentTarget as HTMLElement
    panelMarginTop.value = el.offsetTop
}

function closePanel() { 
    panel.value = null
    formError.value = '' 
    panelMarginTop.value = 0
}

function openView(skill: Skill, event?: MouseEvent) {
    panel.value = skill
    mode.value  = 'view'
    alignPanel(event)
}

function openEdit(skill: Skill, event?: MouseEvent) {
    panel.value = skill
    mode.value  = 'edit'
    Object.assign(form.value, { ...skill })
    alignPanel(event)
}

function confirmDelete(skill: Skill, event?: MouseEvent) {
    panel.value = skill
    mode.value  = 'delete'
    alignPanel(event)
}

function openCreate() {
    panel.value = { id: 0, name: '', effectType: C.EFFECT_DAMAGE, elementType: C.ELEMENT_NEUTRAL, description: '', mpCost: 10, baseDamage: null, healAmount: null } as Skill
    mode.value = 'create'
    form.value = { name: '', effectType: C.EFFECT_DAMAGE, elementType: C.ELEMENT_NEUTRAL, description: '', mpCost: 10, baseDamage: null, healAmount: null }
    formError.value = ''
    panelMarginTop.value = 0
    window.scrollTo({ top: 0, behavior: 'smooth' })
}

async function save() {
    if (!form.value.name.trim()) { formError.value = 'Le nom est requis.'; return }
    saving.value = true; formError.value = ''
    try {
        if (mode.value === 'create') {
            const res = await api.post('/skills', form.value)
            skills.value.push(res.data)
        } else {
            await api.put(`/skills/${panel.value!.id}`, form.value)
            const idx = skills.value.findIndex(s => s.id === panel.value!.id)
            if (idx !== -1) skills.value[idx] = { ...skills.value[idx], ...form.value }
        }
        closePanel()
    } catch { formError.value = 'Une erreur est survenue.' }
    finally { saving.value = false }
}

async function executeDelete() {
    saving.value = true
    try {
        await api.delete(`/skills/${panel.value!.id}`)
        skills.value = skills.value.filter(s => s.id !== panel.value!.id)
        closePanel()
    } catch { formError.value = 'Erreur lors de la suppression.' }
    finally { saving.value = false }
}

function effectColor(type: string): string {
    const map: Record<string,string> = { [C.EFFECT_DAMAGE]:'#C0392B', [C.EFFECT_HEAL]:'#1E8449', [C.EFFECT_BUFF]:'#D68910', [C.EFFECT_DEBUFF]:'#2D4A8A' }
    return map[type] ?? 'var(--rpg-ink-muted)'
}
</script>