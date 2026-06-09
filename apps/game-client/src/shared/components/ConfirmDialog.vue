<script setup lang="ts">
defineProps<{
  modelValue: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
  loading?: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  confirm: [];
  cancel: [];
}>();

function close() {
  emit('update:modelValue', false);
  emit('cancel');
}

function confirm() {
  emit('confirm');
}
</script>

<template>
  <Teleport to="body">
    <Transition name="dialog-fade">
      <div
        v-if="modelValue"
        class="dialog-backdrop"
        @click.self="close"
      >
        <div class="dialog panel" role="dialog" :aria-modal="true">
          <p class="system-label">{{ danger ? 'Action irréversible' : 'Confirmation' }}</p>
          <h3 class="dialog__title">{{ title }}</h3>
          <p class="dialog__message">{{ message }}</p>

          <footer class="dialog__actions">
            <button
              class="ghost-button dialog__cancel"
              :disabled="loading"
              @click="close"
            >
              {{ cancelLabel ?? 'Annuler' }}
            </button>
            <button
              class="ghost-button dialog__confirm"
              :class="{ 'dialog__confirm--danger': danger }"
              :disabled="loading"
              @click="confirm"
            >
              <span v-if="loading">En cours…</span>
              <span v-else>{{ confirmLabel ?? 'Confirmer' }}</span>
            </button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.dialog-backdrop {
  position: fixed;
  inset: 0;
  z-index: 200;
  display: grid;
  place-items: center;
  background: color-mix(in oklch, var(--color-void), transparent 30%);
  backdrop-filter: blur(2px);
}

.dialog {
  width: min(36rem, 90vw);
  padding: var(--space-6);
  display: grid;
  gap: var(--space-4);
}

.dialog__title {
  margin: 0;
  font-size: 1.3rem;
  color: var(--color-frost);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.dialog__message {
  color: var(--color-muted);
  line-height: 1.6;
  margin: 0;
}

.dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-3);
  padding-top: var(--space-2);
  border-top: 1px solid color-mix(in oklch, var(--color-line), transparent 40%);
}

.dialog__cancel {
  color: var(--color-dim);
  border-color: color-mix(in oklch, var(--color-line), transparent 20%);
}

.dialog__cancel:hover:not(:disabled) {
  color: var(--color-ink);
}

.dialog__confirm {
  border-color: var(--color-gold);
  color: var(--color-gold);
}

.dialog__confirm--danger {
  border-color: var(--color-blood);
  color: var(--color-blood);
}

.dialog__confirm:hover:not(:disabled) {
  background: color-mix(in oklch, var(--color-gold), transparent 88%);
}

.dialog__confirm--danger:hover:not(:disabled) {
  background: color-mix(in oklch, var(--color-blood), transparent 88%);
}

.dialog__confirm:disabled,
.dialog__cancel:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.dialog-fade-enter-active,
.dialog-fade-leave-active {
  transition: opacity 0.15s ease;
}

.dialog-fade-enter-from,
.dialog-fade-leave-to {
  opacity: 0;
}
</style>
