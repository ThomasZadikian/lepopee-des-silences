<script setup lang="ts">
import type { ActivePalaceLawDto } from '../runs/types/runTypes';

defineProps<{
  laws?: ActivePalaceLawDto[];
}>();
</script>

<template>
  <section class="panel laws">
    <header>
      <span class="system-label">PALACE_LAWS</span>
      <span class="system-value">{{ laws?.length ?? 0 }} ACTIVES</span>
    </header>

    <article v-if="!laws || laws.length === 0">
      <strong>NO_ACTIVE_LAWS</strong>
      <p>MESSAGE_NO_ACTIVE_LAWS</p>
    </article>

    <article v-for="law in laws" :key="`${law.key}-${law.version}`">
      <strong>{{ law.displayName }} {{ law.version }}</strong>
      <p>{{ law.description }}</p>
      <small>{{ law.domain }}</small>
    </article>
  </section>
</template>

<style scoped>
.laws {
  padding: var(--space-4);
}

.laws header {
  display: flex;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.laws article {
  padding: var(--space-3) 0;
  border-top: 1px solid color-mix(in oklch, var(--line), transparent 45%);
}

.laws strong {
  color: var(--gold);
}

.laws p {
  color: var(--ink-3);
  font-size: 0.9rem;
}

.laws small {
  color: var(--ink-4);
  font-family: var(--font-mono);
  text-transform: uppercase;
}
</style>