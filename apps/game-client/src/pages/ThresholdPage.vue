<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

import { useRunStore } from '../features/runs/stores/runStore';

const router = useRouter();
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
</script>

<template>
  <main class="threshold">
    <div class="es-atmos">
      <div class="es-vignette" />
      <div class="es-grain" />
    </div>

    <div class="threshold__card">
      <!-- Top: icon + title -->
      <header class="threshold__header">
        <div class="threshold__icon">◈</div>
        <div>
          <h1 class="threshold__title">Seuil du Palais</h1>
          <p class="threshold__tagline">Chaque traversée s'écrit dans ton Tome — puis se tait.</p>
        </div>
      </header>

      <hr class="es-rule" />

      <!-- Description -->
      <div class="threshold__body">
        <p class="threshold__description">
          Le Palais se souvient de chaque pas. Les murs changent, les lois se renouvellent,
          les souvenirs se transforment — mais le Tome, lui, garde trace.
        </p>
      </div>

      <hr class="es-rule" />

      <!-- Actions -->
      <div class="threshold__actions">
        <!-- Resumable run -->
        <template v-if="runStore.isLoadingResumableRun">
          <span class="es-label">Vérification…</span>
        </template>

        <template v-else-if="resumableRun">
          <div class="threshold__run-stats">
            <div class="threshold__stat">
              <span class="es-label">Seed</span>
              <span class="threshold__stat-value">{{ resumableRun.seed }}</span>
            </div>
            <div class="threshold__stat">
              <span class="es-label">Salle</span>
              <span class="threshold__stat-value">{{ resumableRun.currentRoomNumber }}</span>
            </div>
            <div class="threshold__stat">
              <span class="es-label">Sauvegardée</span>
              <span class="threshold__stat-value">{{ formatSavedAt(resumableRun.savedAt) }}</span>
            </div>
          </div>
          <button class="es-btn es-btn--frost es-btn--lg" :disabled="runStore.isLoading" @click="resumeRun">
            {{ runStore.isLoading ? 'Chargement…' : 'Reprendre →' }}
          </button>
        </template>

        <template v-else>
          <span class="es-label">Aucune run suspendue</span>
          <button class="es-btn es-btn--lg" disabled>Reprendre →</button>
        </template>

        <!-- Divider -->
        <div class="threshold__divider" />

        <!-- New run -->
        <div class="threshold__stat">
          <span class="es-label">Nouveau seuil</span>
          <span class="threshold__stat-value">Seed inédite</span>
        </div>
        <button class="es-btn es-btn--gold es-btn--lg" :disabled="runStore.isLoading" @click="startRun">
          {{ runStore.isLoading ? 'Génération…' : 'Générer une run →' }}
        </button>
      </div>

      <!-- Elise -->
      <div class="threshold__elise">
        <div class="es-corner es-corner--frost tl" />
        <div class="es-corner es-corner--frost tr" />
        <div class="es-corner es-corner--frost bl" />
        <div class="es-corner es-corner--frost br" />
        <span class="es-label">Élise · au seuil</span>
        <p>Te revoilà. Le Palais n'a rien oublié de la dernière fois — moi non plus.</p>
      </div>

      <p v-if="runStore.error" class="threshold__error">{{ runStore.error }}</p>
    </div>
  </main>
</template>

<style scoped>
.threshold {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100dvh;
  padding: var(--space-4);
}

.threshold__card {
  position: relative;
  z-index: 1;
  width: min(780px, 92vw);
  background: oklch(0.20 0.04 272 / 0.85);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  box-shadow:
    oklch(1 0 0 / 0.03) 0 0 0 1px inset,
    oklch(0.12 0.03 272 / 0.7) 0 0 30px -8px inset,
    oklch(0.1 0.03 272 / 0.7) 0 18px 44px -26px,
    var(--wash-frost) 0 0 56px -22px;
  backdrop-filter: blur(12px);
  padding: var(--space-4) var(--space-6);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.threshold__header {
  display: flex;
  align-items: center;
  gap: var(--space-4);
}

.threshold__icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  font-size: 1.5rem;
  color: var(--frost);
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: 4px;
  flex-shrink: 0;
}

.threshold__title {
  font-family: var(--font-display);
  font-size: 1.6rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: var(--ink-2);
  margin: 0;
}

.threshold__tagline {
  font-family: var(--font);
  font-size: 0.82rem;
  color: var(--frost);
  margin: var(--space-1) 0 0;
}

.threshold__body {
  padding: 0 var(--space-2);
}

.threshold__description {
  font-family: var(--font);
  font-size: 0.92rem;
  line-height: 1.6;
  color: var(--ink-2);
  max-width: 52ch;
  margin: 0;
}

.threshold__actions {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  flex-wrap: wrap;
}

.threshold__run-stats {
  display: flex;
  gap: var(--space-4);
}

.threshold__stat {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.threshold__stat-value {
  font-family: var(--font);
  font-size: 0.82rem;
  color: var(--frost);
}

.threshold__divider {
  width: 1px;
  height: 32px;
  background: var(--line);
}

.threshold__elise {
  position: relative;
  padding: var(--space-3) var(--space-4);
  background: var(--card-soft);
  border: 1px solid var(--line-soft);
  border-radius: var(--radius-sm);
}

.threshold__elise p {
  font-family: var(--font);
  font-size: 0.88rem;
  color: var(--ink-2);
  margin: var(--space-1) 0 0;
  line-height: 1.5;
}

.threshold__error {
  color: var(--blood);
  font-size: 0.78rem;
  margin: 0;
}
</style>
