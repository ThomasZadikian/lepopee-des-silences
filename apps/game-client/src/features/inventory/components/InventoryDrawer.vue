<script setup lang="ts">
import type { RunItemDto } from '../../runs/types/runTypes';
import BesacePanel from '../../../shared/components/BesacePanel.vue';

defineProps<{
  items: RunItemDto[];
  runId: string;
  capacity?: number | null;
}>();

const emit = defineEmits<{ close: [] }>();
</script>

<template>
  <aside class="inv-drawer" aria-label="La Besace">
    <button type="button" class="inv-drawer__close" aria-label="Fermer" @click="emit('close')">✕</button>
    <BesacePanel :items="items" :run-id="runId" :capacity="capacity" />
  </aside>
</template>

<style scoped>
.inv-drawer {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  z-index: var(--z-drawer, 30);
  width: 380px;
  background: var(--panel);
  border-left: 1px solid var(--line);
  padding: 24px 22px;
  overflow-y: auto;
  animation: inv-drawer-slide .35s cubic-bezier(0.5, 0, 0.5, 1);
}

.inv-drawer__close {
  all: unset;
  position: absolute;
  top: 16px;
  right: 16px;
  cursor: pointer;
  color: var(--ink-4);
  font-size: 12px;
  padding: 4px;
  transition: color .15s;
}

.inv-drawer__close:hover { color: var(--mint-dim); }

@keyframes inv-drawer-slide {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}

@media (prefers-reduced-motion: reduce) {
  .inv-drawer { animation: none; }
}
</style>
