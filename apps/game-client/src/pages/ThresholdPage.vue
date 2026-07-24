<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import { useRunStore } from '../features/runs/stores/runStore';

const router   = useRouter();
const runStore = useRunStore();

const resumableRun = computed(() => runStore.resumableRun);

onMounted(() => { runStore.loadResumableRun(); });

async function resumeRun() {
  const run = resumableRun.value;
  if (!run) return;
  await runStore.loadRun(run.id);
  if (runStore.currentRun?.id) await router.push(`/run/${run.id}`);
}

async function startRun() {
  await runStore.startRun();
  const runId = runStore.currentRun?.id;
  if (!runId) return;
  await router.push(`/run/${runId}`);
}

function formatSavedAt(savedAt: string): string {
  try {
    return new Intl.DateTimeFormat('fr-FR', {
      day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit',
    }).format(new Date(savedAt));
  } catch { return '—'; }
}

// ── UI accordéons rubans ──────────────────────────────────────────────────
type PanelId = 'reprise' | 'neuf' | null;
const openPanel  = ref<PanelId>('reprise');
const showConfirm = ref<'reprise' | 'neuf' | null>(null);

function togglePanel(id: PanelId) {
  openPanel.value = openPanel.value === id ? null : id;
}
const isReprise = computed(() => openPanel.value === 'reprise');
const isNeuf    = computed(() => openPanel.value === 'neuf');
</script>

<template>
  <main class="threshold-screen">
    <!-- Atmosphère -->
    <div class="es-atmos" />
    <div class="es-vignette" />
    <div class="es-grain" />

    <div class="threshold-content">

      <!-- En-tête -->
      <span class="es-kicker" style="color: var(--ink-4)">Seuil du Palais</span>

      <!-- Filet losange -->
      <div class="es-rule threshold-rule">
        <span class="es-lozenge" />
      </div>

      <h1 class="es-h1 threshold-title">L'ÉPOPÉE DES SILENCES</h1>
      <p class="es-lede threshold-tagline">
        Deux portes au seuil. L'une retient ta descente&nbsp;; l'autre l'efface pour mieux la réécrire.
      </p>

      <!-- Diptyque-rubans -->
      <div class="threshold-ribbons">

        <!-- ── Ruban Reprise ── -->
        <div
          :class="['ribbon', 'es-panel', isReprise && 'ribbon--open ribbon--gold']"
          :style="isReprise ? { borderColor: 'var(--gold)' } : {}"
        >
          <div class="ribbon__head" @click="togglePanel('reprise')">
            <div class="ribbon__head-left">
              <div class="ribbon__node" :style="{ borderColor: 'var(--gold)', color: 'var(--gold)' }">
                <!-- Sigil seuil -->
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round">
                  <path d="M5 20.5 V11 a7 7 0 0 1 14 0 V20.5" />
                </svg>
              </div>
              <div>
                <span class="ribbon__chip ribbon__chip--gold">Run en cours</span>
                <h3
                  class="es-h3 ribbon__heading"
                  :style="{ color: isReprise ? 'var(--ink)' : 'var(--ink-2)' }"
                >Reprendre la descente</h3>
              </div>
            </div>
            <div class="ribbon__head-right">
              <span
                v-if="resumableRun"
                class="es-mono ribbon__seed"
                style="color: var(--gold)"
              >{{ resumableRun.seed }}</span>
              <span v-else-if="runStore.isLoadingResumableRun" class="es-label">Vérification…</span>
              <span v-else class="es-label" style="color: var(--ink-4)">Aucune run</span>
              <svg
                :style="{ transform: isReprise ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform .25s', color: 'var(--gold)' }"
                width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
              ><polyline points="6 9 12 15 18 9" /></svg>
            </div>
          </div>

          <!-- Corps : affiché seulement si une run est disponible -->
          <div class="ribbon__body" :style="{ maxHeight: isReprise ? '320px' : '0', opacity: isReprise ? 1 : 0 }">
            <div class="ribbon__body-inner">
              <div class="ribbon__divider" />

              <template v-if="resumableRun">
                <div class="ribbon__stats">
                  <div class="ribbon__stat">
                    <span class="es-label" style="color: var(--ink-4)">Seed</span>
                    <span class="es-mono ribbon__stat-value">{{ resumableRun.seed }}</span>
                  </div>
                  <div class="ribbon__stat">
                    <span class="es-label" style="color: var(--ink-4)">Salle</span>
                    <span class="es-mono ribbon__stat-value">{{ resumableRun.currentRoomNumber }}</span>
                  </div>
                  <div class="ribbon__stat">
                    <span class="es-label" style="color: var(--ink-4)">Sauvegardée</span>
                    <span class="ribbon__stat-value" style="font-size: 13px; color: var(--ink-2)">
                      {{ formatSavedAt(resumableRun.savedAt) }}
                    </span>
                  </div>
                </div>
                <button
                  class="es-btn ribbon__btn"
                  style="border-color: var(--gold); color: var(--gold)"
                  :disabled="runStore.isLoading"
                  @click.stop="showConfirm = 'reprise'"
                >
                  {{ runStore.isLoading ? 'Chargement…' : 'Reprendre →' }}
                </button>
              </template>

              <template v-else-if="runStore.isLoadingResumableRun">
                <span class="es-label" style="color: var(--ink-4)">Recherche d'une run en cours…</span>
              </template>

              <template v-else>
                <p class="es-body" style="color: var(--ink-3); font-size: 14px; margin: 0 0 18px">
                  Aucune descente suspendue. Lance une nouvelle run pour commencer.
                </p>
                <button class="es-btn" disabled style="opacity: 0.3">Reprendre →</button>
              </template>
            </div>
          </div>
        </div>

        <!-- ── Ruban Nouveau seuil ── -->
        <div
          :class="['ribbon', 'es-panel', isNeuf && 'ribbon--open ribbon--frost']"
          :style="isNeuf ? { borderColor: 'var(--frost)' } : {}"
        >
          <div class="ribbon__head" @click="togglePanel('neuf')">
            <div class="ribbon__head-left">
              <div class="ribbon__node" :style="{ borderColor: 'var(--frost)', color: 'var(--frost)' }">
                <!-- Sigil étoile -->
                <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" stroke="none">
                  <path d="M12 2.5 L13.7 10.3 L21.5 12 L13.7 13.7 L12 21.5 L10.3 13.7 L2.5 12 L10.3 10.3 Z" />
                </svg>
              </div>
              <div>
                <span class="ribbon__chip ribbon__chip--frost">Seed inédite</span>
                <h3
                  class="es-h3 ribbon__heading"
                  :style="{ color: isNeuf ? 'var(--ink)' : 'var(--ink-2)' }"
                >Franchir un nouveau seuil</h3>
              </div>
            </div>
            <div class="ribbon__head-right">
              <span class="es-mono ribbon__seed" style="color: var(--frost)">à générer</span>
              <svg
                :style="{ transform: isNeuf ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform .25s', color: 'var(--frost)' }"
                width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"
              ><polyline points="6 9 12 15 18 9" /></svg>
            </div>
          </div>

          <div class="ribbon__body" :style="{ maxHeight: isNeuf ? '320px' : '0', opacity: isNeuf ? 1 : 0 }">
            <div class="ribbon__body-inner">
              <div class="ribbon__divider" />
              <p class="es-body" style="font-size: 14px; margin: 0 0 16px; max-width: 540px">
                Une seed inédite réécrit l'architecture du Palais&nbsp;: pièces, lois, ennemis.
                Tu repars avec ton Tome — rien d'autre.
              </p>
              <div class="ribbon__stats" style="margin-bottom: 20px">
                <div class="ribbon__stat">
                  <span class="es-label" style="color: var(--ink-4)">Difficulté</span>
                  <span class="ribbon__stat-value" style="color: var(--frost)">Stable · 4 choix</span>
                </div>
                <div class="ribbon__stat">
                  <span class="es-label" style="color: var(--ink-4)">Seed</span>
                  <span class="ribbon__stat-value" style="color: var(--frost)">attribuée au départ</span>
                </div>
              </div>

              <button
                class="es-btn ribbon__btn"
                style="border-color: var(--frost); color: var(--frost); margin-top: 18px"
                :disabled="runStore.isLoading"
                @click.stop="showConfirm = 'neuf'"
              >
                {{ runStore.isLoading ? 'Génération…' : 'Générer une run →' }}
              </button>
            </div>
          </div>
        </div>
      </div><!-- /ribbons -->

      <!-- Elise -->
      <div class="threshold-elise">
        <div class="threshold-elise__name">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round">
            <circle cx="12" cy="12" r="8" /><circle cx="12" cy="12" r="3.2" />
          </svg>
          ELISE
          <span style="color: var(--ink-4); letter-spacing: 0.18em; margin-left: 4px">· au seuil</span>
        </div>
        <p class="threshold-elise__line">
          Te revoilà. Le Palais n'a rien oublié de la dernière fois — <em>moi non plus</em>.
        </p>
      </div>

      <p v-if="runStore.error" class="threshold-error">{{ runStore.error }}</p>
    </div><!-- /threshold-content -->

    <!-- ── Modale de confirmation ── -->
    <Teleport to="body">
      <Transition name="overlay">
        <div v-if="showConfirm" class="confirm-backdrop" @click.self="showConfirm = null">
          <div class="confirm-dialog" @click.stop>

            <!-- Reprendre -->
            <template v-if="showConfirm === 'reprise' && resumableRun">
              <span class="ribbon__chip ribbon__chip--gold" style="margin-bottom: 14px; display: inline-flex">
                Reprise
              </span>
              <h2 class="es-h2" style="margin-bottom: 14px">Reprendre la descente&nbsp;?</h2>
              <p class="es-body" style="margin-bottom: 22px">
                Tu replonges depuis la salle
                <strong>{{ resumableRun.currentRoomNumber }}</strong>,
                seed <span class="es-mono" style="color: var(--gold); font-size: 13px">{{ resumableRun.seed }}</span>.
                Sauvegarde du {{ formatSavedAt(resumableRun.savedAt) }}.
              </p>
              <div style="display: flex; gap: 10px">
                <button class="es-btn es-btn--ghost" style="flex: 1" @click="showConfirm = null">
                  Pas encore
                </button>
                <button
                  class="es-btn"
                  style="flex: 1.5; border-color: var(--gold); color: var(--gold)"
                  :disabled="runStore.isLoading"
                  @click="resumeRun()"
                >
                  {{ runStore.isLoading ? 'Chargement…' : 'Reprendre →' }}
                </button>
              </div>
            </template>

            <!-- Nouveau seuil -->
            <template v-else-if="showConfirm === 'neuf'">
              <span class="ribbon__chip ribbon__chip--frost" style="margin-bottom: 14px; display: inline-flex">
                Nouveau seuil
              </span>
              <h2 class="es-h2" style="margin-bottom: 14px">Effacer pour réécrire&nbsp;?</h2>
              <p class="es-body" style="margin-bottom: 22px">
                Une nouvelle architecture s'écrira sous tes pas. Tu repars avec ton Tome — rien d'autre.
              </p>
              <div style="display: flex; gap: 10px">
                <button class="es-btn es-btn--ghost" style="flex: 1" @click="showConfirm = null">
                  Annuler
                </button>
                <button
                  class="es-btn"
                  style="flex: 1.5; border-color: var(--frost); color: var(--frost)"
                  :disabled="runStore.isLoading"
                  @click="startRun()"
                >
                  {{ runStore.isLoading ? 'Génération…' : 'Générer une run →' }}
                </button>
              </div>
            </template>
          </div>
        </div>
      </Transition>
    </Teleport>
  </main>
</template>

<style scoped>
/* ── Écran ── */
.threshold-screen {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100dvh;
  overflow: hidden;
  background:
    radial-gradient(70% 52% at 20% 12%, var(--wash-frost), transparent 60%),
    radial-gradient(64% 56% at 86% 80%, var(--wash-blood), transparent 58%),
    radial-gradient(58% 50% at 60% 26%, var(--wash-sap),   transparent 60%),
    radial-gradient(56% 50% at 12% 92%, var(--wash-gold),  transparent 60%),
    radial-gradient(150% 130% at 50% -10%, oklch(0.310 0.058 272) 0%, var(--bg) 48%, var(--void) 100%);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

/* ── Contenu centré ── */
.threshold-content {
  position: relative;
  z-index: 5;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: var(--space-8) var(--space-6);
  width: min(860px, 96vw);
}

/* ── En-tête ── */
.threshold-rule {
  width: 200px;
  margin: 18px 0 16px;
}

.threshold-title {
  text-align: center;
  margin-bottom: 8px;
}

.threshold-tagline {
  margin-bottom: 40px;
  text-align: center;
  max-width: 560px;
  color: var(--ink-3);
  font-style: italic;
}

/* ── Rubans ── */
.threshold-ribbons {
  display: flex;
  flex-direction: column;
  gap: 14px;
  width: 100%;
}

.ribbon {
  overflow: hidden;
  transition: border-color .25s;
  padding: 0;
}

.ribbon__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 24px;
  cursor: pointer;
  user-select: none;
}

.ribbon__head-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.ribbon__head-right {
  display: flex;
  align-items: center;
  gap: 14px;
}

.ribbon__node {
  width: 46px;
  height: 46px;
  border-radius: 50%;
  border: 1px solid;
  background: radial-gradient(circle at 50% 34%, var(--raise), oklch(0.20 0.026 270));
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.ribbon__chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 3px 9px;
  border-radius: 3px;
  font-family: var(--font-caps);
  font-size: 11px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  border: 1px solid var(--line);
  background: oklch(0.32 0.028 268 / 0.5);
  color: var(--ink-2);
  margin-bottom: 6px;
}
.ribbon__chip--gold  { color: oklch(0.90 0.11 86);  border-color: var(--gold-dim);  background: oklch(0.56 0.12 84 / 0.26); }
.ribbon__chip--frost { color: oklch(0.90 0.11 276); border-color: var(--frost-dim); background: oklch(0.56 0.12 276 / 0.26); }

.ribbon__heading {
  margin: 0;
  transition: color .2s;
}

.ribbon__seed {
  font-size: 13px;
}

/* corps expansible */
.ribbon__body {
  overflow: hidden;
  transition: max-height .32s ease, opacity .24s ease;
}

.ribbon__body-inner {
  padding: 0 24px 22px;
}

.ribbon__divider {
  height: 2px;
  margin-bottom: 18px;
  filter: blur(0.4px);
  background: linear-gradient(90deg, transparent, var(--line-strong) 18%, var(--line-strong) 82%, transparent);
}

.ribbon__stats {
  display: flex;
  gap: 28px;
  margin-bottom: 18px;
}

.ribbon__stat {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.ribbon__stat-value {
  font-size: 13px;
  color: var(--ink);
  font-family: var(--font-mono);
}

.ribbon__btn {
  font-family: var(--font-caps);
}

/* ── Elise ── */
.threshold-elise {
  position: relative;
  margin-top: 32px;
  width: 100%;
  padding: 16px 20px 16px 22px;
  border-radius: 4px;
  background: linear-gradient(90deg, oklch(0.40 0.08 268 / 0.42), oklch(0.26 0.028 268 / 0.6));
  border: 1px solid var(--frost-dim);
  border-left-width: 3px;
  border-left-color: var(--frost);
  box-shadow: 0 0 60px -18px oklch(0.70 0.10 268 / 0.5);
  backdrop-filter: blur(3px);
}

.threshold-elise__name {
  display: flex;
  align-items: center;
  gap: 8px;
  font-family: var(--font-caps);
  font-size: 11px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: var(--frost);
  margin-bottom: 7px;
}

.threshold-elise__line {
  font-size: 15px;
  line-height: 1.56;
  color: var(--ink);
  font-style: italic;
  margin: 0;
}

.threshold-elise__line em {
  color: var(--frost);
  font-style: normal;
  font-weight: 500;
}

/* ── Erreur ── */
.threshold-error {
  color: var(--blood);
  font-size: 0.78rem;
  margin-top: var(--space-3);
}

/* ── Modale ── */
.confirm-backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.14 0.030 272 / 0.72);
  backdrop-filter: blur(6px);
}

.confirm-dialog {
  position: relative;
  width: min(500px, 92vw);
  padding: 36px 38px;
  border-radius: 5px;
  background: linear-gradient(180deg, var(--raise), var(--panel));
  border: 1px solid var(--frost-dim);
  box-shadow:
    0 30px 66px -30px oklch(0.10 0.03 272 / 0.7),
    0 12px 30px -18px oklch(0.10 0.03 272 / 0.5),
    0 0 64px -22px var(--wash-frost);
}

.overlay-enter-active,
.overlay-leave-active { transition: opacity .2s ease; }
.overlay-enter-from,
.overlay-leave-to     { opacity: 0; }
</style>
