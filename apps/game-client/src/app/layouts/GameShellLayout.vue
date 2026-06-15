<script setup lang="ts">
import GameTopBar from '../../features/run-shell/GameTopBar.vue';
</script>

<template>
  <div class="game-shell">
    <div class="es-atmos">
      <div class="es-vignette" />
      <div class="es-grain" />
    </div>

    <GameTopBar />

    <main class="game-shell__main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.game-shell {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100dvh;
  overflow: hidden;
}

.game-shell__main {
  flex: 1;
  min-height: 0;
  position: relative;
  z-index: var(--z-base);
}
</style>
