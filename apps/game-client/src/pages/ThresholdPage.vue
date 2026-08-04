<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import LivingWalls from '../shared/components/LivingWalls.vue';
import { useRunStore } from '../features/runs/stores/runStore';

const router   = useRouter();
const runStore = useRunStore();

const resumableRun = computed(() => runStore.resumableRun);
const hasResumable = computed(() => Boolean(resumableRun.value));

onMounted(() => { runStore.loadResumableRun(); });

const showConfirm = ref(false);
const isTransitioning = ref(false);
const transitionLabel = ref('');

function startTransition(label: string) {
  transitionLabel.value = label;
  isTransitioning.value = true;
}

async function resumeRun() {
  const run = resumableRun.value;
  if (!run) return;
  startTransition('Vous reprenez votre traversée…');
  try {
    await runStore.loadRun(run.id);
    if (runStore.currentRun?.id) await router.push(`/run/${run.id}`);
  } finally {
    isTransitioning.value = false;
  }
}

async function startRun() {
  startTransition('Vous franchissez le seuil…');
  try {
    await runStore.startRun();
    const runId = runStore.currentRun?.id;
    if (!runId) return;
    await router.push(`/run/${runId}`);
  } finally {
    isTransitioning.value = false;
  }
}

function onClickReprendre() {
  if (hasResumable.value) resumeRun();
}

function onClickNouvelle() {
  if (hasResumable.value) showConfirm.value = true;
  else startRun();
}

function confirmAbandon() {
  showConfirm.value = false;
  startRun();
}

/** Constante du monde du Palais (structure fixe à 27 salles) — pas une donnée de run. */
const TOTAL_ROOMS = 27;
</script>

<template>
  <main class="threshold-screen">
    <LivingWalls veins motes />

    <div class="threshold-content">
      <div class="threshold-title-block">
        <span class="threshold-kicker">Le Palais</span>
        <h1 class="threshold-title">L'Épopée des Silences</h1>
        <div class="threshold-title-rule" />
      </div>

      <div class="threshold-links">
        <button
          type="button"
          class="threshold-link"
          :class="{ 'threshold-link--disabled': !hasResumable }"
          :disabled="!hasResumable"
          @click="onClickReprendre"
        >
          <span class="threshold-link__glyph">◈</span>
          <span class="threshold-link__label">Reprendre la traversée</span>
        </button>

        <div class="threshold-links__divider" />

        <button type="button" class="threshold-link" @click="onClickNouvelle">
          <span class="threshold-link__glyph">○</span>
          <span class="threshold-link__label">Nouvelle traversée</span>
        </button>
      </div>

      <div v-if="resumableRun" class="threshold-info">
        <span>Semence <span class="threshold-info__value">{{ resumableRun.seed }}</span></span>
        <span class="threshold-info__sep">·</span>
        <span>Salle <span class="threshold-info__value">{{ resumableRun.currentRoomNumber }}</span> / {{ TOTAL_ROOMS }}</span>
      </div>

      <p v-if="runStore.error" class="threshold-error">{{ runStore.error }}</p>
    </div>

    <!-- ── Modale de confirmation ── -->
    <Teleport to="body">
      <Transition name="threshold-flood">
        <div v-if="showConfirm" class="confirm-backdrop" @click.self="showConfirm = false">
          <div class="confirm-dialog">
            <div class="confirm-dialog__body">
              <h2 class="confirm-dialog__title">Abandonner la traversée&nbsp;?</h2>
              <p class="confirm-dialog__desc">
                La traversée en cours — semence {{ resumableRun?.seed }}, salle {{ resumableRun?.currentRoomNumber }} sur {{ TOTAL_ROOMS }} — sera perdue. Le Palais n'attend pas deux fois.
              </p>
            </div>
            <div class="confirm-dialog__actions">
              <button type="button" class="confirm-btn confirm-btn--cancel" @click="showConfirm = false">
                Annuler
              </button>
              <button type="button" class="confirm-btn confirm-btn--danger" @click="confirmAbandon">
                Abandonner
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ── Transition plein écran ── -->
    <Teleport to="body">
      <Transition name="threshold-flood">
        <div v-if="isTransitioning" class="threshold-transition">
          <span class="threshold-transition__label">{{ transitionLabel }}</span>
        </div>
      </Transition>
    </Teleport>
  </main>
</template>

<style scoped>
.threshold-screen {
  position: relative;
  width: 100%;
  height: 100dvh;
  overflow: hidden;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

.threshold-content {
  position: relative;
  z-index: 3;
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 46px;
  padding: 40px 20px;
}

.threshold-title-block {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
}

.threshold-kicker {
  font-size: 11px;
  letter-spacing: 0.32em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.threshold-title {
  margin: 0;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: clamp(34px, 5vw, 50px);
  letter-spacing: 0.01em;
  color: var(--ink);
}

.threshold-title-rule {
  width: 120px;
  height: 1px;
  background: var(--mint-dim);
  animation: threshold-title-glow 6s ease-in-out infinite;
}

@keyframes threshold-title-glow {
  0%, 100% { opacity: 0.3; }
  50% { opacity: 0.7; }
}

.threshold-links {
  display: flex;
  align-items: stretch;
}

.threshold-link {
  all: unset;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 0 52px;
  cursor: pointer;
  color: var(--ink-2);
  transition: color .6s cubic-bezier(0.5, 0, 0.5, 1);
}

.threshold-link:hover:not(.threshold-link--disabled) {
  color: var(--mint);
}

.threshold-link:hover:not(.threshold-link--disabled) .threshold-link__glyph {
  color: var(--mint);
}

.threshold-link--disabled {
  cursor: not-allowed;
  color: var(--ink-5);
}

.threshold-link__glyph {
  font-size: 16px;
  color: var(--ink-4);
  transition: color .6s;
}

.threshold-link--disabled .threshold-link__glyph {
  color: var(--ink-5);
}

.threshold-link__label {
  font-size: 13px;
  font-weight: 500;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.threshold-links__divider {
  width: 1px;
  background: linear-gradient(var(--void), var(--line), var(--void));
}

.threshold-info {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 11px 22px;
  border-top: 1px solid var(--line-soft);
  border-bottom: 1px solid var(--line-soft);
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--ink-3);
}

.threshold-info__sep { color: var(--ink-5); }
.threshold-info__value { color: var(--ink-2); }

.threshold-error {
  color: var(--danger);
  font-size: 0.78rem;
}

/* ── Modale de confirmation ── */
.confirm-backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.05 0.008 262 / 0.7);
  backdrop-filter: blur(3px);
}

.confirm-dialog {
  width: 400px;
  background: var(--panel);
  border: 1px solid var(--line);
}

.confirm-dialog__body {
  padding: 28px 26px 20px;
  border-bottom: 1px solid var(--line-soft);
}

.confirm-dialog__title {
  margin: 0 0 10px;
  font-family: var(--font-display);
  font-style: italic;
  font-weight: 400;
  font-size: 22px;
  color: var(--ink);
}

.confirm-dialog__desc {
  margin: 0;
  font-size: 13px;
  line-height: 1.6;
  color: var(--ink-3);
}

.confirm-dialog__actions {
  display: flex;
  padding: 16px 26px 22px;
  gap: 12px;
  justify-content: flex-end;
}

.confirm-btn {
  padding: 9px 18px;
  background: transparent;
  border: 1px solid var(--line);
  color: var(--ink-3);
  font-family: var(--font);
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  cursor: pointer;
  transition: border-color .4s, background .4s;
}

.confirm-btn--cancel:hover { border-color: var(--ink-3); }

.confirm-btn--danger {
  border-color: var(--danger-dim);
  color: var(--danger-dim);
}

.confirm-btn--danger:hover {
  background: oklch(0.5 0.1 20 / 0.14);
}

/* ── Transition plein écran ── */
.threshold-transition {
  position: fixed;
  inset: 0;
  z-index: 200;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(circle at 50% 55%, var(--mint) 0%, var(--void) 74%);
}

.threshold-transition__label {
  font-family: var(--font-display);
  font-style: italic;
  font-size: 20px;
  letter-spacing: 0.02em;
  color: var(--void);
}

.threshold-flood-enter-active { transition: opacity 1.2s cubic-bezier(0.5, 0, 0.5, 1); }
.threshold-flood-leave-active { transition: opacity .3s ease; }
.threshold-flood-enter-from,
.threshold-flood-leave-to { opacity: 0; }

@media (prefers-reduced-motion: reduce) {
  .threshold-title-rule { animation: none; }
}
</style>
