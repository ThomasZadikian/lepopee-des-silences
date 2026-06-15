<script setup lang="ts">
import type { CombatMetricsState } from '../composables/useCombatMetrics';
import CombatSideMeter from './CombatSideMeter.vue';

defineProps<{
  metrics: CombatMetricsState;
}>();
</script>

<template>
  <div class="meters-panel">
    <CombatSideMeter
      title="Les Voix"
      side="allies"
      :metrics="metrics.allies"
    />
    <CombatSideMeter
      title="Les Manifestations"
      side="enemies"
      :metrics="metrics.enemies"
    />
  </div>
</template>

<style scoped>
.meters-panel {
  display: flex;
  gap: var(--space-3);
  justify-content: center;
  padding: var(--space-1) var(--space-4);
  border-bottom: 1px solid var(--line-soft);
}
</style>
