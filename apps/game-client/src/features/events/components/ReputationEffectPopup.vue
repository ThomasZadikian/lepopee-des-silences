<script setup lang="ts">
import { watch } from 'vue';

const props = defineProps<{
  effects: { id: number; amount: number; npcName: string }[];
}>();

const emit = defineEmits<{
  dismiss: [id: number];
}>();

const AUTO_DISMISS_MS = 1800;

watch(
  () => props.effects.map((e) => e.id),
  (ids, previousIds) => {
    const previous = new Set(previousIds ?? []);
    for (const id of ids) {
      if (previous.has(id)) continue;
      setTimeout(() => emit('dismiss', id), AUTO_DISMISS_MS);
    }
  },
  { immediate: true },
);
</script>

<template>
  <div class="rep-stack" aria-live="polite">
    <TransitionGroup name="rep-pop">
      <div
        v-for="effect in effects"
        :key="effect.id"
        class="rep-pill"
        :class="effect.amount > 0 ? 'rep-pill--gain' : 'rep-pill--loss'"
      >
        <span class="rep-pill__amount">{{ effect.amount > 0 ? '+' : '' }}{{ effect.amount }}</span>
        <span class="rep-pill__label">{{ effect.npcName }}</span>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.rep-stack {
  position: fixed;
  top: 84px;
  right: 24px;
  z-index: 9200;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
  pointer-events: none;
}

.rep-pill {
  display: flex;
  align-items: baseline;
  gap: 8px;
  padding: 6px 12px;
  border: 1px solid var(--line);
  background: var(--panel);
  backdrop-filter: blur(6px);
}

.rep-pill__amount {
  font-family: var(--font-mono);
  font-size: 13px;
}

.rep-pill__label {
  font-family: var(--font-mono);
  font-size: 9.5px;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.rep-pill--gain .rep-pill__amount { color: var(--mint-dim); }
.rep-pill--loss .rep-pill__amount { color: var(--mauve-dim); }

.rep-pop-enter-active,
.rep-pop-leave-active {
  transition: opacity .25s, transform .25s;
}

.rep-pop-enter-from {
  opacity: 0;
  transform: translateY(-8px);
}

.rep-pop-leave-to {
  opacity: 0;
  transform: translateX(12px);
}

@media (prefers-reduced-motion: reduce) {
  .rep-pop-enter-active,
  .rep-pop-leave-active {
    transition: none;
  }
}
</style>
