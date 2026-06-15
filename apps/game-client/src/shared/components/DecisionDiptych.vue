<script setup lang="ts">
import { watch } from 'vue';

const props = defineProps<{
  modelValue: boolean;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  confirm: [];
}>();

function onConfirm() {
  emit('confirm');
}

function onCancel() {
  emit('update:modelValue', false);
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') onCancel();
}

watch(() => props.modelValue, (open) => {
  if (open) document.addEventListener('keydown', onKeydown);
  else document.removeEventListener('keydown', onKeydown);
});
</script>

<template>
  <Teleport to="body">
    <Transition name="fade">
      <div v-if="modelValue" class="diptych-overlay" @click.self="onCancel">
        <div class="diptych">
          <div class="diptych__side diptych__side--cancel" @click="onCancel">
            <span class="diptych__label">{{ cancelLabel ?? 'Annuler' }}</span>
            <span class="diptych__glyph">✕</span>
          </div>

          <div class="diptych__center">
            <p class="es-kicker">Confirmation</p>
            <h3 class="es-h2">{{ title }}</h3>
            <p v-if="description" class="es-lede es-dim diptych__desc">{{ description }}</p>
          </div>

          <div
            class="diptych__side diptych__side--confirm"
            :class="{ 'diptych__side--danger': danger }"
            @click="onConfirm"
          >
            <span class="diptych__label">{{ confirmLabel ?? 'Confirmer' }}</span>
            <span class="diptych__glyph">→</span>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.diptych-overlay {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  background: oklch(0.10 0.03 272 / 0.8);
  backdrop-filter: blur(6px);
}

.diptych {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  width: min(680px, 90vw);
  min-height: 200px;
}

.diptych__side {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  padding: var(--space-6);
  cursor: pointer;
  transition: background 0.2s ease;
}

.diptych__side--cancel {
  background: var(--panel);
  border: 1px solid var(--line);
  border-radius: var(--radius-md) 0 0 var(--radius-md);
  border-right: none;
}

.diptych__side--cancel:hover {
  background: var(--panel-2);
}

.diptych__side--confirm {
  background: var(--panel);
  border: 1px solid var(--edge-frost);
  border-radius: 0 var(--radius-md) var(--radius-md) 0;
  border-left: none;
  color: var(--frost);
}

.diptych__side--confirm:hover {
  background: var(--card-sel-frost);
}

.diptych__side--danger {
  border-color: color-mix(in oklch, var(--blood), transparent 50%);
  color: var(--blood);
}

.diptych__side--danger:hover {
  background: var(--wash-blood);
}

.diptych__label {
  font-family: var(--font-caps);
  font-size: 0.72rem;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.diptych__glyph {
  font-size: 1.5rem;
  opacity: 0.5;
}

.diptych__center {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  padding: var(--space-6) var(--space-4);
  background: var(--bg);
  border-top: 1px solid var(--line);
  border-bottom: 1px solid var(--line);
  text-align: center;
}

.diptych__desc {
  max-width: 280px;
  margin: 0;
}
</style>
