<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';

import { useRunStore } from '../features/runs/stores/runStore';

const router = useRouter();
const runStore = useRunStore();

const resumableRun = computed(() => runStore.resumableRun);

onMounted(() => {
  runStore.loadResumableRun();
});

async function resumeRun() {
  const run = resumableRun.value;
  if (!run) return;
  await runStore.loadRun(run.id);
  if (runStore.currentRun?.id) {
    await router.push(`/run/${run.id}`);
  }
}

async function startRun() {
  await runStore.startRun();
  const runId = runStore.currentRun?.id;
  if (!runId) {
    console.error('[game-client] Unable to navigate: run id is missing.', {
      currentRun: runStore.currentRun,
    });
    return;
  }
  await router.push(`/run/${runId}`);
}

function formatSavedAt(savedAt: string): string {
  try {
    return new Intl.DateTimeFormat('fr-FR', {
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(savedAt));
  } catch {
    return '—';
  }
}
</script>

<template>
  <main class="threshold">
    <section class="threshold__hero">
      <p class="system-label">Seuil du Palais</p>
      <h1>L'Épopée des silences</h1>
      <p class="threshold__subtitle">
        Chaque traversée du Palais s'écrit dans ton Tome — puis se tait.
      </p>
    </section>

    <section class="threshold__grid">

      <!-- Run en cours / reprise -->
      <article class="panel threshold-card">
        <!-- Loading -->
        <template v-if="runStore.isLoadingResumableRun">
          <p class="system-label">Run en cours · vérification</p>
          <h2>Recherche d'une run suspendue…</h2>
        </template>

        <!-- Live resumable run -->
        <template v-else-if="resumableRun">
          <p class="system-label">Run en cours · reprise</p>
          <h2>Reprendre la descente</h2>

          <dl>
            <div>
              <dt>Seed</dt>
              <dd>{{ resumableRun.seed }}</dd>
            </div>
            <div>
              <dt>Salle</dt>
              <dd>{{ resumableRun.currentRoomNumber }}</dd>
            </div>
            <div>
              <dt>Sauvegardée le</dt>
              <dd>{{ formatSavedAt(resumableRun.savedAt) }}</dd>
            </div>
            <div>
              <dt>Compagnon</dt>
              <dd>Neige</dd>
            </div>
          </dl>

          <button
            class="ghost-button"
            :disabled="runStore.isLoading"
            @click="resumeRun"
          >
            {{ runStore.isLoading ? 'Chargement…' : 'Reprendre →' }}
          </button>

          <p v-if="runStore.error" class="threshold-card__error">
            {{ runStore.error }}
          </p>
        </template>

        <!-- No resumable run -->
        <template v-else>
          <p class="system-label">Run en cours · aucune</p>
          <h2>Aucune run suspendue</h2>
          <p>
            Sauvegarde ta progression depuis le Repli du Palais ou la fin
            d'une pièce pour pouvoir reprendre ici.
          </p>
          <button class="ghost-button" disabled>
            Reprendre →
          </button>
        </template>
      </article>

      <!-- Nouvelle seed -->
      <article class="panel threshold-card threshold-card--primary">
        <p class="system-label">Nouveau seuil · seed inédite</p>
        <h2>Franchir un nouveau seuil</h2>
        <p>
          Une seed inédite réécrit l'architecture du Palais : pièces, lois,
          ennemis. Tu n'emportes que ce que le Tome a déjà retenu.
        </p>

        <button
          class="ghost-button"
          :disabled="runStore.isLoading"
          @click="startRun"
        >
          {{ runStore.isLoading ? 'Génération…' : 'Générer une run →' }}
        </button>

        <p v-if="runStore.error" class="threshold-card__error">
          {{ runStore.error }}
        </p>
      </article>
    </section>

    <aside class="panel threshold__elise">
      <p class="system-label">Élise · au seuil</p>
      <p>
        Te revoilà. Le Palais n'a rien oublié de la dernière fois — moi non plus.
      </p>
    </aside>
  </main>
</template>

<style scoped>
.threshold {
  min-height: 100vh;
  display: grid;
  align-content: center;
  gap: var(--space-8);
  padding: min(8vh, 5rem) clamp(1rem, 5vw, 6rem);
}

.threshold__hero {
  max-width: 56rem;
}

.threshold-card__error {
  margin-top: var(--space-4);
  color: var(--color-blood);
  font-family: var(--font-mono);
  font-size: 0.8rem;
}

.threshold__hero h1 {
  margin: var(--space-2) 0;
  font-size: clamp(2.5rem, 7vw, 6.5rem);
  line-height: 0.9;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.threshold__subtitle {
  max-width: 42rem;
  color: var(--color-muted);
  font-size: 1.15rem;
}

.threshold__grid {
  display: grid;
  grid-template-columns: 1fr 1.1fr;
  gap: var(--space-4);
}

.threshold-card {
  padding: var(--space-6);
}

.threshold-card--primary {
  border-color: color-mix(in oklch, var(--color-gold), transparent 40%);
}

.threshold-card h2 {
  margin: var(--space-2) 0 var(--space-4);
  font-size: 1.6rem;
}

.threshold-card p,
.threshold-card dd {
  color: var(--color-muted);
}

.threshold-card dl {
  display: grid;
  gap: var(--space-3);
  margin: var(--space-6) 0;
}

.threshold-card div {
  display: flex;
  justify-content: space-between;
  gap: var(--space-4);
  border-bottom: 1px solid color-mix(in oklch, var(--color-line), transparent 50%);
  padding-bottom: var(--space-2);
}

.threshold-card dt,
.threshold-card dd {
  margin: 0;
  font-family: var(--font-mono);
  font-size: 0.8rem;
}

.threshold-card dt {
  color: var(--color-dim);
  text-transform: uppercase;
}

.threshold__elise {
  max-width: 48rem;
  padding: var(--space-4) var(--space-6);
  border-color: color-mix(in oklch, var(--color-frost), transparent 35%);
}

.threshold__elise p:last-child {
  color: var(--color-ink);
}
</style>
