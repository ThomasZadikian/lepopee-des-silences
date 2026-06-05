<script setup lang="ts">
defineProps<{
  message?: string | null;
}>();
</script>

<template>
  <aside class="elise panel">
    <p class="system-label">ELISE</p>
    <p>
      {{ message || "ELISE_PLACEHOLDER_MESSAGES" }}
    </p>
  </aside>
</template>

<style scoped>
.elise {
  position: absolute;
  left: var(--space-6);
  bottom: var(--space-6);
  max-width: 34rem;
  padding: var(--space-4) var(--space-6);
  border-color: color-mix(in oklch, var(--color-frost), transparent 25%);
}

.elise p:last-child {
  margin-bottom: 0;
  color: var(--color-ink);
  line-height: 1.55;
}
</style>