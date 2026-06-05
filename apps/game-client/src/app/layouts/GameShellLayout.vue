<script setup lang="ts">
import GameTopBar from '../../features/run-shell/GameTopBar.vue';
</script>

<template>
  <div class="game-shell">
    <GameTopBar />

    <main class="game-shell__main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.game-shell {
  min-height: 100vh;
  position: relative;
  overflow: hidden;
}

.game-shell::before {
  content: '';
  position: fixed;
  inset: 0;
  pointer-events: none;
  background:
    linear-gradient(rgb(255 255 255 / 2%) 1px, transparent 1px),
    linear-gradient(90deg, rgb(255 255 255 / 2%) 1px, transparent 1px);
  background-size: 64px 64px;
  mask-image: radial-gradient(circle at center, black, transparent 80%);
}

.game-shell__main {
  padding: var(--space-4);
}
</style>