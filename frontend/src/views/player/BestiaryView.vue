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
      <!-- ── HERO éditorial ─────────────────────────────────────────── -->
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
              Mes données · Créatures
            </div>
            <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,3rem);font-weight:900;line-height:1;letter-spacing:-0.03em;margin-bottom:12px;">
              Bestiaire
              <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">&amp; créatures.</span>
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Entrées
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ bestiary.length }}
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Boss vaincus
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ bossCount }}
            </div>
          </div>
          <div style="padding:40px 32px;">
            <div class="editorial-label mb-3">
              Créature la plus forte
            </div>
            <div style="font-family:var(--font-serif);font-size:1.6rem;font-weight:700;line-height:1.2;">
              {{ rarest?.enemy.name ?? '—' }}
            </div>
          </div>
        </div>
      </div>

      <!-- Vide -->
      <div
        v-if="bestiary.length === 0"
        class="text-center py-16"
      >
        <div class="editorial-label mb-4">
          Aucune entrée
        </div>
        <div style="font-family:var(--font-serif);font-size:1.5rem;font-weight:700;">
          Bestiaire vide
        </div>
        <div
          class="mt-2"
          style="color:var(--rpg-ink-muted);font-size:13px;"
        >
          Combattez des ennemis pour remplir votre bestiaire.
        </div>
      </div>

      <template v-else>
        <!-- ── PANNEAU DÉTAIL + LISTE ───────────────────────────────── -->
        <v-row
          no-gutters
          style="align-items: flex-start;"
        >
          <!-- Panneau détail -->
          <v-col
            v-if="selected"
            cols="12"
            md="4"
            style="
              border-right: 1px solid var(--rpg-border);
              padding-right: 32px; margin-right: 32px;
              position: sticky; top: 80px;
            "
          >
            <div class="editorial-label mb-3">
              Entrée n° {{ String(selectedIndex + 1).padStart(2, '0') }} · {{ selected.enemy.type.toUpperCase() }}
            </div>

            <!-- Artwork placeholder -->
            <div
              style="
              width:100%; aspect-ratio:4/3;
              background: var(--rpg-cream-dark);
              border: 1px solid var(--rpg-border);
              margin-bottom:20px;
              display:flex; align-items:flex-end; padding:16px;
              position:relative; overflow:hidden;
            "
            >
              <div
                style="
                position:absolute;inset:0;
                background: repeating-linear-gradient(
                  45deg,
                  rgba(0,0,0,0.02) 0px, rgba(0,0,0,0.02) 1px,
                  transparent 1px, transparent 12px
                );
              "
              />
              <div style="position:relative;z-index:1;">
                <div class="editorial-label mb-1">
                  Croquis · {{ selected.enemy.name.toUpperCase() }}
                </div>
                <div style="font-size:10px;color:var(--rpg-ink-muted);">
                  Artwork non disponible
                </div>
              </div>
            </div>

            <div style="font-family:var(--font-serif);font-size:1.8rem;font-weight:700;margin-bottom:8px;line-height:1.1;">
              {{ selected.enemy.name }}
            </div>

            <div
              v-if="selected.enemy.description"
              style="font-size:13px;color:var(--rpg-ink-muted);font-style:italic;margin-bottom:20px;line-height:1.6;"
            >
              « {{ selected.enemy.description }} »
            </div>

            <!-- Stats -->
            <div style="border-top:1px solid var(--rpg-border);padding-top:16px;margin-bottom:16px;">
              <v-row no-gutters>
                <v-col
                  cols="6"
                  style="padding-right:12px;padding-bottom:16px;"
                >
                  <div class="editorial-label mb-1">
                    HP max
                  </div>
                  <div style="font-family:var(--font-serif);font-size:1.3rem;font-weight:700;">
                    {{ selected.enemy.maxHP }}
                  </div>
                </v-col>
                <v-col
                  cols="6"
                  style="padding-bottom:16px;"
                >
                  <div class="editorial-label mb-1">
                    Force
                  </div>
                  <div style="font-family:var(--font-serif);font-size:1.3rem;font-weight:700;">
                    {{ selected.enemy.strength }}
                  </div>
                </v-col>
                <v-col
                  cols="6"
                  style="padding-right:12px;"
                >
                  <div class="editorial-label mb-1">
                    XP
                  </div>
                  <div style="font-family:var(--font-serif);font-size:1.3rem;font-weight:700;">
                    {{ selected.enemy.experienceReward }}
                  </div>
                </v-col>
                <v-col cols="6">
                  <div class="editorial-label mb-1">
                    Or
                  </div>
                  <div style="font-family:var(--font-serif);font-size:1.3rem;font-weight:700;">
                    {{ selected.enemy.goldReward }} 🪙
                  </div>
                </v-col>
              </v-row>
            </div>

            <!-- Résistances -->
            <div style="border-top:1px solid var(--rpg-border);padding-top:16px;">
              <div class="editorial-label mb-3">
                Résistances
              </div>
              <div style="margin-bottom:10px;">
                <div
                  class="d-flex justify-space-between"
                  style="font-size:12px;margin-bottom:4px;"
                >
                  <span style="color:var(--rpg-ink-muted);">Physique</span>
                  <span style="font-weight:600;">{{ (selected.enemy.physicalResistance * 100).toFixed(0) }}%</span>
                </div>
                <div style="height:2px;background:rgba(0,0,0,0.08);">
                  <div :style="`width:${Math.min(selected.enemy.physicalResistance * 50, 100)}%;height:100%;background:var(--rpg-ink);`" />
                </div>
              </div>
              <div>
                <div
                  class="d-flex justify-space-between"
                  style="font-size:12px;margin-bottom:4px;"
                >
                  <span style="color:var(--rpg-ink-muted);">Magique</span>
                  <span style="font-weight:600;">{{ (selected.enemy.magicalResistance * 100).toFixed(0) }}%</span>
                </div>
                <div style="height:2px;background:rgba(0,0,0,0.08);">
                  <div :style="`width:${Math.min(selected.enemy.magicalResistance * 50, 100)}%;height:100%;background:#2D4A8A;`" />
                </div>
              </div>
            </div>

            <div
              class="editorial-label mt-4"
              style="cursor:pointer;color:var(--rpg-ink-muted);"
              @click="selected = null"
            >
              Fermer ×
            </div>
          </v-col>

          <!-- Liste -->
          <v-col>
            <!-- Header tableau -->
            <div style="border-bottom:1px solid var(--rpg-border);padding-bottom:12px;margin-bottom:0;">
              <v-row no-gutters>
                <v-col cols="1">
                  <div class="editorial-label">
                    #
                  </div>
                </v-col>
                <v-col cols="4">
                  <div class="editorial-label">
                    Créature
                  </div>
                </v-col>
                <v-col cols="2">
                  <div class="editorial-label">
                    Catégorie
                  </div>
                </v-col>
                <v-col cols="2">
                  <div class="editorial-label">
                    Menace
                  </div>
                </v-col>
                <v-col cols="2">
                  <div class="editorial-label">
                    Récompense
                  </div>
                </v-col>
                <v-col
                  cols="1"
                  class="text-right"
                >
                  <div class="editorial-label">
                    Statut
                  </div>
                </v-col>
              </v-row>
            </div>

            <div
              v-for="(entry, i) in bestiary"
              :key="entry.id"
              class="editorial-row"
              style="padding:18px 0; cursor:pointer;"
              :style="selected?.id === entry.id ? 'background:rgba(0,0,0,0.02);' : ''"
              @click="selectEntry(entry, i)"
            >
              <v-row
                no-gutters
                align="center"
              >
                <v-col cols="1">
                  <div style="font-size:11px;font-weight:600;color:var(--rpg-ink-muted);">
                    {{ String(i + 1).padStart(2, '0') }}
                  </div>
                </v-col>

                <v-col cols="4">
                  <div style="font-family:var(--font-serif);font-size:1rem;font-weight:700;margin-bottom:2px;">
                    {{ entry.enemy.name }}
                  </div>
                  <div style="font-size:11px;color:var(--rpg-ink-muted);font-style:italic;">
                    {{ entry.enemy.description?.substring(0, 40) }}{{ (entry.enemy.description?.length ?? 0) > 40 ? '…' : '' }}
                  </div>
                </v-col>

                <v-col cols="2">
                  <div
                    style="font-size:10px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;"
                    :style="`color:${typeColor(entry.enemy.type)};`"
                  >
                    {{ entry.enemy.type }}
                  </div>
                </v-col>

                <v-col cols="2">
                  <div style="height:2px;background:rgba(0,0,0,0.08);width:80px;">
                    <div :style="`width:${Math.min((entry.enemy.maxHP / 600) * 100, 100)}%;height:100%;background:${typeColor(entry.enemy.type)};`" />
                  </div>
                </v-col>

                <v-col cols="2">
                  <div style="font-size:12px;color:var(--rpg-ink-muted);">
                    {{ entry.enemy.experienceReward }} XP · {{ entry.enemy.goldReward }} 🪙
                  </div>
                </v-col>

                <v-col
                  cols="1"
                  class="text-right"
                >
                  <div style="font-size:10px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#1E8449;">
                    Vaincu
                  </div>
                </v-col>
              </v-row>
            </div>
          </v-col>
        </v-row>
      </template>
    </template>
  </div>
</template>

<script setup lang="ts">
import { bestiaryApi } from '@/api/bestiary'
import type { BestiaryUnlock } from '@/interfaces/bestiary'
import { computed, onMounted, ref } from 'vue'

const bestiary      = ref<BestiaryUnlock[]>([])
const loading       = ref(true)
const selected      = ref<BestiaryUnlock | null>(null)
const selectedIndex = ref(0)

const bossCount = computed(() =>
  bestiary.value.filter(b => b.enemy.type === 'boss').length
)

const rarest = computed(() =>
  bestiary.value.length > 0
    ? [...bestiary.value].sort((a, b) => b.enemy.maxHP - a.enemy.maxHP)[0]
    : null
)

onMounted(async () => {
  try {
    const res = await bestiaryApi.getMe()
    bestiary.value = res.data.items ?? []
  } catch {
    bestiary.value = []
  } finally {
    loading.value = false
  }
})

function selectEntry(entry: BestiaryUnlock, index: number) {
  if (selected.value?.id === entry.id) {
    selected.value = null
  } else {
    selected.value      = entry
    selectedIndex.value = index
  }
}
function typeColor(type: string): string {
  const map: Record<string, string> = {
    basic:    'var(--rpg-ink-muted)',
    miniboss: '#D68910',
    boss:     '#C0392B',
  }
  return map[type] ?? 'var(--rpg-ink-muted)'
}
console.log(bestiary)
</script>