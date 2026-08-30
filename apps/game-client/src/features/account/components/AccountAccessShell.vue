<script setup lang="ts">
import LivingWalls from '../../../shared/components/LivingWalls.vue';

defineProps<{
  kicker?: string;
  title: string;
  subtitle?: string;
  narrow?: boolean;
}>();
</script>

<template>
  <main class="account-shell">
    <LivingWalls veins motes />

    <section class="account-shell__content" :class="{ 'account-shell__content--narrow': narrow }">
      <header class="account-shell__header">
        <span class="account-shell__kicker">{{ kicker || 'Le Palais' }}</span>
        <h1 class="account-shell__title">{{ title }}</h1>
        <p v-if="subtitle" class="account-shell__subtitle">{{ subtitle }}</p>
        <span class="account-shell__rule" />
      </header>

      <div class="account-shell__panel">
        <slot />
      </div>

      <footer class="account-shell__footer">
        <slot name="footer" />
      </footer>
    </section>
  </main>
</template>

<style scoped>
.account-shell {
  position: relative;
  width: 100%;
  min-height: 100dvh;
  overflow-x: hidden;
  background: var(--void);
  color: var(--ink);
  font-family: var(--font);
  -webkit-font-smoothing: antialiased;
}

.account-shell__content {
  position: relative;
  z-index: 3;
  width: min(920px, calc(100% - 40px));
  min-height: 100dvh;
  margin: 0 auto;
  padding: 72px 0 48px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 34px;
}

.account-shell__content--narrow {
  width: min(480px, calc(100% - 40px));
}

.account-shell__header {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 10px;
}

.account-shell__kicker {
  font-size: 10px;
  letter-spacing: .32em;
  text-transform: uppercase;
  color: var(--ink-4);
}

.account-shell__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(32px, 5vw, 46px);
  line-height: 1;
  font-style: italic;
  font-weight: 400;
  color: var(--ink);
}

.account-shell__subtitle {
  max-width: 660px;
  margin: 0;
  color: var(--ink-3);
  font-size: 13px;
  line-height: 1.65;
}

.account-shell__rule {
  width: 88px;
  height: 1px;
  margin-top: 8px;
  background: var(--mint-dim);
  opacity: .55;
}

.account-shell__panel {
  border: 1px solid var(--line);
  background: color-mix(in srgb, var(--panel) 90%, transparent);
  box-shadow: var(--shadow-panel);
  backdrop-filter: blur(10px);
}

.account-shell__footer {
  min-height: 18px;
  text-align: center;
  color: var(--ink-4);
  font-size: 12px;
}

@media (max-width: 640px) {
  .account-shell__content,
  .account-shell__content--narrow {
    width: min(100% - 24px, 920px);
    padding: 44px 0 28px;
  }
}
</style>
