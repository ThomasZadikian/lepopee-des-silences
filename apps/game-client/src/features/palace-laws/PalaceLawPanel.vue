<script setup lang="ts">
import type { ActivePalaceLawDto } from '../runs/types/runTypes';

defineProps<{
  laws?: ActivePalaceLawDto[];
}>();
</script>

<template>
  <section class="panel laws">
    <header>
      <span class="system-label">Lois du Palais</span>
      <span class="system-value">{{ laws?.length ?? 0 }} actives</span>
    </header>

    <article v-if="!laws || laws.length === 0">
      <strong>Aucune loi active</strong>
      <p>Le Palais retient encore son souffle.</p>
      <small>Système</small>
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
  border-top: 1px solid color-mix(in oklch, var(--color-line), transparent 45%);
}

.laws strong {
  color: var(--color-gold);
}

.laws p {
  color: var(--color-muted);
  font-size: 0.9rem;
}

.laws small {
  color: var(--color-dim);
  font-family: var(--font-mono);
  text-transform: uppercase;
}
</style>