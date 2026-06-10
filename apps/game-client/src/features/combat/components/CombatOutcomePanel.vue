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
  background: color-mix(in oklch, var(--color-void), transparent 25%);
}

.outcome-panel--victory .outcome-panel__backdrop {
  background: color-mix(in oklch, var(--color-gold), transparent 88%);
}

.outcome-panel--defeat .outcome-panel__backdrop {
  background: color-mix(in oklch, var(--color-blood), transparent 82%);
}

.outcome-panel__content {
  position: relative;
  display: grid;
  gap: var(--space-4);
  text-align: center;
  padding: var(--space-8);
}

.outcome-panel__title {
  margin: 0;
  font-size: 2rem;
  letter-spacing: 0.25em;
}

.outcome-panel__title--victory {
  color: var(--color-gold);
}

.outcome-panel__title--defeat {
  color: var(--color-blood);
}

.outcome-panel__desc {
  color: var(--color-muted);
}

.outcome-panel__button {
  justify-self: center;
  border-color: var(--color-gold);
  color: var(--color-gold);
}

.outcome-panel__button--defeat {
  border-color: var(--color-blood);
  color: var(--color-blood);
}
</style>
