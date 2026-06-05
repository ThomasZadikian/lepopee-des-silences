<script setup lang="ts">
import GameShellLayout from '../app/layouts/GameShellLayout.vue';
import EliseOverlay from '../features/elise/EliseOverlay.vue';
import NodeDetailPanel from '../features/node-details/NodeDetailPanel.vue';
import PalaceLawPanel from '../features/palace-laws/PalaceLawPanel.vue';
import PalaceMapPlaceholder from '../features/palace-map/PalaceMapPlaceholder.vue';
import PartyPanel from '../features/party/PartyPanel.vue';
</script>

<template>
  <GameShellLayout>
    <section class="run-grid">
      <aside class="run-grid__left">
        <PartyPanel />
        <PalaceLawPanel />
      </aside>

      <section class="run-grid__center panel">
        <PalaceMapPlaceholder />
        <EliseOverlay />
      </section>

      <aside class="run-grid__right">
        <NodeDetailPanel />
      </aside>
    </section>
  </GameShellLayout>
</template>

<style scoped>
.run-grid {
  display: grid;
  grid-template-columns: 18rem minmax(36rem, 1fr) 20rem;
  gap: var(--space-4);
  height: calc(100vh - 7rem);
}

.run-grid__left,
.run-grid__right {
  display: grid;
  gap: var(--space-4);
  align-content: start;
}

.run-grid__center {
  position: relative;
  overflow: hidden;
  padding: var(--space-6);
}
</style>