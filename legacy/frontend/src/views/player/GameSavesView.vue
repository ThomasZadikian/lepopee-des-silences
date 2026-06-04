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
              Mes données · Progression
            </div>
            <div style="font-family:var(--font-serif);font-size:clamp(2rem,3vw,3rem);font-weight:900;line-height:1;letter-spacing:-0.03em;margin-bottom:12px;">
              Sauvegardes
              <span style="font-style:italic;color:var(--rpg-ink-muted);display:block;">&amp; parties.</span>
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Total sauvegardes
            </div>
            <div style="font-family:var(--font-serif);font-size:2.5rem;font-weight:700;line-height:1;">
              {{ saves.length }}
            </div>
          </div>
          <div style="padding:40px 32px;border-right:1px solid var(--rpg-border);">
            <div class="editorial-label mb-3">
              Dernière zone
            </div>
            <div style="font-family:var(--font-serif);font-size:1.8rem;font-weight:700;line-height:1.2;">
              {{ lastSave?.currentZone ?? '—' }}
            </div>
          </div>
          <div style="padding:40px 32px;">
            <div class="editorial-label mb-3">
              Dernière sync
            </div>
            <div style="font-family:var(--font-serif);font-size:1.4rem;font-weight:700;line-height:1.2;">
              {{ lastSave ? new Date(lastSave.savedAt).toLocaleDateString('fr-FR') : '—' }}
            </div>
          </div>
        </div>
      </div>

      <!-- Aucune sauvegarde -->
      <div
        v-if="saves.length === 0"
        class="text-center py-16"
      >
        <div class="editorial-label mb-4">
          Aucune donnée
        </div>
        <div style="font-family: var(--font-serif); font-size: 1.5rem; font-weight: 700;">
          Aucune sauvegarde trouvée
        </div>
        <div
          class="mt-2"
          style="color: var(--rpg-ink-muted); font-size: 13px;"
        >
          Lancez le jeu pour créer votre première sauvegarde.
        </div>
      </div>

      <!-- ── LISTE DES SAUVEGARDES ──────────────────────────────────── -->
      <template v-else>
        <!-- Header tableau -->
        <div style="border-bottom: 1px solid rgba(0,0,0,0.08); padding-bottom: 12px; margin-bottom: 0;">
          <v-row no-gutters>
            <v-col cols="1">
              <div class="editorial-label">
                #
              </div>
            </v-col>
            <v-col cols="3">
              <div class="editorial-label">
                Zone
              </div>
            </v-col>
            <v-col cols="3">
              <div class="editorial-label">
                Position
              </div>
            </v-col>
            <v-col cols="3">
              <div class="editorial-label">
                Sauvegardé le
              </div>
            </v-col>
            <v-col
              cols="2"
              class="text-right"
            >
              <div class="editorial-label">
                Action
              </div>
            </v-col>
          </v-row>
        </div>

        <!-- Lignes -->
        <div
          v-for="(save, i) in saves"
          :key="save.id"
          class="editorial-row"
          style="padding: 20px 0;"
        >
          <v-row
            no-gutters
            align="center"
          >
            <v-col cols="1">
              <div
                style="
                font-size: 11px; font-weight: 600;
                color: var(--rpg-ink-muted);
                font-family: var(--font-sans);
              "
              >
                {{ String(i + 1).padStart(2, '0') }}
              </div>
            </v-col>

            <v-col cols="3">
              <div style="font-family: var(--font-serif); font-size: 1.1rem; font-weight: 700;">
                {{ save.currentZone }}
              </div>
            </v-col>

            <v-col cols="3">
              <div style="font-size: 13px; color: var(--rpg-ink-muted);">
                {{ save.positionX.toFixed(1) }}, {{ save.positionY.toFixed(1) }}
              </div>
            </v-col>

            <v-col cols="3">
              <div style="font-size: 13px; color: var(--rpg-ink-muted);">
                {{ new Date(save.savedAt).toLocaleString('fr-FR') }}
              </div>
            </v-col>

            <v-col
              cols="2"
              class="text-right"
            >
              <span
                style="
                  font-size: 11px; font-weight: 600; letter-spacing: 0.08em;
                  text-transform: uppercase; color: #C0392B;
                  cursor: pointer;
                  transition: opacity 0.15s;
                "
                @mouseenter="e => (e.currentTarget as HTMLElement).style.opacity = '0.6'"
                @mouseleave="e => (e.currentTarget as HTMLElement).style.opacity = '1'"
                @click="confirmDelete(save)"
              >
                Supprimer
              </span>
            </v-col>
          </v-row>
        </div>
      </template>
    </template>

    <!-- ── Dialogue confirmation ──────────────────────────────────── -->
    <v-dialog
      v-model="dialog.show"
      max-width="420"
    >
      <div
        style="
        background: var(--rpg-cream);
        border: 1px solid rgba(0,0,0,0.1);
        padding: 32px;
      "
      >
        <div class="editorial-label mb-3">
          Confirmer la suppression
        </div>
        <div style="font-family: var(--font-serif); font-size: 1.3rem; font-weight: 700; margin-bottom: 12px;">
          Supprimer cette sauvegarde ?
        </div>
        <div style="color: var(--rpg-ink-muted); font-size: 13px; margin-bottom: 28px;">
          Zone <strong>{{ dialog.save?.currentZone }}</strong> — cette action est irréversible.
        </div>
        <div class="d-flex justify-end ga-3">
          <span
            style="font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; cursor: pointer; padding: 8px 0; color: var(--rpg-ink-muted);"
            @click="dialog.show = false"
          >
            Annuler
          </span>
          <span
            style="font-size: 11px; font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; cursor: pointer; padding: 8px 16px; background: #1A1A1A; color: white;"
            @click="doDelete"
          >
            Confirmer →
          </span>
        </div>
      </div>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { gameSavesApi } from '@/api/gameSave'
import type { GameSave } from '@/interfaces/gameSave'
import { computed, onMounted, reactive, ref } from 'vue'

const saves   = ref<GameSave[]>([])
const loading = ref(true)

const dialog = reactive<{ show: boolean; save: GameSave | null }>({
  show: false,
  save: null,
})

const lastSave = computed(() =>
  saves.value.length > 0
    ? [...saves.value].sort((a, b) =>
        new Date(b.savedAt).getTime() - new Date(a.savedAt).getTime()
      )[0]
    : null
)

onMounted(async () => {
  try {
    const res = await gameSavesApi.getMe()
    saves.value = res.data.items ?? []
  } finally {
    loading.value = false
  }
})

function confirmDelete(save: GameSave) {
  dialog.save = save
  dialog.show = true
}

async function doDelete() {
  if (!dialog.save) return
  try {
    await gameSavesApi.delete(dialog.save.id)
    saves.value = saves.value.filter(s => s.id !== dialog.save!.id)
  } catch {
    // erreur silencieuse
  } finally {
    dialog.show = false
  }
}
</script>