<template>
  <div>
    <!-- Hero -->
    <div style="margin: -32px -32px 40px -32px; border-bottom: 1px solid var(--rpg-border);">
      <div style="display:grid;grid-template-columns:2fr 1fr 1fr;min-height:140px;">
        <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Outil · Simulation</div>
          <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,2.8rem);font-weight:900;line-height:1;letter-spacing:-0.03em;">
            Calculateur
            <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">de scaling.</span>
          </div>
        </div>
        <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Multiplicateur</div>
          <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
            {{ result ? `×${result.enemy.multiplier.toFixed(2)}` : '—' }}
          </div>
        </div>
        <div style="padding:40px 32px;">
          <div class="editorial-label mb-3">Puissance joueur</div>
          <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
            {{ playerPower > 0 ? playerPower : '—' }}
          </div>
        </div>
      </div>
    </div>

    <div :style="result ? 'display:grid;grid-template-columns:1fr 1fr;gap:48px;' : ''">
      <!-- Panneau de configuration -->
      <div>
        <!-- Mode de saisie -->
        <div style="display:flex;gap:0;margin-bottom:32px;border:1px solid var(--rpg-border);">
          <button
            style="flex:1;padding:12px;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;cursor:pointer;border:none;transition:all .15s;"
            :style="mode === 'import' ? 'background:var(--rpg-ink);color:white;' : 'background:transparent;color:var(--rpg-ink-muted);'"
            @click="switchMode('import')"
          >
            Mon personnage
          </button>
          <button
            style="flex:1;padding:12px;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;cursor:pointer;border:none;border-left:1px solid var(--rpg-border);transition:all .15s;"
            :style="mode === 'custom' ? 'background:var(--rpg-ink);color:white;' : 'background:transparent;color:var(--rpg-ink-muted);'"
            @click="switchMode('custom')"
          >
            Personnage custom
          </button>
        </div>

        <!-- Mode import -->
        <template v-if="mode === 'import'">
          <div v-if="loadingProfile" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate color="primary" size="32" width="2"/>
          </div>
          <template v-else-if="profile">
            <div style="border:1px solid var(--rpg-border);padding:20px;margin-bottom:24px;">
              <div class="editorial-label mb-3">Personnage importé</div>
              <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;margin-bottom:12px;">{{ profile.characterName }}</div>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;">
                <div v-for="stat in profileStats" :key="stat.label">
                  <div style="font-size:10px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;color:var(--rpg-ink-muted);margin-bottom:2px;">{{ stat.label }}</div>
                  <div style="font-family:var(--font-serif);font-size:1.1rem;font-weight:700;">{{ stat.value }}</div>
                </div>
              </div>
            </div>

            <div style="margin-bottom:24px;">
              <div class="editorial-label mb-3">Items équipés</div>
              <div v-if="equippedItems.length === 0" style="font-size:13px;color:var(--rpg-ink-muted);font-style:italic;">Aucun item équipé.</div>
              <div v-for="item in equippedItems" :key="item.id" style="display:flex;justify-content:space-between;align-items:center;padding:10px 0;border-bottom:1px solid var(--rpg-border);">
                <div>
                  <div style="font-size:13px;font-weight:600;">{{ item.item.name }}</div>
                  <div style="font-size:11px;color:var(--rpg-ink-muted);text-transform:capitalize;">{{ item.item.type }}</div>
                </div>
                <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;color:#1E8449;">+{{ item.item.effectValue ?? 0 }}</div>
              </div>
              <div v-if="equippedItems.length > 0" style="display:flex;justify-content:space-between;padding:10px 0;border-top:2px solid var(--rpg-border);margin-top:4px;">
                <div style="font-size:12px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;">Bonus total</div>
                <div style="font-family:var(--font-serif);font-size:1.1rem;font-weight:700;color:#1E8449;">+{{ totalEquipmentBonus }}</div>
              </div>
            </div>

            <div style="padding:12px 16px;background:rgba(0,0,0,0.03);border-left:2px solid var(--rpg-border);margin-bottom:24px;">
              <div style="font-size:11px;color:var(--rpg-ink-muted);">Puissance joueur = niveau × 10 + bonus équipement</div>
              <div style="font-family:var(--font-serif);font-size:1.1rem;font-weight:700;margin-top:4px;">
                {{ profile.level }} × 10 + {{ totalEquipmentBonus }} = <span style="color:var(--rpg-ink);">{{ playerPower }}</span>
              </div>
            </div>
          </template>
          <div v-else style="font-size:13px;color:var(--rpg-ink-muted);">Impossible de charger le profil.</div>
        </template>

        <!-- Mode custom -->
        <template v-else>
          <div style="margin-bottom:24px;">
            <div class="editorial-label mb-2">Niveau du personnage</div>
            <input
              type="number" v-model.number="customLevel" min="1" max="99"
              style="width:100%;padding:10px 12px;border:1px solid var(--rpg-border);background:transparent;font-family:var(--font-sans);font-size:14px;outline:none;"
              @keydown="blockInvalidChars"
            />
            <div style="font-size:11px;color:var(--rpg-ink-muted);margin-top:4px;">Entre 1 et 99</div>
          </div>

          <div style="margin-bottom:24px;">
            <div class="editorial-label mb-3">Items</div>
            <div v-if="loadingItems" class="d-flex justify-center pa-4">
              <v-progress-circular indeterminate color="primary" size="24" width="2"/>
            </div>
            <template v-else>
              <div v-for="category in itemCategories" :key="category.type" style="margin-bottom:16px;">
                <div style="font-size:10px;font-weight:700;letter-spacing:.1em;text-transform:uppercase;color:var(--rpg-ink-muted);margin-bottom:6px;">
                  {{ category.label }}
                </div>
                <select
                  v-model="selectedByType[category.type]"
                  style="width:100%;padding:8px 10px;border:1px solid var(--rpg-border);background:transparent;font-family:var(--font-sans);font-size:13px;outline:none;"
                  @change="onItemSelect"
                >
                  <option value="">— Aucun —</option>
                  <option v-for="item in itemsByType(category.type)" :key="item.id" :value="item.id">
                    {{ item.name }} (+{{ item.effectValue ?? 0 }})
                  </option>
                </select>
              </div>

              <div v-if="totalEquipmentBonus > 0" style="display:flex;justify-content:space-between;padding:10px 0;border-top:2px solid var(--rpg-border);margin-top:4px;">
                <div style="font-size:12px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;">Bonus total</div>
                <div style="font-family:var(--font-serif);font-size:1.1rem;font-weight:700;color:#1E8449;">+{{ totalEquipmentBonus }}</div>
              </div>
            </template>
          </div>

          <div style="padding:12px 16px;background:rgba(0,0,0,0.03);border-left:2px solid var(--rpg-border);margin-bottom:24px;">
            <div style="font-size:11px;color:var(--rpg-ink-muted);">Puissance joueur = niveau × 10 + bonus équipement</div>
            <div style="font-family:var(--font-serif);font-size:1.1rem;font-weight:700;margin-top:4px;">
              {{ customLevel }} × 10 + {{ totalEquipmentBonus }} = <span style="color:var(--rpg-ink);">{{ playerPower }}</span>
            </div>
          </div>
        </template>

        <!-- Sélection ennemi -->
        <div style="margin-bottom:24px;">
          <div class="editorial-label mb-3">Ennemi à simuler</div>
          <div v-if="loadingEnemies" class="d-flex justify-center pa-4">
            <v-progress-circular indeterminate color="primary" size="24" width="2"/>
          </div>
          <div v-else>
            <input v-model="enemySearch" type="text" placeholder="Rechercher un ennemi..."
              style="width:100%;padding:8px 12px;border:1px solid var(--rpg-border);background:transparent;font-family:var(--font-sans);font-size:12px;outline:none;margin-bottom:8px;"/>
            <div style="max-height:200px;overflow-y:auto;border:1px solid var(--rpg-border);">
              <div v-for="enemy in filteredEnemies" :key="enemy.id"
                style="padding:10px 14px;cursor:pointer;display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid rgba(0,0,0,0.06);"
                :style="selectedEnemy?.id === enemy.id ? 'background:var(--rpg-ink);color:white;' : ''"
                @click="selectEnemy(enemy)">
                <div>
                  <div style="font-size:13px;font-weight:600;">{{ enemy.name }}</div>
                  <div style="font-size:10px;text-transform:uppercase;letter-spacing:.06em;opacity:0.7;">{{ enemy.type }}</div>
                </div>
                <div style="font-size:11px;opacity:0.7;">HP {{ enemy.maxHP }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Erreur -->
        <div v-if="error" style="font-size:12px;color:#C0392B;border-left:2px solid #C0392B;padding:8px 12px;margin-bottom:12px;">{{ error }}</div>

        <!-- Bouton calcul -->
        <button
          style="width:100%;padding:14px;border:1px solid var(--rpg-ink);background:var(--rpg-ink);color:white;cursor:pointer;font-family:var(--font-sans);font-size:11px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;"
          :disabled="!canCalculate || calculating"
          :style="!canCalculate ? 'opacity:0.4;cursor:not-allowed;' : ''"
          @click="calculate"
        >
          {{ calculating ? 'Calcul en cours...' : 'Simuler le combat' }}
        </button>
      </div>

      <!-- Résultats -->
      <div v-if="result">
        <div class="editorial-label mb-4">Résultats · {{ result.enemy.name }}</div>

        <!-- Multiplicateur visuel -->
        <div style="border:1px solid var(--rpg-border);padding:24px;margin-bottom:24px;text-align:center;">
          <div class="editorial-label mb-2">Multiplicateur de scaling</div>
          <div style="font-family:var(--font-serif);font-size:3.5rem;font-weight:900;letter-spacing:-0.03em;line-height:1;">
            ×{{ result.enemy.multiplier.toFixed(2) }}
          </div>
          <div style="height:4px;background:rgba(0,0,0,0.06);margin-top:16px;border-radius:2px;">
            <div :style="`width:${Math.min((result.enemy.multiplier / 20) * 100, 100)}%;height:100%;background:${multiplierColor};border-radius:2px;transition:width 0.5s;`"/>
          </div>
          <div style="font-size:11px;color:var(--rpg-ink-muted);margin-top:8px;">{{ multiplierLabel }}</div>
        </div>

        <!-- Comparaison stats -->
        <div style="border:1px solid var(--rpg-border);margin-bottom:24px;">
          <div style="display:grid;grid-template-columns:1fr 1fr 1fr;border-bottom:1px solid var(--rpg-border);padding:10px 16px;">
            <div class="editorial-label">Stat</div>
            <div class="editorial-label text-center">Base</div>
            <div class="editorial-label text-right">Scalé</div>
          </div>
          <div v-for="stat in comparisonStats" :key="stat.label"
            style="display:grid;grid-template-columns:1fr 1fr 1fr;padding:12px 16px;border-bottom:1px solid rgba(0,0,0,0.06);align-items:center;">
            <div style="font-size:12px;color:var(--rpg-ink-muted);">{{ stat.label }}</div>
            <div style="font-size:13px;text-align:center;color:var(--rpg-ink-muted);">{{ stat.base }}</div>
            <div style="text-align:right;">
              <span style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">{{ stat.scaled }}</span>
            </div>
          </div>
        </div>

        <!-- Analyse du combat -->
        <div style="padding:16px;border:1px solid var(--rpg-border);">
          <div class="editorial-label mb-3">Analyse du combat</div>

          <!-- Métriques -->
          <div style="display:grid;grid-template-columns:1fr 1fr 1fr;border:1px solid var(--rpg-border);margin-bottom:16px;">
            <div style="padding:14px;border-right:1px solid var(--rpg-border);text-align:center;">
              <div class="editorial-label mb-1">Tuer l'ennemi</div>
              <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;">~{{ combatAnalysis?.turnsToKillEnemy }}</div>
              <div style="font-size:10px;color:var(--rpg-ink-muted);">tours</div>
            </div>
            <div style="padding:14px;border-right:1px solid var(--rpg-border);text-align:center;">
              <div class="editorial-label mb-1">Survivre</div>
              <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;">~{{ combatAnalysis?.turnsToKillPlayer }}</div>
              <div style="font-size:10px;color:var(--rpg-ink-muted);">tours</div>
            </div>
            <div style="padding:14px;text-align:center;">
              <div class="editorial-label mb-1">Ratio</div>
              <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;" :style="`color:${multiplierColor};`">
                {{ combatAnalysis?.survivalRatio.toFixed(2) }}
              </div>
              <div style="font-size:10px;color:var(--rpg-ink-muted);">survie/attaque</div>
            </div>
          </div>

          <!-- Verdict -->
          <div style="font-size:13px;line-height:1.6;color:var(--rpg-ink-muted);margin-bottom:16px;">{{ analysisText }}</div>

          <!-- Formule + disclaimer -->
          <div style="padding:12px;background:rgba(0,0,0,0.03);border-left:2px solid var(--rpg-border);">
            <div class="editorial-label mb-2">Formule utilisée</div>
            <div style="font-size:11px;color:var(--rpg-ink-muted);line-height:1.8;">
              Tours pour tuer l'ennemi = HP_ennemi_scalé ÷ Force_joueur<br>
              Tours pour mourir = HP_joueur ÷ Force_ennemi_scalé<br>
              Ratio de survie = Tours pour mourir ÷ Tours pour tuer (> 1 = victoire probable)
              <span v-if="mode === 'custom'"><br>HP joueur estimé = 100 + (niveau − 1) × 10<br>Force joueur estimée = 10 + (niveau − 1) × 2</span>
            </div>
            <div style="margin-top:10px;font-size:11px;color:#D68910;font-style:italic;">
              ⚠ Cette simulation est purement statistique. Elle ne tient pas compte des compétences actives du joueur ou de l'ennemi, des effets de statut, des résistances élémentaires, ni du niveau de difficulté de la partie.
            </div>
          </div>
        </div>

        <!-- Nouvelle simulation -->
        <button
          style="width:100%;margin-top:16px;padding:12px;border:1px solid var(--rpg-border);background:transparent;cursor:pointer;font-family:var(--font-sans);font-size:10px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;color:var(--rpg-ink-muted);"
          @click="reset"
        >
          Nouvelle simulation
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { enemiesApi } from '@/api/enemies'
import { inventoryApi } from '@/api/inventory'
import type { Enemy } from '@/interfaces/bestiary'
import type { Item } from '@/interfaces/playerInventory'
import type { PlayerInventory } from '@/interfaces/playerInventory'
import type { PlayerProfileResponse } from '@/interfaces/playerProfile'
import { computed, onMounted, ref } from 'vue'
import api from '@/api/auth'

interface ScaledEnemy {
    id: number; name: string; type: string; multiplier: number
    scaledMaxHP: number; scaledStrength: number; scaledIntelligence: number; scaledSpeed: number
    experienceReward: number; goldReward: number
}

interface CalculatorResult {
    enemy: ScaledEnemy
}

// ── State ──────────────────────────────────────────────────────────────────────
const mode           = ref<'import'|'custom'>('import')
const loadingProfile = ref(false)
const loadingItems   = ref(false)
const loadingEnemies = ref(false)
const calculating    = ref(false)
const error          = ref('')

const profile       = ref<PlayerProfileResponse | null>(null)
const inventory     = ref<PlayerInventory[]>([])
const allItems      = ref<Item[]>([])
const enemies       = ref<Enemy[]>([])
const enemySearch   = ref('')
const selectedEnemy = ref<Enemy | null>(null)
const customLevel   = ref(1)
const result        = ref<CalculatorResult | null>(null)

const selectedByType = ref<Record<string, number | ''>>({
    weapon:     '',
    armor:      '',
    accessory:  '',
    consumable: '',
})

const itemCategories = [
    { type: 'weapon',     label: 'Arme' },
    { type: 'armor',      label: 'Armure' },
    { type: 'accessory',  label: 'Accessoire' },
    { type: 'consumable', label: 'Consommable' },
]

// ── Computed ───────────────────────────────────────────────────────────────────
const equippedItems = computed(() =>
    inventory.value.filter(i => i.isEquipped && (i.item.effectValue ?? 0) > 0)
)

const totalEquipmentBonus = computed(() => {
    if (mode.value === 'import') {
        return equippedItems.value.reduce((sum, i) => sum + (i.item.effectValue ?? 0), 0)
    }
    return Object.values(selectedByType.value)
        .filter(id => id !== '')
        .reduce((sum, id) => {
            const item = allItems.value.find(i => i.id === id)
            return sum + (item?.effectValue ?? 0)
        }, 0)
})

const playerLevel = computed(() =>
    mode.value === 'import' ? (profile.value?.level ?? 1) : customLevel.value
)

const playerPower = computed(() =>
    playerLevel.value * 10 + totalEquipmentBonus.value
)

const canCalculate = computed(() =>
    selectedEnemy.value !== null && playerLevel.value >= 1 && playerLevel.value <= 99
)

const filteredEnemies = computed(() => {
    if (!enemySearch.value) return enemies.value
    const q = enemySearch.value.toLowerCase()
    return enemies.value.filter(e => e.name.toLowerCase().includes(q) || e.type.toLowerCase().includes(q))
})

const profileStats = computed(() => {
    if (!profile.value) return []
    return [
        { label: 'Niveau',       value: profile.value.level },
        { label: 'Personnage',   value: profile.value.characterName },
        { label: 'HP',           value: `${profile.value.currentHP} / ${profile.value.maxHP}` },
        { label: 'Force',        value: profile.value.strength },
        { label: 'Intelligence', value: profile.value.intelligence },
        { label: 'Vitesse',      value: profile.value.speed },
    ]
})

const comparisonStats = computed(() => {
    if (!result.value || !selectedEnemy.value) return []
    const e = result.value.enemy
    const b = selectedEnemy.value
    return [
        { label: 'HP max',       base: b.maxHP,            scaled: e.scaledMaxHP },
        { label: 'Force',        base: b.strength,         scaled: e.scaledStrength },
        { label: 'Intelligence', base: b.intelligence,     scaled: e.scaledIntelligence },
        { label: 'Vitesse',      base: b.speed,            scaled: e.scaledSpeed },
        { label: 'Récomp. XP',  base: b.experienceReward, scaled: e.experienceReward },
        { label: 'Récomp. Or',  base: b.goldReward,       scaled: e.goldReward },
    ]
})

const estimatedPlayerStats = computed(() => {
    if (mode.value === 'import' && profile.value) {
        return {
            hp:       profile.value.maxHP,
            strength: profile.value.strength,
        }
    }
    return {
        hp:       100 + (customLevel.value - 1) * 10,
        strength: 10  + (customLevel.value - 1) * 2,
    }
})

const combatAnalysis = computed(() => {
    if (!result.value || !selectedEnemy.value) return null
    const e = result.value.enemy
    const p = estimatedPlayerStats.value
    const turnsToKillEnemy  = Math.ceil(e.scaledMaxHP    / Math.max(p.strength, 1))
    const turnsToKillPlayer = Math.ceil(p.hp             / Math.max(e.scaledStrength, 1))
    const survivalRatio     = turnsToKillPlayer / turnsToKillEnemy
    return { turnsToKillEnemy, turnsToKillPlayer, survivalRatio }
})

const multiplierColor = computed(() => {
    if (!combatAnalysis.value) return 'var(--rpg-ink-muted)'
    const { survivalRatio } = combatAnalysis.value
    if (survivalRatio >= 1.5) return '#1E8449'
    if (survivalRatio >= 1.0) return '#D68910'
    return '#C0392B'
})

const multiplierLabel = computed(() => {
    if (!combatAnalysis.value) return ''
    const { survivalRatio } = combatAnalysis.value
    if (survivalRatio >= 3)   return 'Victoire facile'
    if (survivalRatio >= 1.5) return 'Victoire probable'
    if (survivalRatio >= 1.0) return 'Combat équilibré'
    if (survivalRatio >= 0.5) return 'Défaite probable'
    return 'Défaite certaine'
})

const analysisText = computed(() => {
    if (!combatAnalysis.value || !selectedEnemy.value) return ''
    const { turnsToKillEnemy, turnsToKillPlayer, survivalRatio } = combatAnalysis.value
    const type = selectedEnemy.value.type
    let verdict = ''
    if      (survivalRatio >= 3)   verdict = `Vous éliminez ce ${type} largement avant qu'il ne soit dangereux.`
    else if (survivalRatio >= 1.5) verdict = `Combat favorable — vous avez une marge confortable.`
    else if (survivalRatio >= 1.0) verdict = `Combat serré — une erreur peut être fatale.`
    else if (survivalRatio >= 0.5) verdict = `Ce ${type} vous surpasse — le combat sera difficile.`
    else                           verdict = `Ce ${type} vous élimine avant que vous puissiez réagir.`
    return `${verdict} Vous tuez l'ennemi en ~${turnsToKillEnemy} tours, mais vous tombez en ~${turnsToKillPlayer} tours si rien ne change.`
})

// ── Helpers ────────────────────────────────────────────────────────────────────
function itemsByType(type: string) {
    return allItems.value.filter(i => i.type === type)
}

// ── Lifecycle ──────────────────────────────────────────────────────────────────
onMounted(async () => {
    await Promise.all([loadEnemies(), loadProfile()])
})

async function loadProfile() {
    loadingProfile.value = true
    try {
        const [profileRes, inventoryRes] = await Promise.all([
            api.get('/playerprofile/me'),
            inventoryApi.getMe()
        ])
        profile.value   = profileRes.data
        inventory.value = inventoryRes.data?.items ?? []
    } catch {
        profile.value = null
    } finally {
        loadingProfile.value = false
    }
}

async function loadEnemies() {
    loadingEnemies.value = true
    try {
        const res     = await enemiesApi.getAll()
        enemies.value = res.data.items ?? []
    } finally {
        loadingEnemies.value = false
    }
}

async function loadAllItems() {
    loadingItems.value = true
    try {
        const res      = await api.get('/items')
        allItems.value = res.data.items ?? []
    } finally {
        loadingItems.value = false
    }
}

// ── Actions ────────────────────────────────────────────────────────────────────
function switchMode(m: 'import'|'custom') {
    mode.value           = m
    result.value         = null
    selectedByType.value = { weapon: '', armor: '', accessory: '', consumable: '' }
    if (m === 'custom' && allItems.value.length === 0) loadAllItems()
}

function selectEnemy(enemy: Enemy) {
    selectedEnemy.value = enemy
    result.value        = null
}

function onItemSelect() {
    result.value = null
}

async function calculate() {
    if (!canCalculate.value) return
    if (playerLevel.value < 1 || playerLevel.value > 99) {
        error.value = 'Niveau invalide — entre 1 et 99.'
        return
    }
    if (totalEquipmentBonus.value < 0 || totalEquipmentBonus.value > 500) {
        error.value = 'Bonus équipement invalide.'
        return
    }
    calculating.value = true
    error.value       = ''
    try {
        const res    = await api.get(
            `/enemies/${selectedEnemy.value!.id}/scaled`,
            { params: { playerLevel: playerLevel.value, equipmentBonus: totalEquipmentBonus.value } }
        )
        result.value = res.data
    } catch {
        error.value = 'Erreur lors du calcul. Réessayez.'
    } finally {
        calculating.value = false
    }
}

function reset() {
    result.value         = null
    selectedEnemy.value  = null
    enemySearch.value    = ''
    error.value          = ''
    selectedByType.value = { weapon: '', armor: '', accessory: '', consumable: '' }
}

function blockInvalidChars(e: KeyboardEvent) {
    if (['e', 'E', '+', '-', '.', ','].includes(e.key)) e.preventDefault()
}
</script>