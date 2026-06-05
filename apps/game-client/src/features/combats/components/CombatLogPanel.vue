<script setup lang="ts">
import type { CombatActionResultDto } from '../types/combatTypes';

defineProps<{
  result: CombatActionResultDto | null;
  error: string | null;
}>();
</script>

<template>
  <section class="combat-log panel">
    <p class="system-label">Journal de tour · résolu côté serveur</p>

    <p v-if="error" class="combat-log__error">
      {{ error }}
    </p>

    <p v-else-if="result">
      Action {{ result.actionType ?? 'BasicAttack' }} :
      {{ result.damage ?? 0 }} dégâts.
      PV restants cible : {{ result.targetRemainingHealth ?? '—' }}.
    </p>

    <p v-else>
      En attente d’une action serveur.
    </p>
  </section>
</template>

<style scoped>
.combat-log {
  padding: var(--space-4);
}

.combat-log p:last-child {
  color: var(--color-muted);
  font-family: var(--font-mono);
  font-size: 0.8rem;
  line-height: 1.5;
}

.combat-log__error {
  color: var(--color-blood) !important;
}
</style>