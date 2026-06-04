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

    <template v-else>
      <!-- ── HERO ───────────────────────────────────────────────────── -->
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
              Mes données · Équipement
            </div>
            <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,3rem);font-weight:900;line-height:1;letter-spacing:-0.03em;margin-bottom:12px;">
              Inventaire
              <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">&amp; fardeau.</span>
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Total items
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ inventory.length }}
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Équipés
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ equippedCount }}
            </div>
          </div>
          <div style="padding:40px 32px;">
            <div class="editorial-label mb-3">
              Valeur totale
            </div>
            <div style="font-family:var(--font-serif);font-size:1.8rem;font-weight:700;line-height:1.2;">
              {{ totalValue.toLocaleString() }} 🪙
            </div>
          </div>
        </div>
      </div>

      <!-- Inventaire vide -->
      <div
        v-if="inventory.length === 0"
        class="text-center py-16"
      >
        <div class="editorial-label mb-4">
          Aucun item
        </div>
        <div style="font-family:var(--font-serif);font-size:1.5rem;font-weight:700;">
          Inventaire vide
        </div>
        <div
          class="mt-2"
          style="color:var(--rpg-ink-muted);font-size:13px;"
        >
          Explorez le jeu pour obtenir des objets.
        </div>
      </div>

      <template v-else>
        <!-- ── ÉQUIPEMENT ACTIF ──────────────────────────────────────── -->
        <div
          v-if="equipped.length > 0"
          style="margin-bottom:40px;"
        >
          <div class="editorial-label mb-4">
            ■ Équipement actif
          </div>
          <v-row
            no-gutters
            style="border-top:1px solid rgba(0,0,0,0.08);"
          >
            <v-col
              v-for="item in equipped"
              :key="item.id"
              cols="12"
              sm="6"
              md="3"
              style="border-right:1px solid rgba(0,0,0,0.08); padding:20px 20px 20px 0; margin-right:20px;"
            >
              <div style="font-size:10px;font-weight:600;letter-spacing:.15em;text-transform:uppercase;color:var(--rpg-ink-muted);margin-bottom:8px;">
                {{ item.item.type }}
              </div>
              <div style="font-family:var(--font-serif);font-size:1.1rem;font-weight:700;margin-bottom:6px;">
                {{ item.item.name }}
              </div>
              <div
                v-if="item.item.statModifiers"
                style="font-size:12px;color:var(--rpg-ink-muted);"
              >
                <span
                  v-for="(value, stat) in parsedMods(item.item.statModifiers)"
                  :key="stat"
                  style="margin-right:8px;"
                  :style="Number(value) >= 0 ? 'color:#1E8449;' : 'color:#C0392B;'"
                >
                  {{ statLabel(String(stat)) }} {{ Number(value) >= 0 ? '+' : '' }}{{ value }}
                </span>
              </div>
            </v-col>
          </v-row>
        </div>

        <!-- ── CONSOMMABLES ──────────────────────────────────────────── -->
        <div
          v-if="consumables.length > 0"
          style="margin-bottom:40px;"
        >
          <div class="editorial-label mb-4">
            ■ Consommables
          </div>

          <!-- Header -->
          <div style="border-bottom:1px solid rgba(0,0,0,0.08);padding-bottom:10px;margin-bottom:0;">
            <v-row no-gutters>
              <v-col cols="4">
                <div class="editorial-label">
                  Item
                </div>
              </v-col>
              <v-col cols="5">
                <div class="editorial-label">
                  Description
                </div>
              </v-col>
              <v-col cols="2">
                <div class="editorial-label">
                  Effet
                </div>
              </v-col>
              <v-col
                cols="1"
                class="text-right"
              >
                <div class="editorial-label">
                  Qté
                </div>
              </v-col>
            </v-row>
          </div>

          <div
            v-for="item in consumables"
            :key="item.id"
            class="editorial-row"
            style="padding:14px 0;"
          >
            <v-row
              no-gutters
              align="center"
            >
              <v-col cols="4">
                <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">
                  {{ item.item.name }}
                </div>
              </v-col>
              <v-col cols="5">
                <div style="font-size:13px;color:var(--rpg-ink-muted);font-style:italic;">
                  {{ item.item.description }}
                </div>
              </v-col>
              <v-col cols="2">
                <div
                  v-if="item.item.effectValue"
                  style="font-size:13px;color:#1E8449;font-weight:600;"
                >
                  +{{ item.item.effectValue }}
                </div>
              </v-col>
              <v-col
                cols="1"
                class="text-right"
              >
                <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">
                  ×{{ item.quantity }}
                </div>
              </v-col>
            </v-row>
          </div>
        </div>

        <!-- ── AUTRES ITEMS ───────────────────────────────────────────── -->
        <div v-if="others.length > 0">
          <div class="editorial-label mb-4">
            ■ Autres objets
          </div>
          <div style="border-bottom:1px solid rgba(0,0,0,0.08);padding-bottom:10px;">
            <v-row no-gutters>
              <v-col cols="4">
                <div class="editorial-label">
                  Item
                </div>
              </v-col>
              <v-col cols="3">
                <div class="editorial-label">
                  Type
                </div>
              </v-col>
              <v-col cols="3">
                <div class="editorial-label">
                  Prix unitaire
                </div>
              </v-col>
              <v-col
                cols="2"
                class="text-right"
              >
                <div class="editorial-label">
                  Qté
                </div>
              </v-col>
            </v-row>
          </div>
          <div
            v-for="item in others"
            :key="item.id"
            class="editorial-row"
            style="padding:14px 0;"
          >
            <v-row
              no-gutters
              align="center"
            >
              <v-col cols="4">
                <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">
                  {{ item.item.name }}
                </div>
                <div
                  v-if="item.item.statModifiers"
                  style="font-size:12px;margin-top:2px;"
                >
                  <span
                    v-for="(value, stat) in parsedMods(item.item.statModifiers)"
                    :key="stat"
                    style="margin-right:8px;"
                    :style="Number(value) >= 0 ? 'color:#1E8449;' : 'color:#C0392B;'"
                  >
                    {{ statLabel(String(stat)) }} {{ Number(value) >= 0 ? '+' : '' }}{{ value }}
                  </span>
                </div>
              </v-col>
              <v-col cols="3">
                <div style="font-size:13px;color:var(--rpg-ink-muted);text-transform:capitalize;">
                  {{ item.item.type }}
                </div>
              </v-col>
              <v-col cols="3">
                <div style="font-size:13px;color:var(--rpg-ink-muted);">
                  {{ item.item.price }} 🪙
                </div>
              </v-col>
              <v-col
                cols="2"
                class="text-right"
              >
                <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;">
                  ×{{ item.quantity }}
                </div>
              </v-col>
            </v-row>
          </div>
        </div>
      </template>
    </template>
  </div>
</template>

<script setup lang="ts">
import { inventoryApi } from '@/api/inventory'
import type { PlayerInventory } from '@/interfaces/playerInventory'
import { computed, onMounted, ref } from 'vue'

const inventory = ref<PlayerInventory[]>([])
const loading   = ref(true)

const equipped    = computed(() => inventory.value.filter(i => i.isEquipped))
const consumables = computed(() => inventory.value.filter(i => !i.isEquipped && i.item.type === 'consumable'))
const others      = computed(() => inventory.value.filter(i => !i.isEquipped && i.item.type !== 'consumable'))

const equippedCount = computed(() => equipped.value.length)
const totalValue    = computed(() => inventory.value.reduce((sum, i) => sum + i.item.price * i.quantity, 0))

onMounted(async () => {
  try {
    const res = await inventoryApi.getMe()
    inventory.value = res.data.items ?? []
  } finally {
    loading.value = false
  }
})

function statLabel(stat: string): string {
  const map: Record<string, string> = {
    strength: 'Force', intelligence: 'Int.', speed: 'Vitesse',
    maxHP: 'HP max', maxMP: 'MP max',
  }
  return map[stat] ?? stat
}

function parsedMods(raw: string): Record<string, number> {
  try { return JSON.parse(raw) }
  catch { return {} }
}
</script>