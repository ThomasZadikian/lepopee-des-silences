<script setup lang="ts">
import GameTopBar from '../../features/run-shell/GameTopBar.vue';
import SessionMenu from '../../features/account/components/SessionMenu.vue';
import LivingWalls from '../../shared/components/LivingWalls.vue';

defineProps<{
  hideTopBar?: boolean;
}>();
</script>

<template>
  <div class="game-shell">
    <LivingWalls />

    <GameTopBar v-if="!hideTopBar" />
    <SessionMenu />

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
  background: var(--void);
}

.game-shell__main {
  flex: 1;
  min-height: 0;
  position: relative;
  z-index: var(--z-base);
}
</style>
