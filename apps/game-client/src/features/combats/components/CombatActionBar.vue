<script setup lang="ts">
defineProps<{
  canSubmit: boolean;
  isLoading: boolean;
}>();

defineEmits<{
  basicAttack: [];
}>();
</script>

<template>
  <section class="combat-actions">
    <p class="system-label">Compétences · joueur</p>

    <button
      class="combat-actions__button"
      :disabled="!canSubmit || isLoading"
      @click="$emit('basicAttack')"
    >
      <strong>Aveu</strong>
      <span>BasicAttack · Frappe ciblée serveur-autoritaire.</span>
    </button>

    <button class="combat-actions__button" disabled>
      <strong>Refoulement</strong>
      <span>Garde · bientôt disponible.</span>
    </button>

    <button class="combat-actions__button" disabled>
      <strong>Cri</strong>
      <span>Zone · bientôt disponible.</span>
    </button>
  </section>
</template>

<style scoped>
.combat-actions {
  display: grid;
  gap: var(--space-3);
}

.combat-actions__button {
  display: grid;
  gap: var(--space-1);
  padding: var(--space-3);
  color: var(--color-ink);
  background: color-mix(in oklch, var(--color-panel), transparent 8%);
  border: 1px solid color-mix(in oklch, var(--color-line), transparent 25%);
  text-align: left;
  cursor: pointer;
}

.combat-actions__button:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.combat-actions__button:not(:disabled):hover {
  border-color: var(--color-frost);
}

.combat-actions__button strong {
  color: var(--color-frost);
  font-family: var(--font-mono);
  text-transform: uppercase;
}

.combat-actions__button span {
  color: var(--color-muted);
  font-size: 0.85rem;
}
</style>