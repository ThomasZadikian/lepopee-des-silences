<script setup lang="ts">
import { ref } from 'vue';
import ConfirmDialog from '../../../shared/components/ConfirmDialog.vue';

const props = defineProps<{
  /** Whether the run is currently at a safe point (RoomResolved or Interlude). */
  isSafePoint: boolean;
  isSavingAndExiting: boolean;
  isAbandoningRun: boolean;
  isExitingMidRoom?: boolean;
  error?: string | null;
}>();

const emit = defineEmits<{
  saveAndExit: [];
  abandon: [];
  exitMidRoom: [];
}>();

const showAbandonDialog = ref(false);

function onAbandonConfirm() {
  showAbandonDialog.value = false;
  emit('abandon');
}
</script>

<template>
  <div class="danger-actions">
    <p class="system-label danger-actions__label">Actions de run</p>

    <p v-if="!isSafePoint" class="danger-actions__hint">
      Disponible en fin de salle
    </p>

    <button
      v-if="!isSafePoint"
      class="ghost-button danger-actions__exit-room"
      :disabled="isExitingMidRoom"
      title="Quitter la salle en cours et retourner au Repli du Palais (les ressources gagnées dans cette salle seront perdues)"
      @click="$emit('exitMidRoom')"
    >
      <span v-if="isExitingMidRoom">Sortie…</span>
      <span v-else>⟳ Quitter la salle</span>
    </button>

    <div class="danger-actions__buttons">
      <button
        class="ghost-button danger-actions__save"
        :disabled="!isSafePoint || isSavingAndExiting || isAbandoningRun"
        :title="!isSafePoint ? 'Terminer la salle actuelle d\'abord' : 'Sauvegarder et retourner au menu'"
        @click="$emit('saveAndExit')"
      >
        <span v-if="isSavingAndExiting">Sauvegarde…</span>
        <span v-else>Sauvegarder et quitter</span>
      </button>

      <button
        class="ghost-button danger-actions__abandon"
        :disabled="!isSafePoint || isSavingAndExiting || isAbandoningRun"
        :title="!isSafePoint ? 'Terminer la salle actuelle d\'abord' : 'Abandonner la run définitivement'"
        @click="isSafePoint && (showAbandonDialog = true)"
      >
        <span v-if="isAbandoningRun">Abandon…</span>
        <span v-else>Abandonner la run</span>
      </button>
    </div>

    <p v-if="error" class="danger-actions__error">{{ error }}</p>

    <ConfirmDialog
      v-model="showAbandonDialog"
      title="Abandonner la run ?"
      message="Cette action est irréversible. La run sera définitivement fermée et ne pourra pas être reprise."
      confirm-label="Abandonner définitivement"
      cancel-label="Annuler"
      :danger="true"
      :loading="isAbandoningRun"
      @confirm="onAbandonConfirm"
    />
  </div>
</template>

<style scoped>
.danger-actions {
  display: grid;
  gap: var(--space-2);
}

.danger-actions__label {
  margin-bottom: var(--space-1);
}

.danger-actions__hint {
  font-family: var(--font-mono);
  font-size: 0.75rem;
  color: var(--color-dim);
  margin: 0;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.danger-actions__error {
  color: var(--color-blood);
  font-family: var(--font-mono);
  font-size: 0.78rem;
  margin: 0;
}

.danger-actions__buttons {
  display: grid;
  gap: var(--space-2);
}

.danger-actions__save {
  width: 100%;
  justify-content: flex-start;
  color: var(--color-muted);
  border-color: color-mix(in oklch, var(--color-line), transparent 20%);
  font-size: 0.82rem;
  text-align: left;
}

.danger-actions__save:hover:not(:disabled) {
  color: var(--color-ink);
  border-color: var(--color-frost);
}

.danger-actions__abandon {
  width: 100%;
  justify-content: flex-start;
  color: var(--color-blood);
  border-color: color-mix(in oklch, var(--color-blood), transparent 60%);
  font-size: 0.82rem;
  text-align: left;
}

.danger-actions__abandon:hover:not(:disabled) {
  background: color-mix(in oklch, var(--color-blood), transparent 92%);
  border-color: var(--color-blood);
}

.danger-actions__exit-room {
  width: 100%;
  justify-content: flex-start;
  color: var(--color-gold);
  border-color: color-mix(in oklch, var(--color-gold), transparent 55%);
  font-size: 0.82rem;
  text-align: left;
  margin-bottom: var(--space-2);
}

.danger-actions__exit-room:hover:not(:disabled) {
  background: color-mix(in oklch, var(--color-gold), transparent 88%);
  border-color: var(--color-gold);
}

.danger-actions__exit-room:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.danger-actions__save:disabled,
.danger-actions__abandon:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}
</style>
