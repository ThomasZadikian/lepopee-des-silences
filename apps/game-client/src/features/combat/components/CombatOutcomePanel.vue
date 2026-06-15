<script setup lang="ts">
defineProps<{
  isVictory: boolean;
  isLoading: boolean;
}>();

defineEmits<{
  continue: [];
  leaveRun: [];
}>();
</script>

<template>
  <section
    class="outcome-panel"
    :class="{
      'outcome-panel--victory': isVictory,
      'outcome-panel--defeat': !isVictory,
    }"
  >
    <div class="outcome-panel__backdrop" />

    <div class="outcome-panel__content">
      <p class="system-label">COMBAT TERMINÉ</p>
      <h2 v-if="isVictory" class="outcome-panel__title outcome-panel__title--victory">
        VICTOIRE
      </h2>
      <h2 v-else class="outcome-panel__title outcome-panel__title--defeat">
        DÉFAITE
      </h2>
      <p v-if="isVictory" class="outcome-panel__desc">
        Tous les ennemis ont été vaincus.
      </p>
      <p v-else class="outcome-panel__desc">
        Tous les alliés ont été vaincus.
      </p>

      <button
        v-if="isVictory"
        class="ghost-button outcome-panel__button"
        :disabled="isLoading"
        @click="$emit('continue')"
      >
        {{ isLoading ? 'CHARGEMENT…' : 'CONTINUER →' }}
      </button>

      <button
        v-else
        class="ghost-button outcome-panel__button outcome-panel__button--defeat"
        :disabled="isLoading"
        @click="$emit('leaveRun')"
      >
        {{ isLoading ? 'CHARGEMENT…' : 'QUITTER LA RUN' }}
      </button>
    </div>
  </section>
</template>

<style scoped>
.outcome-panel {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
}

.outcome-panel__backdrop {
  position: absolute;
  inset: 0;
  background: color-mix(in oklch, var(--void), transparent 25%);
  animation: outcome-backdrop-in 280ms ease-out;
}

.outcome-panel--victory .outcome-panel__backdrop {
  background: color-mix(in oklch, var(--gold), transparent 88%);
}

.outcome-panel--defeat .outcome-panel__backdrop {
  background: color-mix(in oklch, var(--blood), transparent 82%);
}

.outcome-panel__content {
  position: relative;
  display: grid;
  gap: var(--space-4);
  text-align: center;
  padding: var(--space-8);
  animation: outcome-content-in 360ms ease-out;
}

.outcome-panel__title {
  margin: 0;
  font-size: 2rem;
  letter-spacing: 0.25em;
}

.outcome-panel__title--victory {
  color: var(--gold);
}

.outcome-panel__title--defeat {
  color: var(--blood);
}

.outcome-panel__desc {
  color: var(--ink-3);
}

.outcome-panel__button {
  justify-self: center;
  border-color: var(--gold);
  color: var(--gold);
}

.outcome-panel__button--defeat {
  border-color: var(--blood);
  color: var(--blood);
}

@keyframes outcome-backdrop-in {
  0% { opacity: 0; }
  100% { opacity: 1; }
}

@keyframes outcome-content-in {
  0% {
    opacity: 0;
    transform: translateY(8px);
  }
  100% {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (prefers-reduced-motion: reduce) {
  .outcome-panel__backdrop,
  .outcome-panel__content {
    animation: none;
  }
}
</style>
