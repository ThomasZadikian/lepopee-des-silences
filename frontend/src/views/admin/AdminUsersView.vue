<template>
  <div>
    <div style="margin: -32px -32px 0 -32px; padding: 10px 32px; background: var(--rpg-ink); color: white; display: flex; justify-content: space-between; align-items: center;">
      <div style="font-size:10px;font-weight:700;letter-spacing:.15em;text-transform:uppercase;">● RPG_ESI07 Admin · Console privée</div>
      <div style="font-size:10px;letter-spacing:.1em;color:rgba(255,255,255,0.5);">Session {{ auth.username }} · {{ new Date().toLocaleDateString('fr-FR') }}</div>
    </div>

    <div style="margin: 0 -32px; padding: 32px 32px 0; border-bottom: 1px solid var(--rpg-border); display: flex; justify-content: space-between; align-items: flex-end;">
      <div>
        <div class="editorial-label mb-2">Console Admin · Base utilisateurs</div>
        <div style="font-family:var(--font-serif);font-size:clamp(2rem,4vw,3rem);font-weight:900;letter-spacing:-0.03em;line-height:1;margin-bottom:24px;">Tous les utilisateurs</div>
      </div>
      <div class="d-flex ga-1 mb-0">
        <div v-for="tab in adminTabs" :key="tab.label"
          style="padding:8px 16px; border:1px solid var(--rpg-border); font-size:10px;font-weight:600;letter-spacing:.12em;text-transform:uppercase; cursor:pointer;color:var(--rpg-ink-muted);"
          :style="tab.active ? 'background:var(--rpg-ink);color:white;border-color:var(--rpg-ink);' : ''"
          @click="tab.route && $router.push({ name: tab.route })">{{ tab.label }}</div>
      </div>
    </div>

    <div style="margin: 0 -32px; display: grid; grid-template-columns: repeat(5, 1fr); border-bottom: 1px solid var(--rpg-border);">
      <div v-for="(stat, i) in stats" :key="stat.label" style="padding:24px 32px;" :style="i < 4 ? 'border-right:1px solid var(--rpg-border);' : ''">
        <div class="editorial-label mb-2">{{ stat.label }}</div>
        <div style="font-family:var(--font-serif);font-size:2rem;font-weight:700;line-height:1;">{{ stat.value }}</div>
      </div>
    </div>

    <div style="margin: 0 -32px; padding: 14px 32px; border-bottom: 1px solid var(--rpg-border);">
      <input v-model="search" type="text" placeholder="Rechercher..." style="padding:7px 12px; border:1px solid var(--rpg-border); background:transparent; font-family:var(--font-sans);font-size:11px; outline:none;width:200px;"/>
    </div>

    <div v-if="loading" class="d-flex justify-center pa-12"><v-progress-circular indeterminate color="primary" size="40" width="2"/></div>

    <template v-else>
      <div :style="panel ? 'display:grid;grid-template-columns:1fr 380px;gap:0;' : ''">
        <div>
          <div style="border-bottom:1px solid var(--rpg-border);padding:10px 0;margin-top:8px;">
            <div style="display:grid;grid-template-columns:40px 2fr 80px 80px 1fr 100px 100px 120px;gap:0;">
              <div class="editorial-label">#</div>
              <div class="editorial-label">Utilisateur</div>
              <div class="editorial-label">Inscrit</div>
              <div class="editorial-label">Rôle</div>
              <div class="editorial-label">Dernière co.</div>
              <div class="editorial-label">MFA</div>
              <div class="editorial-label">Statut</div>
              <div class="editorial-label text-right">Actions</div>
            </div>
          </div>

          <div v-for="(user, i) in filteredUsers" :key="user.id" class="editorial-row" style="padding:14px 0; cursor:pointer;"
            :style="panel?.id === user.id ? 'background:rgba(0,0,0,0.02);' : ''"
            @click="(e) => openView(user, e)">
            <div style="display:grid;grid-template-columns:40px 2fr 80px 80px 1fr 100px 100px 120px;gap:0;align-items:center;">
              <div style="font-size:11px;font-weight:600;color:var(--rpg-ink-muted);">{{ String(i+1).padStart(2,'0') }}</div>
              <div class="d-flex align-center ga-2">
                <div style="width:28px;height:28px;border-radius:50%;background:var(--rpg-ink);color:white;display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;flex-shrink:0;">{{ user.username.charAt(0).toUpperCase() }}</div>
                <div style="font-family:var(--font-serif);font-size:0.95rem;font-weight:700;">@{{ user.username }}</div>
              </div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ formatDate(user.createdAt) }}</div>
              <div>
                <span style="font-size:10px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;padding:3px 8px;"
                  :style="user.role === 'Admin' ? 'background:var(--rpg-ink);color:white;' : 'border:1px solid var(--rpg-border);color:var(--rpg-ink-muted);'">{{ user.role }}</span>
              </div>
              <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ formatDate(user.lastLoginAt) }}</div>
              <div class="d-flex align-center ga-1">
                <div style="width:6px;height:6px;border-radius:50%;" :style="user.mfaEnabled ? 'background:#1E8449;' : 'background:#D68910;'"/>
                <div style="font-size:11px;" :style="user.mfaEnabled ? 'color:#1E8449;' : 'color:#D68910;'">{{ user.mfaEnabled ? 'Actif' : 'Inactif' }}</div>
              </div>
              <div class="d-flex align-center ga-1">
                <div style="width:6px;height:6px;border-radius:50%;" :style="user.deletedAt ? 'background:#C0392B;' : 'background:#1E8449;'"/>
                <div style="font-size:11px;" :style="user.deletedAt ? 'color:#C0392B;' : 'color:#1E8449;'">{{ user.deletedAt ? 'Supprimé' : 'Actif' }}</div>
              </div>
              <div style="text-align:right;" class="d-flex justify-end ga-3" @click.stop>
                <span style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;" @click="(e) => openView(user, e)">Voir</span>
                <span v-if="!user.deletedAt" style="font-size:10px;font-weight:600;letter-spacing:.06em;text-transform:uppercase;cursor:pointer;text-decoration:underline;color:#C0392B;" @click.stop="(e) => confirmDelete(user, e)">Suppr.</span>
              </div>
            </div>
          </div>

          <div v-if="filteredUsers.length === 0" class="text-center py-12"><div class="editorial-label mb-2">Aucun résultat</div></div>
        </div>

        <div v-if="panel" ref="panelRef" style="border-left:1px solid var(--rpg-border);padding:32px 24px;position:sticky;top:80px;max-height:calc(100vh - 80px);overflow-y:auto;">
          <template v-if="mode === 'view'">
            <div class="editorial-label mb-3">Profil utilisateur</div>
            <div style="display:flex;align-items:center;gap:16px;margin-bottom:20px;">
              <div style="width:48px;height:48px;border-radius:50%;background:var(--rpg-ink);color:white;display:flex;align-items:center;justify-content:center;font-size:18px;font-weight:700;">{{ panel.username.charAt(0).toUpperCase() }}</div>
              <div>
                <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:900;">@{{ panel.username }}</div>
                <div style="font-size:11px;color:var(--rpg-ink-muted);">{{ panel.role }}</div>
              </div>
            </div>
            <div style="border-top:1px solid var(--rpg-border);padding-top:16px;">
              <div v-for="stat in detailStats" :key="stat.label" style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid var(--rpg-border);">
                <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ stat.label }}</div>
                <div style="font-size:13px;font-weight:600;">{{ stat.value }}</div>
              </div>
            </div>
            <div v-if="!panel.deletedAt" style="display:flex;gap:8px;margin-top:16px;">
              <button style="flex:1;padding:10px;border:1px solid #C0392B;background:#C0392B;color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="confirmDelete(panel)">Supprimer</button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Fermer</button>
            </div>
            <button v-else style="width:100%;margin-top:16px;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="closePanel">Fermer</button>
          </template>

          <template v-else-if="mode === 'delete'">
            <div class="editorial-label mb-4">■ Supprimer l'utilisateur</div>
            <div style="font-family:var(--font-serif);font-size:1.2rem;font-weight:700;margin-bottom:12px;">@{{ panel.username }}</div>
            <div style="font-size:13px;color:var(--rpg-ink-muted);margin-bottom:24px;line-height:1.6;">Cette action supprimera le compte. Les données personnelles seront anonymisées conformément au RGPD.</div>
            <div v-if="formError" style="font-size:12px;color:#C0392B;border-left:2px solid #C0392B;padding:8px 12px;margin-bottom:12px;">{{ formError }}</div>
            <div style="display:flex;gap:8px;">
              <button style="flex:1;padding:10px;border:1px solid #C0392B;background:#C0392B;color:white;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" :disabled="saving" @click="executeDelete">{{ saving ? 'Suppression...' : 'Confirmer' }}</button>
              <button style="flex:1;padding:10px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;" @click="openView(panel)">Annuler</button>
            </div>
          </template>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { computed, nextTick, onMounted, ref } from 'vue'
import api from '@/api/auth'

interface User { id: number; username: string; role: string; mfaEnabled: boolean; createdAt: string; lastLoginAt: string | null; deletedAt: string | null }

const auth      = useAuthStore()
const loading   = ref(true)
const search    = ref('')
const users     = ref<User[]>([])
const panel     = ref<User | null>(null)
const mode      = ref<'view'|'delete'>('view')
const saving    = ref(false)
const formError = ref('')

const adminTabs = [
    { label: 'Utilisateurs', route: null,            active: true  },
    { label: 'Objets',       route: 'AdminItems',    active: false },
    { label: 'Compétences',  route: 'AdminSkills',   active: false },
    { label: 'Bestiaire',    route: 'AdminBestiary', active: false },
]

const stats = computed(() => [
    { label: 'Total',      value: users.value.length },
    { label: 'Actifs',     value: users.value.filter(u => !u.deletedAt).length },
    { label: 'Admins',     value: users.value.filter(u => u.role === 'Admin').length },
    { label: 'MFA activé', value: users.value.filter(u => u.mfaEnabled).length },
    { label: 'Supprimés',  value: users.value.filter(u => u.deletedAt).length },
])

const filteredUsers = computed(() => {
    if (!search.value) return users.value
    const q = search.value.toLowerCase()
    return users.value.filter(u => u.username.toLowerCase().includes(q) || u.role.toLowerCase().includes(q))
})

const detailStats = computed(() => {
    if (!panel.value) return []
    return [
        { label: 'Rôle',         value: panel.value.role },
        { label: 'MFA',          value: panel.value.mfaEnabled ? 'Activé' : 'Inactif' },
        { label: 'Inscrit le',   value: formatDate(panel.value.createdAt) },
        { label: 'Dernière co.', value: formatDate(panel.value.lastLoginAt) },
        { label: 'Statut',       value: panel.value.deletedAt ? 'Supprimé' : 'Actif' },
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
        const res = await api.get('/users')
        users.value = res.data.items ?? []
    } finally {
        loading.value = false
    }
})

function openView(user: User, event?: MouseEvent)    { panel.value = user; mode.value = 'view'; scrollToElement(event) }
function confirmDelete(user: User, event?: MouseEvent) { panel.value = user; mode.value = 'delete'; scrollToElement(event) }
function closePanel() { panel.value = null; formError.value = '' }

async function executeDelete() {
    saving.value = true; formError.value = ''
    try {
        await api.delete(`/users/${panel.value!.id}`)
        const idx = users.value.findIndex(u => u.id === panel.value!.id)
        if (idx !== -1) users.value[idx] = { ...users.value[idx], deletedAt: new Date().toISOString() }
        openView(users.value[idx])
    } catch { formError.value = 'Erreur lors de la suppression.' }
    finally { saving.value = false }
}

function formatDate(date: string | null): string {
    if (!date) return '—'
    return new Date(date).toLocaleDateString('fr-FR')
}
</script>