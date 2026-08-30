<script setup lang="ts">
import { computed } from 'vue';

import type { PalacePublicIndicatorDto } from '../runs/types/runTypes';
import PalaceIndicatorCard from './PalaceIndicatorCard.vue';

const props = defineProps<{
  indicators?: PalacePublicIndicatorDto[] | null;
}>();

const publicIndicators = computed(() => props.indicators ?? []);
</script>

<template>
  <section class="ppip-root" aria-label="Indicateurs publics du Palais">
    <div class="ppip-head">
      <span class="es-kicker">Indicateurs du Palais</span>
      <span class="es-chip es-chip--frost">{{ publicIndicators.length }}</span>
    </div>

    <div v-if="publicIndicators.length" class="ppip-list">
      <PalaceIndicatorCard
        v-for="indicator in publicIndicators"
        :key="indicator.key"
        :indicator="indicator"
      />
    </div>

    <p v-else class="ppip-empty">Aucun indicateur public du Palais disponible.</p>
  </section>
</template>

<style scoped>
.ppip-root {
  display: grid;
  gap: var(--space-2);
  padding: var(--space-3);
  border: 1px solid color-mix(in oklch, var(--gold), transparent 70%);
  border-radius: var(--radius-sm);
  background: oklch(0.16 0.034 268 / 0.58);
}

.ppip-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.ppip-list {
  display: grid;
  gap: var(--space-2);
}

.ppip-empty {
  margin: 0;
  color: var(--ink-4);
  font-size: 0.78rem;
  line-height: 1.45;
}
</style>
